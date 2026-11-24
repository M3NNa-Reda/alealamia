
using alealamia.Application.DTOs.Client;
using alealamia.Application.DTOs.Common;

namespace alealamia.Application.Interfaces.Services
{
    public interface IClientProfileService
    {
        Task<ResultDto> CompleteProfileAsync(
            string userId,
            ClientProfileDto dto);

        Task<ClientProfileDetailsDto?> GetClientProfileAsync(
            string userId);

        Task<ResultDto> UpdateProfileAsync(
            string userId,
            ClientProfileDto dto);
    }
}
