using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class TaskRevisionRepository : GenericRepository<TaskRevision>, ITaskRevisionRepository
    {
        public TaskRevisionRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<TaskRevision>> GetByTaskIdAsync(int id)
        {
            return await _context.TaskRevisions
                       .AsNoTracking()
                       .Where(t => t.TaskId == id)
                       .ToListAsync();
        }
        public async Task<IEnumerable<TaskRevision>> GetByTaskReviewIdAsync(int id)
        {
            return await _context.TaskRevisions
                       .AsNoTracking()
                       .Where(t => t.TaskReviewId == id)
                       .ToListAsync();
        }
        public async Task<IEnumerable<TaskRevision>> GetByStatusAsync(RevisionStatus status)
        {
            return await _context.TaskRevisions
                       .AsNoTracking()
                       .Where(t => t.Status == status)
                       .ToListAsync();
        }
        public async Task<IEnumerable<TaskRevision>> GetByCreatedAtRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.TaskRevisions
                    .AsNoTracking()
                    .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
                    .ToListAsync();
        }
    }
}
