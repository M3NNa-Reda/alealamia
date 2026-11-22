using alealamia.Application.DTOs.TaskAssignments;

namespace alealamia.Application.Interfaces.Services;

public interface ITaskAssignmentService
{
    Task<TaskAssignmentResultDto> AcceptOfferAsync(int offerId, int freelancerProfileId);
    Task<TaskAssignmentResultDto> AssignAsync(AssignTaskDto dto);
    Task<TaskAssignmentDto?> GetCurrentAsync(int taskId);
}
