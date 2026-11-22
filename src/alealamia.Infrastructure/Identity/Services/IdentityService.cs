using alealamia.Application.DTOs.Identity;
using alealamia.Application.Interfaces.Services;
using alealamia.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace alealamia.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public async Task<ApplicationUserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return null;
            return MapToDto(user);
        }

        public async Task<ApplicationUserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;
            return MapToDto(user);
        }

        public async Task<bool> CheckPasswordAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<IdentityResultDto> CreateUserAsync(CreateUserDto userDto)
        {
            var user = new ApplicationUser
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                UserName = userDto.Email,
                Email = userDto.Email,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, userDto.Password);

            return new IdentityResultDto
            {
                Succeeded = result.Succeeded,
                UserId = result.Succeeded ? user.Id : null,
                Errors = result.Errors
                    .Select(e => e.Description)
                    .ToList()
            };
        }

        public async Task<bool> IsActiveAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return false;

            return user.IsActive;
        }
        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return new List<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> IsInRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return false;

            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<bool> AddToRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return false;

            if (!await _roleManager.RoleExistsAsync(role))
                return false;

            var result = await _userManager.AddToRoleAsync(
                user,
                role);

            return result.Succeeded;
        }

        public async Task<bool> RemoveFromRoleAsync(
            string userId,
            string role)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return false;

            if (!await _userManager.IsInRoleAsync(user, role))
                return false;

            var result = await _userManager.RemoveFromRoleAsync(
                user,
                role);

            return result.Succeeded;
        }

        public async Task<bool> RoleExistsAsync(string role)
        {
            return await _roleManager.RoleExistsAsync(role);
        }


        private static ApplicationUserDto MapToDto(ApplicationUser user)
        {
            return new ApplicationUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                FullName = string.Concat(user.FirstName, " ", user.LastName),
                EmailConfirmed = user.EmailConfirmed
            };
        }
    }
}
