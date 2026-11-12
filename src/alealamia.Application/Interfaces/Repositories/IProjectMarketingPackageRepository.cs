using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IProjectMarketingPackageRepository : IGenericRepository<ProjectMarketingPackage>
    {
        Task<IEnumerable<ProjectMarketingPackage>> GetByProjectIdAsync(int projectId);
        Task<ProjectMarketingPackage?> GetWithItemsAsync(int id);
    }
}