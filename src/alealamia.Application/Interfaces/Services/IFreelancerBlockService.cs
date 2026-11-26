using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.FreelancerBlock;

namespace alealamia.Application.Interfaces.Services
{
    public interface IFreelancerBlockService
    {
        Task<ResultDto> CreateBlockAsync(CreateFreelancerBlockDto dto);

        Task<IEnumerable<FreelancerBlockDto>> GetActiveBlocksByFreelancerIdAsync(
            int freelancerProfileId);

        Task<IEnumerable<FreelancerBlockDto>> GetAllActiveBlocksAsync();

        Task<IEnumerable<FreelancerBlockDto>> GetBlockHistoryByFreelancerIdAsync(
            int freelancerProfileId);
    }
}