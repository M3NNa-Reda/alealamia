using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Services;
using alealamia.Application.Tests.Support;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using NSubstitute;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Tests;

public class TaskEligibilityServiceTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly ITaskRequiredSkillRepository _requiredSkills = Substitute.For<ITaskRequiredSkillRepository>();
    private readonly ITaskEligibilityRepository _freelancers = Substitute.For<ITaskEligibilityRepository>();
    private readonly ITaskOfferRepository _offers = Substitute.For<ITaskOfferRepository>();
    private readonly TaskEligibilityService _service;

    public TaskEligibilityServiceTests()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(trackId: 1));
        _requiredSkills.GetByTaskIdAsync(1).Returns(new List<TaskRequiredSkill>
        {
            new() { TaskId = 1, SkillId = 1 },
            new() { TaskId = 1, SkillId = 2 }
        });
        _offers.CountAcceptedSinceAsync(Arg.Any<int>(), Arg.Any<DateTime>()).Returns(0);

        _service = new TaskEligibilityService(
            _tasks,
            _requiredSkills,
            _freelancers,
            _offers,
            new FixedTimeProvider(TestData.Now));
    }

    [Fact]
    public async Task Check_ReturnsEligible_WhenEveryRuleIsMet()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now).Returns(TestData.NewFreelancer());

        var result = await _service.CheckAsync(1, 5);

        Assert.True(result.IsEligible);
        Assert.Empty(result.Reasons);
        Assert.Empty(result.MissingSkillIds);
    }

    [Fact]
    public async Task Check_ReportsMissingSkills()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now)
            .Returns(TestData.NewFreelancer(skillIds: new[] { 1 }));

        var result = await _service.CheckAsync(1, 5);

        Assert.False(result.IsEligible);
        Assert.Equal(new[] { 2 }, result.MissingSkillIds);
    }

    [Fact]
    public async Task Check_RejectsFreelancerWhoIsNotApproved()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now)
            .Returns(TestData.NewFreelancer(approval: FreelancerApprovalStatus.Pending));

        var result = await _service.CheckAsync(1, 5);

        Assert.False(result.IsEligible);
    }

    [Fact]
    public async Task Check_RejectsFreelancerFromAnotherTrack()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now)
            .Returns(TestData.NewFreelancer(trackId: 2));

        var result = await _service.CheckAsync(1, 5);

        Assert.False(result.IsEligible);
    }

    [Fact]
    public async Task Check_RejectsUnavailableFreelancer()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now)
            .Returns(TestData.NewFreelancer(available: false));

        var result = await _service.CheckAsync(1, 5);

        Assert.False(result.IsEligible);
    }

    [Fact]
    public async Task Check_RejectsBlockedFreelancer()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now)
            .Returns(TestData.NewFreelancer(blockedUntil: TestData.Now.AddDays(3)));

        var result = await _service.CheckAsync(1, 5);

        Assert.False(result.IsEligible);
    }

    [Fact]
    public async Task Check_RejectsFreelancerWhoReachedTheMonthlyLimit()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now).Returns(TestData.NewFreelancer());
        _offers.CountAcceptedSinceAsync(5, Arg.Any<DateTime>()).Returns(10);

        var result = await _service.CheckAsync(1, 5);

        Assert.False(result.IsEligible);
        Assert.Equal(10, result.AcceptedThisMonth);
    }

    [Fact]
    public async Task Check_AcceptsFreelancerWithNineAcceptedTasks()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now).Returns(TestData.NewFreelancer());
        _offers.CountAcceptedSinceAsync(5, Arg.Any<DateTime>()).Returns(9);

        var result = await _service.CheckAsync(1, 5);

        Assert.True(result.IsEligible);
    }

    [Fact]
    public async Task Check_CountsAcceptedTasksFromTheFirstDayOfTheCurrentMonth()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now).Returns(TestData.NewFreelancer());

        await _service.CheckAsync(1, 5);

        var monthStart = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        await _offers.Received(1).CountAcceptedSinceAsync(5, monthStart);
    }

    [Fact]
    public async Task Check_Throws_WhenTheFreelancerDoesNotExist()
    {
        _freelancers.GetFreelancerSnapshotAsync(99, TestData.Now).Returns((FreelancerEligibilitySnapshot?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CheckAsync(1, 99));
    }

    [Fact]
    public async Task Check_Throws_WhenTheTaskDoesNotExist()
    {
        _tasks.GetReadOnlyByIdAsync(99).Returns((alealamia.Domain.Entities.Task?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CheckAsync(99, 5));
    }

    [Fact]
    public async Task GetEligibleFreelancers_ExcludesMissingSkillsAndFullQuota()
    {
        _freelancers.GetCandidatesAsync(1, TestData.Now).Returns(new List<FreelancerEligibilitySnapshot>
        {
            TestData.NewFreelancer(id: 5),
            TestData.NewFreelancer(id: 6, skillIds: new[] { 1 }),
            TestData.NewFreelancer(id: 7),
            TestData.NewFreelancer(id: 8)
        });
        _offers.GetAcceptedCountsSinceAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<DateTime>())
            .Returns(new Dictionary<int, int> { [5] = 3, [7] = 10, [8] = 0 });

        var result = (await _service.GetEligibleFreelancersAsync(1)).ToList();

        Assert.Equal(new[] { 8, 5 }, result.Select(r => r.FreelancerProfileId).ToArray());
        Assert.Equal(7, result.Single(r => r.FreelancerProfileId == 5).RemainingThisMonth);
    }

    [Fact]
    public async Task GetEligibleFreelancers_ReturnsEmpty_WhenNoCandidateHasTheSkills()
    {
        _freelancers.GetCandidatesAsync(1, TestData.Now).Returns(new List<FreelancerEligibilitySnapshot>
        {
            TestData.NewFreelancer(id: 6, skillIds: new[] { 3 })
        });

        var result = await _service.GetEligibleFreelancersAsync(1);

        Assert.Empty(result);
        await _offers.DidNotReceive().GetAcceptedCountsSinceAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task GetMonthlyLimit_ReturnsRemainingAndPeriod()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now).Returns(TestData.NewFreelancer());
        _offers.CountAcceptedSinceAsync(5, Arg.Any<DateTime>()).Returns(4);

        var result = await _service.GetMonthlyLimitAsync(5);

        Assert.Equal(10, result.MonthlyLimit);
        Assert.Equal(6, result.Remaining);
        Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), result.PeriodStart);
        Assert.Equal(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), result.PeriodEnd);
    }
}
