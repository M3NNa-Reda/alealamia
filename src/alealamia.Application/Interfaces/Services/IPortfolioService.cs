using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Portfolio;

namespace alealamia.Application.Interfaces.Services
{
    public interface IPortfolioService
    {
        Task<ResultDto> CreatePortfolioItemAsync(
            int freelancerProfileId,
            PortfolioItemDto dto);

        Task<ResultDto> UpdatePortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId,
            PortfolioItemDto dto);

        Task<ResultDto> DeletePortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId);

        Task<PortfolioItemDetailsDto?> GetPortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId);

        Task<IEnumerable<PortfolioItemDetailsDto>> GetMyPortfolioAsync(
            int freelancerProfileId);

        Task<ResultDto> AddFilesAsync(
            int freelancerProfileId,
            int portfolioItemId,
            IEnumerable<int> fileIds);

        Task<ResultDto> RemoveFileAsync(
            int freelancerProfileId,
            int portfolioItemId,
            int fileId);

        Task<ResultDto> PublishPortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId);

        Task<ResultDto> UnpublishPortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId);
    }
}
