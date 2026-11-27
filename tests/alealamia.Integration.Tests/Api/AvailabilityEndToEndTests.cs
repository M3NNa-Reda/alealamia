using System.Net;
using alealamia.Application.DTOs.Freelancer;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Integration.Tests.Support;

namespace alealamia.Integration.Tests.Api;

/// <summary>Availability: approval switches it on, a freelancer can switch it off and on again, and it drives eligibility.</summary>
[Collection(ApiCollection.Name)]
public class AvailabilityEndToEndTests
{
    private readonly ApiFixture _api;

    public AvailabilityEndToEndTests(ApiFixture api)
    {
        _api = api;
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Approval_SwitchesAvailabilityOn_WithoutTouchingTheDatabase()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world, approve: true, available: true);

        var status = await freelancer.Http.GetAsAsync<FreelancerStatusDto>("api/FreelancerProfile/status");

        Assert.True(status.IsAvailable);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Freelancer_CanSwitchAvailabilityOffAndOn_AndEligibilityFollows()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);
        var task = await CreateTaskAsync(world);

        Assert.Contains(await EligibleIdsAsync(task), id => id == freelancer.ProfileId);

        await (await freelancer.Http.PatchJsonAsync("api/FreelancerProfile/availability", new { isAvailable = false }))
            .ExpectAsync(HttpStatusCode.OK);
        Assert.False((await freelancer.Http.GetAsAsync<FreelancerStatusDto>("api/FreelancerProfile/status")).IsAvailable);
        Assert.DoesNotContain(await EligibleIdsAsync(task), id => id == freelancer.ProfileId);

        await (await freelancer.Http.PatchJsonAsync("api/FreelancerProfile/availability", new { isAvailable = true }))
            .ExpectAsync(HttpStatusCode.OK);
        Assert.Contains(await EligibleIdsAsync(task), id => id == freelancer.ProfileId);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task APendingFreelancer_CannotChangeAvailability()
    {
        var world = await _api.CreateWorldAsync();
        var pending = await _api.CreateFreelancerAsync(world, approve: false, available: false);

        await (await pending.Http.PatchJsonAsync("api/FreelancerProfile/availability", new { isAvailable = true }))
            .ExpectAsync(HttpStatusCode.BadRequest);

        Assert.False((await pending.Http.GetAsAsync<FreelancerStatusDto>("api/FreelancerProfile/status")).IsAvailable);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task OnlyAFreelancer_CanChangeAvailability()
    {
        var anonymous = _api.CreateAnonymousClient();
        await (await anonymous.PatchJsonAsync("api/FreelancerProfile/availability", new { isAvailable = true }))
            .ExpectAsync(HttpStatusCode.Unauthorized);

        await (await _api.Admin.PatchJsonAsync("api/FreelancerProfile/availability", new { isAvailable = true }))
            .ExpectAsync(HttpStatusCode.Forbidden);
    }

    private async Task<List<int>> EligibleIdsAsync(alealamia.Application.DTOs.Tasks.TaskDto task)
    {
        var list = await _api.Admin.GetAsAsync<List<EligibleFreelancerDto>>($"api/admin/tasks/{task.Id}/eligible-freelancers");
        return list.Select(f => f.FreelancerProfileId).ToList();
    }

    private async Task<alealamia.Application.DTOs.Tasks.TaskDto> CreateTaskAsync(World world)
    {
        var response = await _api.Admin.PostJsonAsync("api/admin/tasks", new
        {
            projectId = _api.ProjectId,
            trackId = world.TrackId,
            title = "E2E availability task",
            description = "Created by the availability end-to-end tests.",
            rewardAmount = 1000m,
            publishedDeadline = DateTime.UtcNow.AddDays(7),
            requiredSkillIds = new[] { world.SkillId }
        });
        await response.ExpectAsync(HttpStatusCode.Created);
        var task = await response.ReadAsync<alealamia.Application.DTOs.Tasks.TaskDto>();
        await (await _api.Admin.PatchJsonAsync($"api/admin/tasks/{task.Id}/status",
            new { newStatus = (int)alealamia.Domain.Enums.TaskStatus.Published })).ExpectAsync(HttpStatusCode.OK);
        return task;
    }
}
