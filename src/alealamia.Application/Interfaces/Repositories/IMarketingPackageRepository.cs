using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IMarketingPackageRepository : IGenericRepository<MarketingPackage>
    {
        Task<IEnumerable<MarketingPackage>> GetActivePackagesAsync();
        Task<MarketingPackage?> GetWithItemsAsync(int id);
    }
}