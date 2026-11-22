using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories;

public interface ITaskReassignmentRepository : IGenericRepository<TaskReassignment>
{
    Task<IReadOnlyList<TaskReassignment>> GetByTaskIdAsync(int taskId);
}
