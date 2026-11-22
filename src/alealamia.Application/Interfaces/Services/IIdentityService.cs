using alealamia.Application.DTOs.Identity;
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
        Task<IdentityResultDto> CreateUserAsync(CreateUserDto userDto);
        Task<bool> IsActiveAsync(string userId);

        // Roles
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<bool> IsInRoleAsync(string userId, string role);
        Task<bool> AddToRoleAsync(string userId, string role);
        Task<bool> RemoveFromRoleAsync(string userId, string role);
        Task<bool> RoleExistsAsync(string role);
    }
}
