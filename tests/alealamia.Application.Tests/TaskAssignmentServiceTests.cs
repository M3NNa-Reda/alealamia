using alealamia.Application.DTOs.TaskAssignments;
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

public class TaskAssignmentServiceTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly ITaskOfferRepository _offers = Substitute.For<ITaskOfferRepository>();
    private readonly ITaskStatusHistoryRepository _history = Substitute.For<ITaskStatusHistoryRepository>();
    private readonly ITaskEligibilityService _eligibility = Substitute.For<ITaskEligibilityService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TaskAssignmentService _service;

    public TaskAssignmentServiceTests()
    {
        _service = new TaskAssignmentService(
            _tasks,
            _offers,
            _history,
            _eligibility,
            _unitOfWork,
            new FixedTimeProvider(TestData.Now));
    }

    private TaskOffer PendingOffer(int offerId = 20, int freelancerId = 5, int taskId = 1) => new()
    {
        Id = offerId,
        TaskId = taskId,
        FreelancerProfileId = freelancerId,
        Status = TaskOfferStatus.Pending,
        SentAt = TestData.Now.AddHours(-2),
        Task = TestData.NewTask(taskId)
    };

    private void GivenOfferAndEligibleFreelancer(TaskOffer offer)
    {
        _offers.GetDetailsByIdAsync(offer.Id).Returns(offer);
        _eligibility.CheckAsync(offer.TaskId, offer.FreelancerProfileId)
            .Returns(TestData.Eligible(offer.TaskId, offer.FreelancerProfileId));
    }

    [Fact]
    public async Task AcceptOffer_AssignsTheTaskAndClosesTheOtherOffers()
    {
        GivenOfferAndEligibleFreelancer(PendingOffer());
        _tasks.TryAssignAsync(1, 5, TestData.Now).Returns(true);
        _offers.TryAcceptAsync(20, TestData.Now).Returns(true);
        _offers.ClosePendingAsync(1, 20, TestData.Now).Returns(3);

        var result = await _service.AcceptOfferAsync(20, 5);

        Assert.Equal(1, result.TaskId);
        Assert.Equal(5, result.FreelancerProfileId);
        Assert.Equal(20, result.OfferId);
        Assert.Equal(TaskStatus.Assigned, result.Status);
        Assert.Equal(3, result.ClosedOfferCount);
        await _unitOfWork.Received(1).BeginTransactionAsync();
        await _unitOfWork.Received(1).CommitTransactionAsync();
        await _unitOfWork.DidNotReceive().RollbackTransactionAsync();
    }

    [Fact]
    public async Task AcceptOffer_RecordsTheStatusChangeInTheHistory()
    {
        GivenOfferAndEligibleFreelancer(PendingOffer());
        _tasks.TryAssignAsync(1, 5, TestData.Now).Returns(true);
        _offers.TryAcceptAsync(20, TestData.Now).Returns(true);

        await _service.AcceptOfferAsync(20, 5);

        await _history.Received(1).AddAsync(Arg.Is<TaskStatusHistory>(h =>
            h.TaskId == 1
            && h.OldStatus == TaskStatus.Published
            && h.NewStatus == TaskStatus.Assigned
            && h.ChangedByUserId == "user-5"));
    }

    [Fact]
    public async Task AcceptOffer_RollsBack_WhenAnotherFreelancerTookTheTask()
    {
        GivenOfferAndEligibleFreelancer(PendingOffer());
        _tasks.TryAssignAsync(1, 5, TestData.Now).Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptOfferAsync(20, 5));

        await _unitOfWork.Received(1).RollbackTransactionAsync();
        await _unitOfWork.DidNotReceive().CommitTransactionAsync();
        await _offers.DidNotReceive().TryAcceptAsync(Arg.Any<int>(), Arg.Any<DateTime>());
        await _offers.DidNotReceive().ClosePendingAsync(Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task AcceptOffer_RollsBack_WhenTheOfferWasAlreadyAnswered()
    {
        GivenOfferAndEligibleFreelancer(PendingOffer());
        _tasks.TryAssignAsync(1, 5, TestData.Now).Returns(true);
        _offers.TryAcceptAsync(20, TestData.Now).Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptOfferAsync(20, 5));

        await _unitOfWork.Received(1).RollbackTransactionAsync();
        await _unitOfWork.DidNotReceive().CommitTransactionAsync();
    }

    [Fact]
    public async Task AcceptOffer_Throws_WhenTheOfferBelongsToAnotherFreelancer()
    {
        GivenOfferAndEligibleFreelancer(PendingOffer(freelancerId: 5));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.AcceptOfferAsync(20, 6));

        await _unitOfWork.DidNotReceive().BeginTransactionAsync();
    }

    [Theory]
    [InlineData(TaskOfferStatus.Accepted)]
    [InlineData(TaskOfferStatus.Declined)]
    [InlineData(TaskOfferStatus.Expired)]
    [InlineData(TaskOfferStatus.Closed)]
    public async Task AcceptOffer_Throws_WhenTheOfferIsNotPending(TaskOfferStatus status)
    {
        var offer = PendingOffer();
        offer.Status = status;
        GivenOfferAndEligibleFreelancer(offer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptOfferAsync(20, 5));

        await _unitOfWork.DidNotReceive().BeginTransactionAsync();
    }

    [Fact]
    public async Task AcceptOffer_Throws_WhenTheOfferDoesNotExist()
    {
        _offers.GetDetailsByIdAsync(99).Returns((TaskOffer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AcceptOfferAsync(99, 5));
    }

    [Fact]
    public async Task AcceptOffer_Throws_WhenTheTaskIsNoLongerPublished()
    {
        var offer = PendingOffer();
        offer.Task.Status = TaskStatus.Assigned;
        offer.Task.AssignedFreelancerId = 9;
        GivenOfferAndEligibleFreelancer(offer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptOfferAsync(20, 5));

        await _unitOfWork.DidNotReceive().BeginTransactionAsync();
    }

    [Fact]
    public async Task AcceptOffer_Throws_WhenTheDeadlineHasPassed()
    {
        var offer = PendingOffer();
        offer.Task.PublishedDeadline = TestData.Now.AddMinutes(-1);
        GivenOfferAndEligibleFreelancer(offer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptOfferAsync(20, 5));
    }

    [Fact]
    public async Task AcceptOffer_Throws_WhenTheFreelancerIsNoLongerEligible()
    {
        var offer = PendingOffer();
        _offers.GetDetailsByIdAsync(20).Returns(offer);
        _eligibility.CheckAsync(1, 5).Returns(TestData.NotEligible(1, 5, "The monthly limit has been reached."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AcceptOfferAsync(20, 5));

        Assert.Contains("monthly limit", exception.Message);
        await _unitOfWork.DidNotReceive().BeginTransactionAsync();
    }

    [Fact]
    public async Task Assign_CreatesAnAcceptedOfferForTheFreelancer()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask());
        _eligibility.CheckAsync(1, 5).Returns(TestData.Eligible(1, 5));
        _tasks.TryAssignAsync(1, 5, TestData.Now).Returns(true);
        _offers.GetOrCreateAcceptedAsync(1, 5, TestData.Now)
            .Returns(new TaskOffer { Id = 31, TaskId = 1, FreelancerProfileId = 5, Status = TaskOfferStatus.Accepted });

        var result = await _service.AssignAsync(new AssignTaskDto
        {
            TaskId = 1,
            FreelancerProfileId = 5,
            AssignedByUserId = "admin-1"
        });

        Assert.Equal(31, result.OfferId);
        Assert.Equal(TaskStatus.Assigned, result.Status);
        await _history.Received(1).AddAsync(Arg.Is<TaskStatusHistory>(h => h.ChangedByUserId == "admin-1"));
        await _unitOfWork.Received(1).CommitTransactionAsync();
    }

    [Fact]
    public async Task Assign_Throws_WhenTheFreelancerIsNotEligible()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask());
        _eligibility.CheckAsync(1, 5).Returns(TestData.NotEligible(1, 5, "The freelancer is blocked."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AssignAsync(new AssignTaskDto
        {
            TaskId = 1,
            FreelancerProfileId = 5,
            AssignedByUserId = "admin-1"
        }));

        await _tasks.DidNotReceive().TryAssignAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Assign_RequiresTheAdministratorId()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AssignAsync(new AssignTaskDto
        {
            TaskId = 1,
            FreelancerProfileId = 5
        }));
    }

    [Fact]
    public async Task GetCurrent_ReturnsNull_WhenTheTaskHasNoFreelancer()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask());

        var result = await _service.GetCurrentAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrent_ReturnsTheAssignmentAndItsDate()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Assigned, assignedFreelancerId: 5));
        _history.GetLatestAsync(1, TaskStatus.Assigned)
            .Returns(new TaskStatusHistory { TaskId = 1, NewStatus = TaskStatus.Assigned, ChangedAt = TestData.Now.AddDays(-1) });

        var result = await _service.GetCurrentAsync(1);

        Assert.NotNull(result);
        Assert.Equal(5, result!.FreelancerProfileId);
        Assert.Equal(TestData.Now.AddDays(-1), result.AssignedAt);
    }
}
