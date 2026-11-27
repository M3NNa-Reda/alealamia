using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Identity;
using alealamia.Application.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.Interfaces.Services
{
    public interface IIdentityService
    {
        // Implement in infrastructure layer 
        // Users
        Task<ApplicationUserDto?> GetUserByEmailAsync(string email);
        Task<ApplicationUserDto?> GetUserByIdAsync(string userId);
        Task<bool> CheckPasswordAsync(string email, string password);
        Task<UserAccountDto?> GetUserAccountAsync(string userId);
        Task<PagedResultDto<UserAccountDto>> GetUsersAsync(UserListQueryDto query);
        Task<IdentityResultDto> SetPhoneNumberAsync(string userId, string? phoneNumber);
        Task<IdentityResultDto> ChangePasswordAsync(
            string userId, string currentPassword, string newPassword);

        Task<IdentityResultDto> SetActiveStatusAsync(string userId, bool isActive);

        /// <summary>
        /// Validates the credentials and applies the lockout policy: a wrong password counts as a failed attempt,
        /// a correct one resets the counter, and a locked account is refused even with the right password.
        /// </summary>
        Task<CredentialCheckResult> CheckCredentialsAsync(string email, string password);
        Task<IdentityResultDto> CreateUserAsync(CreateUserDto userDto);
        Task<bool> IsActiveAsync(string userId);
        Task<IdentityResultDto> UpdateUserNameAsync(string userId, UpdateUserNameDto dto);

        // Roles
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<bool> IsInRoleAsync(string userId, string role);
        Task<bool> AddToRoleAsync(string userId, string role);
        Task<bool> RemoveFromRoleAsync(string userId, string role);
        Task<bool> RoleExistsAsync(string role);
    }
}
