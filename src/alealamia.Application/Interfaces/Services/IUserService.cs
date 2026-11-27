using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Users;

namespace alealamia.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<UserAccountDto?> GetMyAccountAsync(string userId);
        Task<ResultDto> UpdateMyAccountAsync(string userId, UpdateMyAccountDto dto);
        Task<ResultDto> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<ResultDto> DeactivateMyAccountAsync(string userId, DeactivateMyAccountDto dto);
        Task<PagedResultDto<UserAccountDto>> GetAllUsersAsync(UserListQueryDto query);
        Task<UserAccountDto?> GetUserByIdAsync(string userId);
        Task<ResultDto> UpdateUserStatusAsync(
            string adminUserId, string targetUserId, UpdateUserStatusDto dto);
    }
}