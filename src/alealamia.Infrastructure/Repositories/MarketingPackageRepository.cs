using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class MarketingPackageRepository : GenericRepository<MarketingPackage>, IMarketingPackageRepository
    {
        public MarketingPackageRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MarketingPackage>> GetActivePackagesAsync()
        {
            return await _context.MarketingPackages.AsNoTracking()
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        public async Task<MarketingPackage?> GetWithItemsAsync(int id)
        {
            return await _context.MarketingPackages
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}