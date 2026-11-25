using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.DTOs.Tasks;
using alealamia.Domain.Enums;
using alealamia.Integration.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Integration.Tests.Api;

/// <summary>
/// Drives the whole task module through real HTTP calls: admin creates and publishes a task, freelancers receive
/// offers, one accepts, the deadline is extended, the freelancer withdraws, the admin cancels.
/// </summary>
[Collection(ApiCollection.Name)]
public class TaskModuleEndToEndTests
{
    private readonly ApiFixture _api;

    public TaskModuleEndToEndTests(ApiFixture api)
    {
        _api = api;
    }

    // ---------- Lifecycle ----------

    [SqlServerFact]
    public async System.Threading.Tasks.Task Admin_Publishes_Freelancer_Accepts_TaskBecomesAssigned()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);

        var task = await CreateTaskAsync(world, 1500.5m);
        Assert.Equal(TaskStatus.Draft, task.Status);
        Assert.Equal(1500.5m, task.RewardAmount);
        Assert.False(string.IsNullOrWhiteSpace(task.CreatedByUserId));

        await PublishAsync(task.Id);

        var offer = await FindMyOfferAsync(freelancer, task.Id);
        Assert.Equal(TaskOfferStatus.Pending, offer.Status);
        Assert.Equal(1500.5m, offer.TaskRewardAmount);

        var respond = await freelancer.Http.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = true });
        await respond.ExpectAsync(HttpStatusCode.OK);
        var response = await respond.ReadAsync<TaskOfferResponseDto>();
        Assert.NotNull(response.Assignment);
        Assert.Equal(freelancer.ProfileId, response.Assignment!.FreelancerProfileId);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Assigned, details.Status);
        Assert.Equal(freelancer.ProfileId, details.AssignedFreelancerId);
        Assert.Contains(details.StatusHistory, h => h.NewStatus == TaskStatus.Assigned);

        var assignment = await _api.Admin.GetAsAsync<TaskAssignmentDto>($"api/admin/tasks/{task.Id}/assignment");
        Assert.Equal(freelancer.ProfileId, assignment.FreelancerProfileId);

        var mine = await freelancer.Http.GetAsAsync<PagedResultDto<FreelancerTaskDto>>("api/tasks/my-tasks");
        Assert.Contains(mine.Items, t => t.Id == task.Id && t.IsAssignedToMe && t.Status == TaskStatus.Assigned);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Freelancer_TaskView_HidesInternalFields()
    {
        var (world, freelancer, task) = await AssignedTaskAsync();

        var json = await freelancer.Http.GetStringAsync($"api/tasks/{task.Id}");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(task.Id, root.GetProperty("id").GetInt32());
        Assert.False(root.TryGetProperty("projectId", out _), "projectId must not be exposed to freelancers.");
        Assert.False(root.TryGetProperty("createdByUserId", out _), "createdByUserId must not be exposed to freelancers.");
        Assert.False(root.TryGetProperty("statusHistory", out _), "the internal history must not be exposed to freelancers.");
        Assert.Equal(world.SkillId, root.GetProperty("requiredSkills")[0].GetProperty("skillId").GetInt32());
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AvailableTasks_ListsPublishedTasksOfTheFreelancersTrackOnly()
    {
        var world = await _api.CreateWorldAsync();
        var otherWorld = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);

        var visible = await CreateTaskAsync(world, title: "Visible");
        var otherTrack = await CreateTaskAsync(otherWorld, title: "Other track");
        var draft = await CreateTaskAsync(world, title: "Still a draft");
        await PublishAsync(visible.Id);
        await PublishAsync(otherTrack.Id);

        var available = await freelancer.Http.GetAsAsync<PagedResultDto<FreelancerTaskDto>>("api/tasks");

        Assert.Contains(available.Items, t => t.Id == visible.Id);
        Assert.DoesNotContain(available.Items, t => t.Id == otherTrack.Id);
        Assert.DoesNotContain(available.Items, t => t.Id == draft.Id);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task EligibleFreelancers_OnlyApprovedAvailableAndMatching()
    {
        var world = await _api.CreateWorldAsync();
        var eligible = await _api.CreateFreelancerAsync(world);
        var notApproved = await _api.CreateFreelancerAsync(world, approve: false);
        var notAvailable = await _api.CreateFreelancerAsync(world, available: false);
        var task = await CreateTaskAsync(world);

        var list = await _api.Admin.GetAsAsync<List<EligibleFreelancerDto>>($"api/admin/tasks/{task.Id}/eligible-freelancers");

        Assert.Contains(list, f => f.FreelancerProfileId == eligible.ProfileId);
        Assert.DoesNotContain(list, f => f.FreelancerProfileId == notApproved.ProfileId);
        Assert.DoesNotContain(list, f => f.FreelancerProfileId == notAvailable.ProfileId);

        var check = await _api.Admin.GetAsAsync<TaskEligibilityDto>($"api/admin/tasks/{task.Id}/eligibility/{notApproved.ProfileId}");
        Assert.False(check.IsEligible);
        Assert.NotEmpty(check.Reasons);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task ExtendDeadline_AcceptsALaterDate_AndRejectsAnEarlierOne()
    {
        var (_, _, task) = await AssignedTaskAsync();

        var later = DateTime.UtcNow.AddDays(14);
        var extend = await _api.Admin.PostJsonAsync($"api/admin/tasks/{task.Id}/deadline/extend", new { newDeadline = later, reason = "Client asked for more time" });
        await extend.ExpectAsync(HttpStatusCode.OK);

        var deadline = await _api.Admin.GetAsAsync<TaskDeadlineDto>($"api/admin/tasks/{task.Id}/deadline");
        Assert.NotNull(deadline.ExtensionDeadline);
        Assert.False(deadline.IsOverdue);

        var earlier = await _api.Admin.PostJsonAsync($"api/admin/tasks/{task.Id}/deadline/extend", new { newDeadline = DateTime.UtcNow.AddDays(1) });
        await earlier.ExpectAsync(HttpStatusCode.BadRequest);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Withdraw_ReturnsTheTaskToPublished_AndIsRecorded()
    {
        var (_, freelancer, task) = await AssignedTaskAsync();

        var withdraw = await freelancer.Http.PostJsonAsync($"api/tasks/{task.Id}/withdraw", new { reason = "I am not available" });
        await withdraw.ExpectAsync(HttpStatusCode.OK);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Published, details.Status);
        Assert.Null(details.AssignedFreelancerId);

        var history = await _api.Admin.GetAsAsync<List<TaskReassignmentDto>>($"api/admin/tasks/{task.Id}/reassignments");
        var entry = Assert.Single(history);
        Assert.Equal(freelancer.ProfileId, entry.PreviousFreelancerId);
        Assert.Null(entry.NewFreelancerId);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Withdraw_ByAFreelancerWhoDoesNotHoldTheTask_IsRejected()
    {
        var (world, _, task) = await AssignedTaskAsync();
        var stranger = await _api.CreateFreelancerAsync(world);

        var response = await stranger.Http.PostJsonAsync($"api/tasks/{task.Id}/withdraw", new { reason = "Not mine" });

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Conflict or HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
            $"A freelancer who does not hold the task must not be able to withdraw it, got {(int)response.StatusCode}.");

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Assigned, details.Status);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AdminReassign_MovesTheTaskToAnotherFreelancer()
    {
        var (world, first, task) = await AssignedTaskAsync();
        var second = await _api.CreateFreelancerAsync(world);

        var reassign = await _api.Admin.PostJsonAsync($"api/admin/tasks/{task.Id}/reassign", new { newFreelancerProfileId = second.ProfileId, reason = "Quality concerns" });
        await reassign.ExpectAsync(HttpStatusCode.OK);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(second.ProfileId, details.AssignedFreelancerId);

        var mineFirst = await first.Http.GetAsAsync<PagedResultDto<FreelancerTaskDto>>("api/tasks/my-tasks");
        Assert.DoesNotContain(mineFirst.Items, t => t.Id == task.Id);

        var mineSecond = await second.Http.GetAsAsync<PagedResultDto<FreelancerTaskDto>>("api/tasks/my-tasks");
        Assert.Contains(mineSecond.Items, t => t.Id == task.Id);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AdminAssign_AssignsAPublishedTaskDirectly()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);
        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);

        var assign = await _api.Admin.PostJsonAsync($"api/admin/tasks/{task.Id}/assign", new { freelancerProfileId = freelancer.ProfileId });
        await assign.ExpectAsync(HttpStatusCode.OK);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Assigned, details.Status);
        Assert.Equal(freelancer.ProfileId, details.AssignedFreelancerId);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Cancel_ClosesTheTask_AndNothingCanChangeItAfterwards()
    {
        var world = await _api.CreateWorldAsync();
        var task = await CreateTaskAsync(world);

        var cancel = await _api.Admin.DeleteAsync($"api/admin/tasks/{task.Id}?reason=Client%20cancelled");
        await cancel.ExpectAsync(HttpStatusCode.NoContent);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Cancelled, details.Status);
        Assert.Contains(details.StatusHistory, h => h.NewStatus == TaskStatus.Cancelled && h.Notes != null && h.Notes.Contains("Client cancelled"));

        var publish = await _api.Admin.PatchJsonAsync($"api/admin/tasks/{task.Id}/status", new { newStatus = (int)TaskStatus.Published });
        await publish.ExpectAsync(HttpStatusCode.Conflict);

        var cancelAgain = await _api.Admin.DeleteAsync($"api/admin/tasks/{task.Id}");
        await cancelAgain.ExpectAsync(HttpStatusCode.Conflict);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task UpdateTask_ChangesTheRewardWhileInDraft_AndTheHistoryIsKept()
    {
        var world = await _api.CreateWorldAsync();
        var task = await CreateTaskAsync(world, 1000m);

        var update = await _api.Admin.PutAsJsonAsync($"api/admin/tasks/{task.Id}", new
        {
            title = "Renamed task",
            description = "Updated description",
            rewardAmount = 1750.25m,
            publishedDeadline = DateTime.UtcNow.AddDays(10)
        }, ApiJson.Options);
        await update.ExpectAsync(HttpStatusCode.OK);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal("Renamed task", details.Title);
        Assert.Equal(1750.25m, details.RewardAmount);
        Assert.Equal(TaskStatus.Draft, details.Status);
    }

    // ---------- Concurrency ----------

    [SqlServerFact]
    public async System.Threading.Tasks.Task ManyFreelancersAcceptingAtOnce_ExactlyOneWins()
    {
        const int freelancerCount = 8;
        var world = await _api.CreateWorldAsync();

        var freelancers = new List<FreelancerAccount>();
        for (var i = 0; i < freelancerCount; i++)
            freelancers.Add(await _api.CreateFreelancerAsync(world));

        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);

        var offers = new List<(FreelancerAccount Freelancer, TaskOfferDetailsDto Offer)>();
        foreach (var freelancer in freelancers)
            offers.Add((freelancer, await FindMyOfferAsync(freelancer, task.Id)));

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = offers.Select(async entry =>
        {
            await gate.Task;
            return await entry.Freelancer.Http.PostJsonAsync($"api/task-offers/{entry.Offer.Id}/respond", new { accept = true });
        }).ToList();

        gate.SetResult();
        var responses = await System.Threading.Tasks.Task.WhenAll(attempts);

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(freelancerCount - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Assigned, details.Status);
        Assert.Equal(1, details.StatusHistory.Count(h => h.NewStatus == TaskStatus.Assigned));
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AcceptingTheSameOfferTwice_Returns409()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);
        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);
        var offer = await FindMyOfferAsync(freelancer, task.Id);

        await (await freelancer.Http.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = true }))
            .ExpectAsync(HttpStatusCode.OK);
        await (await freelancer.Http.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = true }))
            .ExpectAsync(HttpStatusCode.Conflict);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task DecliningAnOffer_LeavesTheTaskPublished()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);
        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);
        var offer = await FindMyOfferAsync(freelancer, task.Id);

        await (await freelancer.Http.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = false }))
            .ExpectAsync(HttpStatusCode.OK);

        var details = await _api.Admin.GetAsAsync<TaskDetailsDto>($"api/admin/tasks/{task.Id}");
        Assert.Equal(TaskStatus.Published, details.Status);
        Assert.Null(details.AssignedFreelancerId);
    }

    // ---------- Security ----------

    [SqlServerFact]
    public async System.Threading.Tasks.Task AdminEndpoints_RejectAnonymousCallers_And_Freelancers()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);
        using var anonymous = _api.CreateAnonymousClient();

        var requests = new (HttpMethod Method, string Url)[]
        {
            (HttpMethod.Get, "api/admin/tasks"),
            (HttpMethod.Post, "api/admin/tasks"),
            (HttpMethod.Get, "api/admin/tasks/1"),
            (HttpMethod.Delete, "api/admin/tasks/1"),
            (HttpMethod.Get, "api/admin/tasks/1/eligible-freelancers"),
            (HttpMethod.Post, "api/admin/tasks/1/assign"),
            (HttpMethod.Post, "api/admin/tasks/1/reassign"),
            (HttpMethod.Get, "api/admin/tasks/1/reassignments"),
            (HttpMethod.Get, "api/admin/tasks/1/deadline"),
            (HttpMethod.Get, "api/admin/tasks/deadlines/overdue"),
            (HttpMethod.Get, "api/task-offers"),
            (HttpMethod.Post, "api/task-offers/tasks/1/send"),
        };

        foreach (var (method, url) in requests)
        {
            using var anonymousRequest = new HttpRequestMessage(method, url) { Content = JsonContent(method) };
            var anonymousResponse = await anonymous.SendAsync(anonymousRequest);
            Assert.True(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized, $"{method} {url} without a token returned {(int)anonymousResponse.StatusCode}, expected 401.");

            using var freelancerRequest = new HttpRequestMessage(method, url) { Content = JsonContent(method) };
            var freelancerResponse = await freelancer.Http.SendAsync(freelancerRequest);
            Assert.True(freelancerResponse.StatusCode == HttpStatusCode.Forbidden, $"{method} {url} as a freelancer returned {(int)freelancerResponse.StatusCode}, expected 403.");
        }
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task FreelancerEndpoints_RejectAdmins_And_Clients()
    {
        var (client, _, _) = await _api.RegisterAsync("Client");

        var urls = new[] { "api/tasks", "api/tasks/my-tasks", "api/task-offers/my-offers", "api/task-offers/my-limit" };

        foreach (var url in urls)
        {
            var asAdmin = await _api.Admin.GetAsync(url);
            Assert.True(asAdmin.StatusCode == HttpStatusCode.Forbidden, $"GET {url} as admin returned {(int)asAdmin.StatusCode}, expected 403.");

            var asClient = await client.GetAsync(url);
            Assert.True(asClient.StatusCode == HttpStatusCode.Forbidden, $"GET {url} as client returned {(int)asClient.StatusCode}, expected 403.");
        }
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AFreelancerCannotSee_AnotherFreelancersOffer_Task_Or_Files()
    {
        var world = await _api.CreateWorldAsync();
        var otherWorld = await _api.CreateWorldAsync();
        var owner = await _api.CreateFreelancerAsync(world);
        var stranger = await _api.CreateFreelancerAsync(otherWorld);

        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);
        var offer = await FindMyOfferAsync(owner, task.Id);

        (await stranger.Http.GetAsync($"api/task-offers/{offer.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.Http.GetAsync($"api/tasks/{task.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.Http.GetAsync($"api/tasks/{task.Id}/files")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.Http.PostJsonAsync($"api/tasks/{task.Id}/files", new { fileId = 1 })).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.Http.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = true })).StatusCode.ShouldNotBe(HttpStatusCode.OK);

        var strangerOffers = await stranger.Http.GetAsAsync<PagedResultDto<TaskOfferDetailsDto>>("api/task-offers/my-offers");
        Assert.DoesNotContain(strangerOffers.Items, o => o.Id == offer.Id);

        // The owner still has full access.
        (await owner.Http.GetAsync($"api/task-offers/{offer.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await owner.Http.GetAsync($"api/tasks/{task.Id}/files")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AFreelancerWaitingForApproval_CannotUseTheTaskEndpoints()
    {
        var world = await _api.CreateWorldAsync();
        var pending = await _api.CreateFreelancerAsync(world, approve: false);

        foreach (var url in new[] { "api/tasks", "api/tasks/my-tasks" })
        {
            var response = await pending.Http.GetAsync(url);
            Assert.True(
                response.StatusCode == HttpStatusCode.Forbidden,
                $"GET {url} for an unapproved freelancer returned {(int)response.StatusCode}, expected 403.");
        }
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AnAdminCannotRespondToAnOffer()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);
        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);
        var offer = await FindMyOfferAsync(freelancer, task.Id);

        await (await _api.Admin.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = true }))
            .ExpectAsync(HttpStatusCode.Forbidden);
    }

    // ---------- Validation and errors ----------

    [SqlServerFact]
    public async System.Threading.Tasks.Task CreateTask_WithInvalidInput_ReturnsProblemDetails400()
    {
        var world = await _api.CreateWorldAsync();

        var zeroReward = await PostTaskAsync(world, reward: 0m);
        await zeroReward.ExpectAsync(HttpStatusCode.BadRequest);
        var problem = await zeroReward.ReadAsync<ProblemDetails>();
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));

        await (await PostTaskAsync(world, reward: 10.123m)).ExpectAsync(HttpStatusCode.BadRequest);
        await (await PostTaskAsync(world, title: "   ")).ExpectAsync(HttpStatusCode.BadRequest);
        await (await PostTaskAsync(world, deadline: DateTime.UtcNow.AddDays(-1))).ExpectAsync(HttpStatusCode.BadRequest);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task UnknownTask_Returns404()
    {
        await (await _api.Admin.GetAsync("api/admin/tasks/999999")).ExpectAsync(HttpStatusCode.NotFound);
        await (await _api.Admin.GetAsync("api/admin/tasks/999999/deadline")).ExpectAsync(HttpStatusCode.NotFound);
        await (await _api.Admin.PatchJsonAsync("api/admin/tasks/999999/status", new { newStatus = 1 })).ExpectAsync(HttpStatusCode.NotFound);
    }

    // ---------- Helpers ----------

    private async Task<TaskDto> CreateTaskAsync(World world, decimal reward = 1500m, string title = "E2E task")
    {
        var response = await PostTaskAsync(world, reward, title);
        await response.ExpectAsync(HttpStatusCode.Created);
        return await response.ReadAsync<TaskDto>();
    }

    private Task<HttpResponseMessage> PostTaskAsync(World world, decimal reward = 1500m, string title = "E2E task", DateTime? deadline = null)
        => _api.Admin.PostJsonAsync("api/admin/tasks", new
        {
            projectId = _api.ProjectId,
            trackId = world.TrackId,
            title,
            description = "Created by the end-to-end tests.",
            rewardAmount = reward,
            publishedDeadline = deadline ?? DateTime.UtcNow.AddDays(7),
            requiredSkillIds = new[] { world.SkillId }
        });

    private async System.Threading.Tasks.Task PublishAsync(int taskId)
    {
        var response = await _api.Admin.PatchJsonAsync($"api/admin/tasks/{taskId}/status", new { newStatus = (int)TaskStatus.Published });
        await response.ExpectAsync(HttpStatusCode.OK);
    }

    private static async Task<TaskOfferDetailsDto> FindMyOfferAsync(FreelancerAccount freelancer, int taskId)
    {
        var offers = await freelancer.Http.GetAsAsync<PagedResultDto<TaskOfferDetailsDto>>("api/task-offers/my-offers");
        return Assert.Single(offers.Items, o => o.TaskId == taskId);
    }

    private async Task<(World World, FreelancerAccount Freelancer, TaskDto Task)> AssignedTaskAsync()
    {
        var world = await _api.CreateWorldAsync();
        var freelancer = await _api.CreateFreelancerAsync(world);
        var task = await CreateTaskAsync(world);
        await PublishAsync(task.Id);

        var offer = await FindMyOfferAsync(freelancer, task.Id);
        await (await freelancer.Http.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = true }))
            .ExpectAsync(HttpStatusCode.OK);

        return (world, freelancer, task);
    }

    private static HttpContent? JsonContent(HttpMethod method)
        => method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch
            ? System.Net.Http.Json.JsonContent.Create(new { })
            : null;
}

internal static class StatusAssertions
{
    public static void ShouldBe(this HttpStatusCode actual, HttpStatusCode expected)
        => Assert.True(actual == expected, $"Expected {(int)expected} {expected} but got {(int)actual} {actual}.");

    public static void ShouldNotBe(this HttpStatusCode actual, HttpStatusCode unexpected)
        => Assert.True(actual != unexpected, $"Did not expect {(int)unexpected} {unexpected}.");
}
