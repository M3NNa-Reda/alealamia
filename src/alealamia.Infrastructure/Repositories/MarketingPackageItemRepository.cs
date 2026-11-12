using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class MarketingPackageItemRepository : GenericRepository<MarketingPackageItem>, IMarketingPackageItemRepository
    {
        public MarketingPackageItemRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MarketingPackageItem>> GetByPackageIdAsync(int packageId)
        {
            return await _context.MarketingPackageItems.AsNoTracking()
                .Where(i => i.MarketingPackageId == packageId)
                .ToListAsync();
        }
    }
}