using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class ProjectStatusHistoryRepository : GenericRepository<ProjectStatusHistory>, IProjectStatusHistoryRepository
    {
        public ProjectStatusHistoryRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<ProjectStatusHistory>> GetByProjectIdAsync(int projectId)
        {
            return await _context.ProjectStatusHistories.AsNoTracking()
                .Where(h => h.ProjectId == projectId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }
    }
}