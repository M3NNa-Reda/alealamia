using alealamia.Application.DTOs.MarketingPackages;

namespace alealamia.Application.Interfaces.Services
{
    public interface IProjectMarketingPackageService
    {
        Task<ProjectMarketingPackageDto> PurchaseAsync(PurchaseMarketingPackageDto dto);
        Task<ProjectMarketingPackageDto?> GetByIdAsync(int id);
        Task<IEnumerable<ProjectMarketingPackageDto>> GetByProjectAsync(int projectId);
        Task CancelAsync(int id);
    }
}