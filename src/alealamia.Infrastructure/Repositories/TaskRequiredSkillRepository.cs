using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Infrastructure.Repositories;

public class TaskRequiredSkillRepository : ITaskRequiredSkillRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRequiredSkillRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TaskRequiredSkill>> GetByTaskIdAsync(int taskId)
    {
        return await _context.TaskRequiredSkills
            .Include(s => s.Skill)
            .Where(s => s.TaskId == taskId)
            .OrderBy(s => s.SkillId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<TaskRequiredSkill> items)
    {
        await _context.TaskRequiredSkills.AddRangeAsync(items);
    }

    public void DeleteRange(IEnumerable<TaskRequiredSkill> items)
    {
        _context.TaskRequiredSkills.RemoveRange(items);
    }
}
