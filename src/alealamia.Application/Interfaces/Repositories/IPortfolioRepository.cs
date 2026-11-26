using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IPortfolioRepository : IGenericRepository<PortfolioItem>
    {
        Task<PortfolioItem?> GetByIdWithFilesAsync(int portfolioItemId);

        Task<IEnumerable<PortfolioItem>> GetByFreelancerIdWithFilesAsync(int freelancerProfileId);

        Task<bool> AreFilesOwnedByUserAsync(IEnumerable<int> fileIds, string userId);
    }
}