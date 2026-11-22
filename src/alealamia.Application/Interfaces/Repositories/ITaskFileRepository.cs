using alealamia.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Interfaces.Repositories;

public interface ITaskFileRepository
{
    Task<IReadOnlyList<TaskFile>> GetByTaskIdAsync(int taskId);
    Task<TaskFile?> GetAsync(int taskId, int fileId);
    Task AddAsync(TaskFile taskFile);
    void Delete(TaskFile taskFile);
}
