using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Interfaces.Repositories;

public interface ITaskRepository : IGenericRepository<TaskEntity>
{
    Task<TaskEntity?> GetReadOnlyByIdAsync(int id);
    Task<TaskEntity?> GetDetailsByIdAsync(int id);
    Task<IReadOnlyList<TaskEntity>> GetByProjectIdAsync(int projectId);
    Task<IReadOnlyList<TaskEntity>> GetByFreelancerIdAsync(int freelancerProfileId);
    Task<(IReadOnlyList<TaskEntity> Items, int TotalCount)> SearchAsync(
        TaskStatus? status,
        int? projectId,
        int? trackId,
        int? assignedFreelancerId,
        int page,
        int pageSize);
    Task<IReadOnlyList<TaskEntity>> GetOverdueAsync(DateTime utcNow);
    /// <summary>Assigned tasks whose final deadline (extension, or published deadline plus the grace days) has passed.</summary>
    Task<IReadOnlyList<TaskEntity>> GetNoShowCandidatesAsync(DateTime utcNow);
    Task<IReadOnlyList<TaskEntity>> GetDueBetweenAsync(DateTime fromUtc, DateTime toUtc);
    Task<bool> TryChangeStatusAsync(int taskId, TaskStatus expectedStatus, TaskStatus newStatus, DateTime utcNow);
    Task<bool> TryAssignAsync(int taskId, int freelancerProfileId, DateTime utcNow);
    Task<bool> TryReassignAsync(int taskId, int previousFreelancerProfileId, int? newFreelancerProfileId, TaskStatus resultingStatus, DateTime utcNow);
    Task<bool> HasActiveTasksAsync(int freelancerProfileId);
}
