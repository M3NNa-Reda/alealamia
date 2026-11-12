using alealamia.Application.DTOs.MarketingPackages;

namespace alealamia.Application.Interfaces.Services
{
    public interface IMarketingPackageService
    {
        Task<IEnumerable<MarketingPackageDto>> GetAllAsync(bool activeOnly = false);
        Task<MarketingPackageDto?> GetByIdAsync(int id);
        Task<MarketingPackageDto> CreateAsync(CreateMarketingPackageDto dto);
        Task<MarketingPackageDto> UpdateAsync(int id, UpdateMarketingPackageDto dto);
        Task DeactivateAsync(int id);

        Task<MarketingPackageItemDto> AddItemAsync(CreateMarketingPackageItemDto dto);
        Task<MarketingPackageItemDto> UpdateItemAsync(int itemId, UpdateMarketingPackageItemDto dto);
        Task DeactivateItemAsync(int itemId);
    }
}