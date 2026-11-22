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

public class TaskReassignmentServiceTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly ITaskReassignmentRepository _reassignments = Substitute.For<ITaskReassignmentRepository>();
    private readonly ITaskOfferRepository _offers = Substitute.For<ITaskOfferRepository>();
    private readonly ITaskStatusHistoryRepository _history = Substitute.For<ITaskStatusHistoryRepository>();
    private readonly ITaskEligibilityRepository _freelancers = Substitute.For<ITaskEligibilityRepository>();
    private readonly ITaskEligibilityService _eligibility = Substitute.For<ITaskEligibilityService>();
    private readonly ITaskOfferService _offerService = Substitute.For<ITaskOfferService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TaskReassignmentService _service;

    public TaskReassignmentServiceTests()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Assigned, assignedFreelancerId: 5));
        _offers.GetOrCreateAcceptedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(new TaskOffer { Id = 40, Status = TaskOfferStatus.Accepted });

        _service = new TaskReassignmentService(
            _tasks,
            _reassignments,
            _offers,
            _history,
            _freelancers,
            _eligibility,
            _offerService,
            _unitOfWork,
            new FixedTimeProvider(TestData.Now));
    }

    private static CreateTaskReassignmentDto Reassign(int? newFreelancerId, string reason = "The freelancer stopped responding.") => new()
    {
        TaskId = 1,
        NewFreelancerProfileId = newFreelancerId,
        Reason = reason,
        ReassignedByUserId = "admin-1"
    };

    [Fact]
    public async Task Reassign_MovesTheTaskToTheNewFreelancerAndKeepsItAssigned()
    {
        _eligibility.CheckAsync(1, 8).Returns(TestData.Eligible(1, 8));
        _tasks.TryReassignAsync(1, 5, 8, TaskStatus.Assigned, TestData.Now).Returns(true);

        var result = await _service.ReassignAsync(Reassign(8));

        Assert.Equal(5, result.PreviousFreelancerId);
        Assert.Equal(8, result.NewFreelancerId);
        Assert.Equal("admin-1", result.ReassignedByUserId);
        await _offers.Received(1).GetOrCreateAcceptedAsync(1, 8, TestData.Now);
        await _reassignments.Received(1).AddAsync(Arg.Is<TaskReassignment>(r => r.PreviousFreelancerId == 5 && r.NewFreelancerId == 8));
        await _history.DidNotReceive().AddAsync(Arg.Any<TaskStatusHistory>());
        await _offerService.DidNotReceive().SendOffersAsync(Arg.Any<int>());
        await _unitOfWork.Received(1).CommitTransactionAsync();
    }

    [Fact]
    public async Task Reassign_WithoutANewFreelancer_ReturnsTheTaskToThePoolAndSendsNewOffers()
    {
        _tasks.TryReassignAsync(1, 5, null, TaskStatus.Published, TestData.Now).Returns(true);

        var result = await _service.ReassignAsync(Reassign(null));

        Assert.Null(result.NewFreelancerId);
        await _history.Received(1).AddAsync(Arg.Is<TaskStatusHistory>(h =>
            h.OldStatus == TaskStatus.Assigned && h.NewStatus == TaskStatus.Published));
        await _offerService.Received(1).SendOffersAsync(1);
        await _offers.DidNotReceive().GetOrCreateAcceptedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Reassign_RollsBack_WhenTheAssignmentChangedMeanwhile()
    {
        _eligibility.CheckAsync(1, 8).Returns(TestData.Eligible(1, 8));
        _tasks.TryReassignAsync(1, 5, 8, TaskStatus.Assigned, TestData.Now).Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReassignAsync(Reassign(8)));

        await _unitOfWork.Received(1).RollbackTransactionAsync();
        await _unitOfWork.DidNotReceive().CommitTransactionAsync();
        await _reassignments.DidNotReceive().AddAsync(Arg.Any<TaskReassignment>());
    }

    [Fact]
    public async Task Reassign_Throws_WhenTheNewFreelancerIsNotEligible()
    {
        _eligibility.CheckAsync(1, 8).Returns(TestData.NotEligible(1, 8, "The freelancer is missing 1 required skill(s)."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReassignAsync(Reassign(8)));

        await _unitOfWork.DidNotReceive().BeginTransactionAsync();
    }

    [Fact]
    public async Task Reassign_Throws_WhenTheTaskIsAlreadyAssignedToThatFreelancer()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReassignAsync(Reassign(5)));
    }

    [Theory]
    [InlineData(TaskStatus.Published)]
    [InlineData(TaskStatus.Submitted)]
    [InlineData(TaskStatus.Completed)]
    public async Task Reassign_Throws_WhenTheTaskIsNotCurrentlyAssigned(TaskStatus status)
    {
        _tasks.GetReadOnlyByIdAsync(2).Returns(TestData.NewTask(id: 2, status: status, assignedFreelancerId: 5));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReassignAsync(new CreateTaskReassignmentDto
        {
            TaskId = 2,
            NewFreelancerProfileId = 8,
            Reason = "Reason",
            ReassignedByUserId = "admin-1"
        }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reassign_RequiresAReason(string reason)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ReassignAsync(Reassign(8, reason)));
    }

    [Fact]
    public async Task Reassign_RequiresTheAdministratorId()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ReassignAsync(new CreateTaskReassignmentDto
        {
            TaskId = 1,
            NewFreelancerProfileId = 8,
            Reason = "Reason"
        }));
    }

    [Fact]
    public async Task Withdraw_ReturnsTheTaskToThePoolAndRecordsTheFreelancerAsTheActor()
    {
        _freelancers.GetFreelancerSnapshotAsync(5, TestData.Now).Returns(TestData.NewFreelancer(id: 5));
        _tasks.TryReassignAsync(1, 5, null, TaskStatus.Published, TestData.Now).Returns(true);

        var result = await _service.WithdrawAsync(new WithdrawTaskDto
        {
            TaskId = 1,
            FreelancerProfileId = 5,
            Reason = "I am no longer available."
        });

        Assert.Equal("user-5", result.ReassignedByUserId);
        Assert.Null(result.NewFreelancerId);
        await _offerService.Received(1).SendOffersAsync(1);
    }

    [Fact]
    public async Task Withdraw_Throws_WhenTheTaskBelongsToAnotherFreelancer()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.WithdrawAsync(new WithdrawTaskDto
        {
            TaskId = 1,
            FreelancerProfileId = 6,
            Reason = "Reason"
        }));
    }

    [Fact]
    public async Task Withdraw_RequiresAReason()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.WithdrawAsync(new WithdrawTaskDto
        {
            TaskId = 1,
            FreelancerProfileId = 5
        }));
    }
}
