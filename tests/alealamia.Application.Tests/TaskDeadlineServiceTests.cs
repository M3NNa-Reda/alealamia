using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Services;
using alealamia.Application.Tests.Support;
using alealamia.Domain.Entities;
using NSubstitute;
using Task = System.Threading.Tasks.Task;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Tests;

public class TaskDeadlineServiceTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly ITaskStatusHistoryRepository _history = Substitute.For<ITaskStatusHistoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TaskDeadlineService _service;

    public TaskDeadlineServiceTests()
    {
        _service = new TaskDeadlineService(_tasks, _history, _unitOfWork, new FixedTimeProvider(TestData.Now));
    }

    [Fact]
    public async Task Get_FlagsARunningTaskPastItsDeadlineAsOverdue()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(
            TestData.NewTask(status: TaskStatus.Assigned, assignedFreelancerId: 5, deadline: TestData.Now.AddHours(-3)));

        var result = await _service.GetAsync(1);

        Assert.True(result.IsOverdue);
        Assert.Equal(-3, result.HoursRemaining);
    }

    [Fact]
    public async Task Get_UsesTheExtensionDeadlineWhenThereIsOne()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(
            status: TaskStatus.Assigned,
            assignedFreelancerId: 5,
            deadline: TestData.Now.AddHours(-3),
            extension: TestData.Now.AddDays(2)));

        var result = await _service.GetAsync(1);

        Assert.False(result.IsOverdue);
        Assert.Equal(TestData.Now.AddDays(2), result.EffectiveDeadline);
    }

    [Theory]
    [InlineData(TaskStatus.Draft)]
    [InlineData(TaskStatus.Published)]
    [InlineData(TaskStatus.Completed)]
    [InlineData(TaskStatus.Cancelled)]
    public async Task Get_NeverFlagsATaskThatIsNotRunningAsOverdue(TaskStatus status)
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: status, deadline: TestData.Now.AddDays(-4)));

        var result = await _service.GetAsync(1);

        Assert.False(result.IsOverdue);
        Assert.Null(result.HoursRemaining);
    }

    [Fact]
    public async Task Get_Throws_WhenTheTaskDoesNotExist()
    {
        _tasks.GetReadOnlyByIdAsync(99).Returns((TaskEntity?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetAsync(99));
    }

    [Fact]
    public async Task GetOverdue_ReturnsTheOverdueTasks()
    {
        _tasks.GetOverdueAsync(TestData.Now).Returns(new List<TaskEntity>
        {
            TestData.NewTask(id: 1, status: TaskStatus.Assigned, assignedFreelancerId: 5, deadline: TestData.Now.AddDays(-1)),
            TestData.NewTask(id: 2, status: TaskStatus.RevisionRequested, assignedFreelancerId: 6, deadline: TestData.Now.AddHours(-2))
        });

        var result = (await _service.GetOverdueAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsOverdue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(169)]
    public async Task GetDueSoon_RejectsAnInvalidWindow(int hours)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetDueSoonAsync(hours));
    }

    [Fact]
    public async Task GetDueSoon_QueriesTheRequestedWindow()
    {
        _tasks.GetDueBetweenAsync(TestData.Now, TestData.Now.AddHours(24)).Returns(new List<TaskEntity>());

        var result = await _service.GetDueSoonAsync(24);

        Assert.Empty(result);
        await _tasks.Received(1).GetDueBetweenAsync(TestData.Now, TestData.Now.AddHours(24));
    }

    [Fact]
    public async Task Extend_SetsTheExtensionDeadlineAndRecordsIt()
    {
        var task = TestData.NewTask(status: TaskStatus.Assigned, assignedFreelancerId: 5, deadline: TestData.Now.AddDays(1));
        _tasks.GetByIdAsync(1).Returns(task);
        var newDeadline = TestData.Now.AddDays(4);

        var result = await _service.ExtendAsync(1, new ExtendTaskDeadlineDto
        {
            NewDeadline = newDeadline,
            Reason = "Client sent new material.",
            ExtendedByUserId = "admin-1"
        });

        Assert.Equal(newDeadline, result.ExtensionDeadline);
        Assert.Equal(newDeadline, task.ExtensionDeadline);
        await _history.Received(1).AddAsync(Arg.Is<TaskStatusHistory>(h =>
            h.ChangedByUserId == "admin-1" && h.Notes!.Contains("Client sent new material.")));
        await _unitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Extend_Throws_WhenTheNewDeadlineIsNotLaterThanTheCurrentOne()
    {
        _tasks.GetByIdAsync(1).Returns(TestData.NewTask(
            status: TaskStatus.Assigned,
            assignedFreelancerId: 5,
            deadline: TestData.Now.AddDays(1),
            extension: TestData.Now.AddDays(3)));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.ExtendAsync(1, new ExtendTaskDeadlineDto
        {
            NewDeadline = TestData.Now.AddDays(2),
            ExtendedByUserId = "admin-1"
        }));

        await _unitOfWork.DidNotReceive().CompleteAsync();
    }

    [Theory]
    [InlineData(TaskStatus.Published)]
    [InlineData(TaskStatus.Submitted)]
    [InlineData(TaskStatus.Completed)]
    public async Task Extend_Throws_WhenTheTaskIsNotInProgress(TaskStatus status)
    {
        _tasks.GetByIdAsync(1).Returns(TestData.NewTask(status: status, assignedFreelancerId: 5));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ExtendAsync(1, new ExtendTaskDeadlineDto
        {
            NewDeadline = TestData.Now.AddDays(9),
            ExtendedByUserId = "admin-1"
        }));
    }

    [Fact]
    public async Task Extend_RequiresTheUserId()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ExtendAsync(1, new ExtendTaskDeadlineDto
        {
            NewDeadline = TestData.Now.AddDays(9)
        }));
    }

    [Fact]
    public async Task RecordActualDuration_RoundsPartialDaysUp()
    {
        var task = TestData.NewTask(status: TaskStatus.Completed, assignedFreelancerId: 5);
        _tasks.GetByIdAsync(1).Returns(task);
        _history.GetLatestAsync(1, TaskStatus.Assigned)
            .Returns(new TaskStatusHistory { ChangedAt = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc) });
        _history.GetLatestAsync(1, TaskStatus.Completed)
            .Returns(new TaskStatusHistory { ChangedAt = new DateTime(2026, 11, 3, 22, 0, 0, DateTimeKind.Utc) });

        var result = await _service.RecordActualDurationAsync(1);

        Assert.Equal(3, result.ActualDurationDays);
        Assert.Equal(3, task.ActualDurationDays);
        await _unitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task RecordActualDuration_IsAtLeastOneDay()
    {
        var task = TestData.NewTask(status: TaskStatus.Completed, assignedFreelancerId: 5);
        _tasks.GetByIdAsync(1).Returns(task);
        _history.GetLatestAsync(1, TaskStatus.Assigned)
            .Returns(new TaskStatusHistory { ChangedAt = TestData.Now.AddHours(-1) });
        _history.GetLatestAsync(1, TaskStatus.Completed)
            .Returns(new TaskStatusHistory { ChangedAt = TestData.Now });

        var result = await _service.RecordActualDurationAsync(1);

        Assert.Equal(1, result.ActualDurationDays);
    }

    [Fact]
    public async Task RecordActualDuration_Throws_WhenTheTaskIsNotCompleted()
    {
        _tasks.GetByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Assigned, assignedFreelancerId: 5));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RecordActualDurationAsync(1));
    }
}
