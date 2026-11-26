using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using alealamia.Application.Tests.Support;
using NSubstitute;
using Task = System.Threading.Tasks.Task;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Tests;

public class TaskNoShowServiceTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly ITaskReassignmentService _reassignments = Substitute.For<ITaskReassignmentService>();
    private readonly TaskNoShowService _service;

    public TaskNoShowServiceTests()
    {
        _service = new TaskNoShowService(_tasks, _reassignments, new FixedTimeProvider(TestData.Now));
    }

    private static TaskEntity Overdue(int id) =>
        TestData.NewTask(id: id, status: TaskStatus.Assigned, assignedFreelancerId: 5, deadline: TestData.Now.AddDays(-3));

    [Fact]
    public async Task Process_HandlesEveryOverdueTask()
    {
        _tasks.GetNoShowCandidatesAsync(TestData.Now).Returns(new List<TaskEntity> { Overdue(1), Overdue(2) });
        _reassignments.RecordNoShowAsync(Arg.Any<int>()).Returns(new TaskReassignmentDto());

        var result = await _service.ProcessAsync();

        Assert.Equal(new[] { 1, 2 }, result.ProcessedTaskIds);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task Process_OneFailingTask_DoesNotStopTheOthers()
    {
        _tasks.GetNoShowCandidatesAsync(TestData.Now).Returns(new List<TaskEntity> { Overdue(1), Overdue(2), Overdue(3) });
        _reassignments.RecordNoShowAsync(Arg.Any<int>()).Returns(new TaskReassignmentDto());
        _reassignments.RecordNoShowAsync(2)
            .Returns(Task.FromException<TaskReassignmentDto>(new InvalidOperationException("The task assignment was changed by someone else.")));

        var result = await _service.ProcessAsync();

        Assert.Equal(new[] { 1, 3 }, result.ProcessedTaskIds);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(2, failure.TaskId);
        Assert.Contains("changed by someone else", failure.Error);
    }

    [Fact]
    public async Task Process_WithNothingOverdue_DoesNothing()
    {
        _tasks.GetNoShowCandidatesAsync(TestData.Now).Returns(new List<TaskEntity>());

        var result = await _service.ProcessAsync();

        Assert.Empty(result.ProcessedTaskIds);
        await _reassignments.DidNotReceive().RecordNoShowAsync(Arg.Any<int>());
    }
}
