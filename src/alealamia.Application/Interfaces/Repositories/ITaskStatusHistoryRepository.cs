using alealamia.Domain.Entities;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Interfaces.Repositories;

public interface ITaskStatusHistoryRepository : IGenericRepository<TaskStatusHistory>
{
    Task<IReadOnlyList<TaskStatusHistory>> GetByTaskIdAsync(int taskId);
    Task<TaskStatusHistory?> GetLatestAsync(int taskId, TaskStatus newStatus);
}
