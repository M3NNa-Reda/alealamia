using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Infrastructure.Repositories;

public class TaskStatusHistoryRepository : GenericRepository<TaskStatusHistory>, ITaskStatusHistoryRepository
{
    public TaskStatusHistoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<TaskStatusHistory>> GetByTaskIdAsync(int taskId)
    {
        return await _context.TaskStatusHistory.AsNoTracking()
            .Where(h => h.TaskId == taskId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .ToListAsync();
    }

    public async Task<TaskStatusHistory?> GetLatestAsync(int taskId, TaskStatus newStatus)
    {
        return await _context.TaskStatusHistory.AsNoTracking()
            .Where(h => h.TaskId == taskId && h.NewStatus == newStatus)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .FirstOrDefaultAsync();
    }
}
