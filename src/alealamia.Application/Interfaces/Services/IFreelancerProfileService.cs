using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Freelancer;

namespace alealamia.Application.Interfaces.Services
{
    public interface IFreelancerProfileService
    {
        Task<ResultDto> CompleteProfileAsync(string userId, CompleteFreelancerProfileDto dto);
        Task<ResultDto> UpdateProfileAsync(string userId, UpdateFreelancerProfileDto dto);
        Task<FreelancerDetailsDto?> GetFreelancerProfileAsync(string userId);
        Task<FreelancerStatusDto?> GetFreelancerStatusAsync(string userId);
        Task<ResultDto> ChangeTrackAsync(string userId, ChangeFreelancerTrackDto dto);
        Task<ResultDto> SetAvailabilityAsync(string userId, SetAvailabilityDto dto);

    }
}
