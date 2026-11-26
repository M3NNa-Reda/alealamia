using System.Net;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.FreelancerBlock;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.DTOs.Tasks;
using alealamia.Domain.Enums;
using alealamia.Integration.Tests.Support;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Integration.Tests.Api;

/// <summary>
/// Blocks (BR-09, BR-11, BR-12): withdrawing and missing the final deadline block the freelancer, a block only stops
/// new offers, and an expired or manual block behaves as the admin expects.
/// </summary>
[Collection(ApiCollection.Name)]
public class BlockEnforcementEndToEndTests
{
    private readonly ApiFixture _api;

    public BlockEnforcementEndToEndTests(ApiFixture api)
    {
        _api = api;
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Withdraw_BlocksTheFreelancerForSevenDays_AndTheTaskIsNotOfferedBackToThem()
    {
        var (_, freelancer, task) = await AssignedTaskAsync();

        await (await freelancer.Http.PostJsonAsync($"api/tasks/{task.Id}/withdraw", new { reason = "I am not available" }))
            .ExpectAsync(HttpStatusCode.OK);

        var block = Assert.Single(await ActiveBlocksAsync(freelancer.ProfileId));
        Assert.Equal(task.Id, block.TaskId);
        AssertDays(block, 7);

        var eligible = await _api.Admin.GetAsAsync<List<EligibleFreelancerDto>>($"api/admin/tasks/{task.Id}/eligible-freelancers");
        Assert.DoesNotContain(eligible, f => f.FreelancerProfileId == freelancer.ProfileId);

        var offers = await freelancer.Http.GetAsAsync<PagedResultDto<TaskOfferDetailsDto>>("api/task-offers/my-offers");
        Assert.DoesNotContain(offers.Items, o => o.TaskId == task.Id && o.Status == TaskOfferStatus.Pending);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AdminReassign_DoesNotBlockTheFreelancer()
    {
        var (world, freelancer, task) = await AssignedTaskAsync();
        var other = await _api.CreateFreelancerAsync(world);

        await (await _api.Admin.PostJsonAsync($"api/admin/tasks/{task.Id}/reassign",
            new { newFreelancerProfileId = other.ProfileId, reason = "Admin decision" })).ExpectAsync(HttpStatusCode.OK);

        Assert.Empty(await ActiveBlocksAsync(freelancer.ProfileId));
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task NoShow_ReturnsTheTaskToThePool_AndBlocksTheFreelancerForThirtyDays()
    {
        var (_, freelancer, task) = await AssignedTaskAsync();
        await _api.SetPublishedDeadlineAsync(task.Id, DateTime.UtcNow.AddDays(-3)); // final deadline passed 2 days ago

        var run = await _api.RunNoShowCheckAsync();

        Assert.Contains(task.Id, run.ProcessedTaskIds);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Published, details.Status);
        Assert.Null(details.AssignedFreelancerId);

        var block = Assert.Single(await ActiveBlocksAsync(freelancer.ProfileId));
        Assert.Equal(task.Id, block.TaskId);
        AssertDays(block, 30);

        var history = await _api.Admin.GetAsAsync<List<TaskReassignmentDto>>($"api/admin/tasks/{task.Id}/reassignments");
        Assert.Equal(freelancer.ProfileId, Assert.Single(history).PreviousFreelancerId);

        var eligible = await _api.Admin.GetAsAsync<List<EligibleFreelancerDto>>($"api/admin/tasks/{task.Id}/eligible-freelancers");
        Assert.DoesNotContain(eligible, f => f.FreelancerProfileId == freelancer.ProfileId);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task NoShow_DoesNotTouchATaskThatIsStillInTheGraceDay()
    {
        var (_, freelancer, task) = await AssignedTaskAsync();
        await _api.SetPublishedDeadlineAsync(task.Id, DateTime.UtcNow.AddHours(-12)); // final deadline in 12 hours

        var run = await _api.RunNoShowCheckAsync();

        Assert.DoesNotContain(task.Id, run.ProcessedTaskIds);
        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Assigned, details.Status);
        Assert.Empty(await ActiveBlocksAsync(freelancer.ProfileId));
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task NoShow_RespectsAnExtensionGrantedByTheAdmin()
    {
        var (_, freelancer, task) = await AssignedTaskAsync();
        await _api.SetPublishedDeadlineAsync(task.Id, DateTime.UtcNow.AddDays(-10));
        await (await _api.Admin.PostJsonAsync($"api/admin/tasks/{task.Id}/deadline/extend",
            new { newDeadline = DateTime.UtcNow.AddDays(2), reason = "Client asked for more time" })).ExpectAsync(HttpStatusCode.OK);

        var run = await _api.RunNoShowCheckAsync();

        Assert.DoesNotContain(task.Id, run.ProcessedTaskIds);
        Assert.Empty(await ActiveBlocksAsync(freelancer.ProfileId));
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task NoShow_RunTwice_BlocksOnlyOnce()
    {
        var (_, freelancer, task) = await AssignedTaskAsync();
        await _api.SetPublishedDeadlineAsync(task.Id, DateTime.UtcNow.AddDays(-3));

        await _api.RunNoShowCheckAsync();
        var second = await _api.RunNoShowCheckAsync();

        Assert.DoesNotContain(task.Id, second.ProcessedTaskIds);
        Assert.Single(await ActiveBlocksAsync(freelancer.ProfileId));
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task ABlock_StopsNewOffers_ButOnlyWhileItIsActive()
    {
        var world = await _api.CreateWorldAsync();
        var blocked = await _api.CreateFreelancerAsync(world);
        var expired = await _api.CreateFreelancerAsync(world);
        var free = await _api.CreateFreelancerAsync(world);

        await (await _api.Admin.PostJsonAsync("api/admin/freelancer-blocks", new
        {
            freelancerProfileId = blocked.ProfileId,
            reason = "Manual block",
            startedAt = DateTime.UtcNow.AddHours(-1),
            endsAt = DateTime.UtcNow.AddDays(1)
        })).ExpectAsync(HttpStatusCode.OK);

        await (await _api.Admin.PostJsonAsync("api/admin/freelancer-blocks", new
        {
            freelancerProfileId = expired.ProfileId,
            reason = "Old block",
            startedAt = DateTime.UtcNow.AddDays(-3),
            endsAt = DateTime.UtcNow.AddDays(-2)
        })).ExpectAsync(HttpStatusCode.OK);

        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);

        var eligible = await _api.Admin.GetAsAsync<List<EligibleFreelancerDto>>($"api/admin/tasks/{task.Id}/eligible-freelancers");
        var ids = eligible.Select(f => f.FreelancerProfileId).ToList();

        Assert.DoesNotContain(blocked.ProfileId, ids);
        Assert.Contains(expired.ProfileId, ids);
        Assert.Contains(free.ProfileId, ids);

        var blockedOffers = await blocked.Http.GetAsAsync<PagedResultDto<TaskOfferDetailsDto>>("api/task-offers/my-offers");
        Assert.DoesNotContain(blockedOffers.Items, o => o.TaskId == task.Id);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task ABlock_DoesNotCancelATaskThatIsAlreadyAssigned()
    {
        var (_, freelancer, task) = await AssignedTaskAsync();

        await (await _api.Admin.PostJsonAsync("api/admin/freelancer-blocks", new
        {
            freelancerProfileId = freelancer.ProfileId,
            reason = "Manual block",
            startedAt = DateTime.UtcNow.AddHours(-1),
            endsAt = DateTime.UtcNow.AddDays(7)
        })).ExpectAsync(HttpStatusCode.OK);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Assigned, details.Status);
        Assert.Equal(freelancer.ProfileId, details.AssignedFreelancerId);
    }

    // ---------- Helpers ----------

    private async Task<List<FreelancerBlockDto>> ActiveBlocksAsync(int freelancerProfileId)
        => await _api.Admin.GetAsAsync<List<FreelancerBlockDto>>($"api/admin/freelancer-blocks/freelancers/{freelancerProfileId}/active");

    private static void AssertDays(FreelancerBlockDto block, int days)
    {
        var length = block.EndsAt - block.StartedAt;
        Assert.InRange(length.TotalDays, days - 0.01, days + 0.01);
    }

    private async Task<TaskDto> CreateTaskAsync(World world)
    {
        var response = await _api.Admin.PostJsonAsync("api/admin/tasks", new
        {
            projectId = _api.ProjectId,
            trackId = world.TrackId,
            title = "E2E block task",
            description = "Created by the block end-to-end tests.",
            rewardAmount = 1000m,
            publishedDeadline = DateTime.UtcNow.AddDays(7),
            requiredSkillIds = new[] { world.SkillId }
        });
        await response.ExpectAsync(HttpStatusCode.Created);
        return await response.ReadAsync<TaskDto>();
    }

    private async System.Threading.Tasks.Task PublishAsync(int taskId)
    {
        var response = await _api.Admin.PatchJsonAsync($"api/admin/tasks/{taskId}/status", new { newStatus = (int)TaskStatus.Published });
        await response.ExpectAsync(HttpStatusCode.OK);
    }

    private async Task<(World World, FreelancerAccount Freelancer, TaskDto Task)> AssignedTaskAsync()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);
        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);

        var offers = await freelancer.Http.GetAsAsync<PagedResultDto<TaskOfferDetailsDto>>("api/task-offers/my-offers");
        var offer = Assert.Single(offers.Items, o => o.TaskId == task.Id);
        await (await freelancer.Http.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = true }))
            .ExpectAsync(HttpStatusCode.OK);

        return (world, freelancer, task);
    }
}
