using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using alealamia.Application.Tests.Support;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using NSubstitute;
using Task = System.Threading.Tasks.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Tests;

public class TaskOfferServiceTests
{
    private readonly ITaskOfferRepository _offers = Substitute.For<ITaskOfferRepository>();
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly ITaskEligibilityService _eligibility = Substitute.For<ITaskEligibilityService>();
    private readonly ITaskAssignmentService _assignment = Substitute.For<ITaskAssignmentService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TaskOfferService _service;

    public TaskOfferServiceTests()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask());
        _service = new TaskOfferService(
            _offers,
            _tasks,
            _eligibility,
            _assignment,
            _unitOfWork,
            new FixedTimeProvider(TestData.Now));
    }

    private static EligibleFreelancerDto Eligible(int id) => new()
    {
        FreelancerProfileId = id,
        UserId = $"user-{id}",
        TrackId = 1,
        RemainingThisMonth = 10
    };

    private static TaskOffer Offer(int id = 20, int freelancerId = 5, TaskOfferStatus status = TaskOfferStatus.Pending) => new()
    {
        Id = id,
        TaskId = 1,
        FreelancerProfileId = freelancerId,
        Status = status,
        SentAt = TestData.Now.AddHours(-1),
        Task = TestData.NewTask()
    };

    [Fact]
    public async Task SendOffers_SkipsFreelancersWhoAlreadyHaveAnOffer()
    {
        _eligibility.GetEligibleFreelancersAsync(1).Returns(new[] { Eligible(5), Eligible(6), Eligible(7) });
        _offers.GetOfferedFreelancerIdsAsync(1).Returns(new HashSet<int> { 6 });

        var result = (await _service.SendOffersAsync(1)).ToList();

        Assert.Equal(new[] { 5, 7 }, result.Select(o => o.FreelancerProfileId).ToArray());
        Assert.All(result, o => Assert.Equal(TaskOfferStatus.Pending, o.Status));
        await _offers.Received(1).AddRangeAsync(Arg.Is<IEnumerable<TaskOffer>>(o => o.Count() == 2));
        await _unitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task SendOffers_DoesNothing_WhenEveryoneWasAlreadyOffered()
    {
        _eligibility.GetEligibleFreelancersAsync(1).Returns(new[] { Eligible(5) });
        _offers.GetOfferedFreelancerIdsAsync(1).Returns(new HashSet<int> { 5 });

        var result = await _service.SendOffersAsync(1);

        Assert.Empty(result);
        await _offers.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<TaskOffer>>());
    }

    [Theory]
    [InlineData(TaskStatus.Draft)]
    [InlineData(TaskStatus.Assigned)]
    [InlineData(TaskStatus.Cancelled)]
    public async Task SendOffers_Throws_WhenTheTaskIsNotPublished(TaskStatus status)
    {
        _tasks.GetReadOnlyByIdAsync(2).Returns(TestData.NewTask(id: 2, status: status));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendOffersAsync(2));
    }

    [Fact]
    public async Task Create_Throws_WhenTheFreelancerIsNotEligible()
    {
        _eligibility.CheckAsync(1, 5).Returns(TestData.NotEligible(1, 5, "The freelancer is unavailable."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(new CreateTaskOfferDto { TaskId = 1, FreelancerProfileId = 5 }));

        await _offers.DidNotReceive().AddAsync(Arg.Any<TaskOffer>());
    }

    [Fact]
    public async Task Create_Throws_WhenAnOfferWasAlreadySent()
    {
        _eligibility.CheckAsync(1, 5).Returns(TestData.Eligible(1, 5));
        _offers.GetOfferedFreelancerIdsAsync(1).Returns(new HashSet<int> { 5 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(new CreateTaskOfferDto { TaskId = 1, FreelancerProfileId = 5 }));
    }

    [Fact]
    public async Task Create_SendsAPendingOffer()
    {
        _eligibility.CheckAsync(1, 5).Returns(TestData.Eligible(1, 5));
        _offers.GetOfferedFreelancerIdsAsync(1).Returns(new HashSet<int>());

        var result = await _service.CreateAsync(new CreateTaskOfferDto { TaskId = 1, FreelancerProfileId = 5 });

        Assert.Equal(TaskOfferStatus.Pending, result.Status);
        Assert.Equal(TestData.Now, result.SentAt);
        await _unitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Respond_Decline_MarksTheOfferAndDoesNotAssign()
    {
        _offers.GetDetailsByIdAsync(20).Returns(Offer());
        _offers.TryDeclineAsync(20, TestData.Now).Returns(true);

        var result = await _service.RespondAsync(20, new RespondToTaskOfferDto { FreelancerProfileId = 5, Accept = false });

        Assert.Equal(TaskOfferStatus.Declined, result.Offer.Status);
        Assert.Equal(TestData.Now, result.Offer.RespondedAt);
        Assert.Null(result.Assignment);
        await _assignment.DidNotReceive().AcceptOfferAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Respond_Accept_DelegatesToTheAssignmentService()
    {
        _offers.GetDetailsByIdAsync(20).Returns(Offer());
        _assignment.AcceptOfferAsync(20, 5).Returns(new TaskAssignmentResultDto
        {
            TaskId = 1,
            FreelancerProfileId = 5,
            OfferId = 20,
            Status = TaskStatus.Assigned,
            AssignedAt = TestData.Now,
            ClosedOfferCount = 2
        });

        var result = await _service.RespondAsync(20, new RespondToTaskOfferDto { FreelancerProfileId = 5, Accept = true });

        Assert.Equal(TaskOfferStatus.Accepted, result.Offer.Status);
        Assert.NotNull(result.Assignment);
        Assert.Equal(2, result.Assignment!.ClosedOfferCount);
        await _offers.DidNotReceive().TryDeclineAsync(Arg.Any<int>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Respond_Throws_WhenTheOfferBelongsToAnotherFreelancer()
    {
        _offers.GetDetailsByIdAsync(20).Returns(Offer(freelancerId: 5));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.RespondAsync(20, new RespondToTaskOfferDto { FreelancerProfileId = 6, Accept = true }));
    }

    [Fact]
    public async Task Respond_Throws_WhenTheOfferWasAlreadyAnswered()
    {
        _offers.GetDetailsByIdAsync(20).Returns(Offer(status: TaskOfferStatus.Closed));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RespondAsync(20, new RespondToTaskOfferDto { FreelancerProfileId = 5, Accept = false }));
    }

    [Fact]
    public async Task Respond_Throws_WhenTheOfferDoesNotExist()
    {
        _offers.GetDetailsByIdAsync(99).Returns((TaskOffer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.RespondAsync(99, new RespondToTaskOfferDto { FreelancerProfileId = 5, Accept = true }));
    }
}
