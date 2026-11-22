using alealamia.Application.DTOs.TaskAssignments;

namespace alealamia.Application.Interfaces.Services;

public interface ITaskReassignmentService
{
    Task<TaskReassignmentDto> ReassignAsync(CreateTaskReassignmentDto dto);
    Task<TaskReassignmentDto> WithdrawAsync(WithdrawTaskDto dto);
    Task<IEnumerable<TaskReassignmentDto>> GetByTaskAsync(int taskId);
}
