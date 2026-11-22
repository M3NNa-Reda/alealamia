using alealamia.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Interfaces.Repositories;

public interface ITaskRequiredSkillRepository
{
    Task<IReadOnlyList<TaskRequiredSkill>> GetByTaskIdAsync(int taskId);
    Task AddRangeAsync(IEnumerable<TaskRequiredSkill> items);
    void DeleteRange(IEnumerable<TaskRequiredSkill> items);
}
