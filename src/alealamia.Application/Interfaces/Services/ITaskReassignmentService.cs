using alealamia.Application.DTOs.TaskAssignments;

namespace alealamia.Application.Interfaces.Services;

public interface ITaskReassignmentService
{
    Task<TaskReassignmentDto> ReassignAsync(CreateTaskReassignmentDto dto);
    Task<TaskReassignmentDto> WithdrawAsync(WithdrawTaskDto dto);
    /// <summary>
    /// The assigned freelancer did not deliver before the final deadline: the task goes back to the pool and the
    /// freelancer is blocked (BR-09). Fails if the task is not assigned or its final deadline has not passed.
    /// </summary>
    Task<TaskReassignmentDto> RecordNoShowAsync(int taskId);
    Task<IEnumerable<TaskReassignmentDto>> GetByTaskAsync(int taskId);
}
