using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Identity;
using alealamia.Application.DTOs.Users;
using alealamia.Application.Interfaces.Services;
using alealamia.Infrastructure.Identity.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
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

        public async Task<CredentialCheckResult> CheckCredentialsAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return CredentialCheckResult.InvalidCredentials;

            if (await _userManager.IsLockedOutAsync(user))
                return CredentialCheckResult.LockedOut;

            if (await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                return CredentialCheckResult.Success;
            }

            // UserManager.CheckPasswordAsync does not count failures, so the attempt is registered here.
            // The call that reaches the limit also locks the account.
            await _userManager.AccessFailedAsync(user);

            return await _userManager.IsLockedOutAsync(user)
                ? CredentialCheckResult.LockedOut
                : CredentialCheckResult.InvalidCredentials;
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
        public async Task<IdentityResultDto> UpdateUserNameAsync(string userId, UpdateUserNameDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new IdentityResultDto
                {
                    Succeeded = false,
                    Errors = new List<string> { "User does not exist." }
                };
            }

            if (string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastName))
            {
                return new IdentityResultDto
                {
                    Succeeded = false,
                    Errors = new List<string> { "First name and last name are required." }
                };
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            return new IdentityResultDto
            {
                Succeeded = result.Succeeded,
                UserId = result.Succeeded ? user.Id : null,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
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

        public async Task<UserAccountDto?> GetUserAccountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            return MapToAccountDto(user, roles);
        }

        public async Task<PagedResultDto<UserAccountDto>> GetUsersAsync(UserListQueryDto query)
        {
            IQueryable<ApplicationUser> users = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();

                users = users.Where(u =>
                    (u.FirstName + " " + u.LastName).Contains(term) ||
                    (u.Email != null && u.Email.Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            if (query.IsActive.HasValue)
            {
                var isActive = query.IsActive.Value;
                users = users.Where(u => u.IsActive == isActive);
            }

            if (!string.IsNullOrWhiteSpace(query.Role))
            {
                var normalizedRole = query.Role.Trim().ToUpperInvariant();

                var roleId = await _roleManager.Roles
                    .Where(r => r.NormalizedName == normalizedRole)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (roleId == null)
                {
                    return new PagedResultDto<UserAccountDto>
                    {
                        PageNumber = query.PageNumber,
                        PageSize = query.PageSize,
                        TotalCount = 0
                    };
                }

                users = users.Where(u =>
                    _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
            }

            var totalCount = await users.CountAsync();

            var page = await users
                .OrderByDescending(u => u.CreatedAt)
                .ThenBy(u => u.Id)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var ids = page.Select(u => u.Id).ToList();

            var roleRows = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where ids.Contains(ur.UserId)
                select new { ur.UserId, r.Name })
                .ToListAsync();

            var rolesByUser = roleRows
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => (IList<string>)g.Select(x => x.Name!).ToList());

            return new PagedResultDto<UserAccountDto>
            {
                Items = page
                    .Select(u => MapToAccountDto(
                        u,
                        rolesByUser.TryGetValue(u.Id, out var roles)
                            ? roles
                            : new List<string>()))
                    .ToList(),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IdentityResultDto> SetPhoneNumberAsync(string userId, string? phoneNumber)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return IdentityFailure("User does not exist.");

            if (user.PhoneNumber == phoneNumber)
                return new IdentityResultDto { Succeeded = true, UserId = user.Id };

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.SetPhoneNumberAsync(user, phoneNumber);

            return ToIdentityResultDto(result, user.Id);
        }

        public async Task<IdentityResultDto> ChangePasswordAsync(
            string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return IdentityFailure("User does not exist.");

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.ChangePasswordAsync(
                user, currentPassword, newPassword);

            return ToIdentityResultDto(result, user.Id);
        }

        public async Task<IdentityResultDto> SetActiveStatusAsync(string userId, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return IdentityFailure("User does not exist.");

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            return ToIdentityResultDto(result, user.Id);
        }

        private static IdentityResultDto IdentityFailure(string error) =>
            new IdentityResultDto
            {
                Succeeded = false,
                Errors = new List<string> { error }
            };

        private static IdentityResultDto ToIdentityResultDto(IdentityResult result, string userId) =>
            new IdentityResultDto
            {
                Succeeded = result.Succeeded,
                UserId = result.Succeeded ? userId : null,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };

        private static UserAccountDto MapToAccountDto(ApplicationUser user, IList<string> roles) =>
            new UserAccountDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = string.Concat(user.FirstName, " ", user.LastName).Trim(),
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = roles
            };
        private static ApplicationUserDto MapToDto(ApplicationUser user)
        {
            return new ApplicationUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                FullName = string.Concat(user.FirstName, " ", user.LastName),
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailConfirmed = user.EmailConfirmed
            };
        }
    }
}
