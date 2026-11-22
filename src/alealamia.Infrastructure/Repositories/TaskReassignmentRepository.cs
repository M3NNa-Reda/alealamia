using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories;

public class TaskReassignmentRepository : GenericRepository<TaskReassignment>, ITaskReassignmentRepository
{
    public TaskReassignmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<TaskReassignment>> GetByTaskIdAsync(int taskId)
    {
        return await _context.TaskReassignments.AsNoTracking()
            .Where(r => r.TaskId == taskId)
            .OrderByDescending(r => r.ReassignedAt)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }
}
