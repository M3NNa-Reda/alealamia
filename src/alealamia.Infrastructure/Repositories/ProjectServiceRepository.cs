using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class ProjectServiceRepository : GenericRepository<ProjectService>, IProjectServiceRepository
    {
        public ProjectServiceRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<ProjectService>> GetByProjectIdAsync(int projectId)
        {
            return await _context.ProjectServices.AsNoTracking()
                .Include(ps => ps.Service)
                .Where(ps => ps.ProjectId == projectId)
                .ToListAsync();
        }

        public async Task<ProjectService?> GetByProjectAndServiceAsync(int projectId, int serviceId)
        {
            return await _context.ProjectServices
                .FirstOrDefaultAsync(ps => ps.ProjectId == projectId && ps.ServiceId == serviceId);
        }
    }
}