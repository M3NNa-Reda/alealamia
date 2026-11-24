using alealamia.Application.DTOs.Admin;
using alealamia.Application.DTOs.Common;


namespace alealamia.Application.Interfaces.Services
{
    public interface IAdminFreelancerService
    {
        Task<IEnumerable<FreelancerReviewListDto>> GetPendingFreelancersAsync();
        Task<FreelancerReviewDetailsDto?> GetFreelancerDetailsAsync(int id);
        Task<ResultDto> ApproveFreelancerAsync(int freelancerId);
        Task<ResultDto> RejectFreelancerAsync(int freelancerId);
        Task<ResultDto> UpdateFreelancerSkillLevelAsync(int freelancerId, int skillId, UpdateFreelancerSkillLevelDto dto);
    }
}
