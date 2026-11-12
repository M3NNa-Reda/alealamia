using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IMarketingPackageItemRepository : IGenericRepository<MarketingPackageItem>
    {
        Task<IEnumerable<MarketingPackageItem>> GetByPackageIdAsync(int packageId);
    }
}