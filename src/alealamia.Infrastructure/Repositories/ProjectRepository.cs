using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        public ProjectRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Project?> GetDetailsByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.ProjectServices).ThenInclude(ps => ps.Service)
                .Include(p => p.StatusHistory)
                .Include(p => p.Meetings)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Project>> GetByClientProfileIdAsync(int clientProfileId)
        {
            return await _context.Projects.AsNoTracking()
                .Where(p => p.ClientProfileId == clientProfileId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status)
        {
            return await _context.Projects.AsNoTracking()
                .Where(p => p.Status == status)
                .ToListAsync();
        }
    }
}