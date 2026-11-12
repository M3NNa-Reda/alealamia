using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class ProjectMarketingPackageRepository : GenericRepository<ProjectMarketingPackage>, IProjectMarketingPackageRepository
    {
        public ProjectMarketingPackageRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<ProjectMarketingPackage>> GetByProjectIdAsync(int projectId)
        {
            return await _context.ProjectMarketingPackages.AsNoTracking()
                .Include(p => p.MarketingPackage)
                .Where(p => p.ProjectId == projectId)
                .ToListAsync();
        }

        public async Task<ProjectMarketingPackage?> GetWithItemsAsync(int id)
        {
            return await _context.ProjectMarketingPackages
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}