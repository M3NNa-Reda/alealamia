using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Identity;
using alealamia.Application.DTOs.Users;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using System.Text.RegularExpressions;

namespace alealamia.Application.Services
{
    public class UserService : IUserService
    {
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 20;

        private const string UserNotFoundMessage = "User does not exist.";
        private const string WrongPasswordMessage = "Current password is incorrect.";

        private static readonly Regex PhoneRegex = new(@"^\+?[0-9]{8,15}$", RegexOptions.Compiled);

        private readonly IIdentityService _identityService;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(
            IIdentityService identityService,
            IRefreshTokenRepository refreshTokens,
            IUnitOfWork unitOfWork)
        {
            _identityService = identityService;
            _refreshTokens = refreshTokens;
            _unitOfWork = unitOfWork;
        }

      
        public Task<UserAccountDto?> GetMyAccountAsync(string userId)
            => _identityService.GetUserAccountAsync(userId);

        public async Task<ResultDto> UpdateMyAccountAsync(
            string userId,
            UpdateMyAccountDto dto)
        {
            var firstName = dto.FirstName?.Trim();
            var lastName = dto.LastName?.Trim();

            if (string.IsNullOrWhiteSpace(firstName))
                return Failed("First name is required.");

            if (string.IsNullOrWhiteSpace(lastName))
                return Failed("Last name is required.");

            var phone = string.IsNullOrWhiteSpace(dto.PhoneNumber)
                ? null
                : dto.PhoneNumber.Trim();

            if (phone != null && !PhoneRegex.IsMatch(phone))
                return Failed("Phone number is invalid.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var nameResult = await _identityService.UpdateUserNameAsync(
                    userId,
                    new UpdateUserNameDto
                    {
                        FirstName = firstName,
                        LastName = lastName
                    });

                if (!nameResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Failed(nameResult.Errors);
                }

                var phoneResult = await _identityService.SetPhoneNumberAsync(userId, phone);

                if (!phoneResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Failed(phoneResult.Errors);
                }

                await _unitOfWork.CommitTransactionAsync();

                return Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ResultDto> ChangePasswordAsync(
            string userId,
            ChangePasswordDto dto)
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword))
                return Failed("Current password is required.");

            if (string.IsNullOrEmpty(dto.NewPassword))
                return Failed("New password is required.");

            if (dto.CurrentPassword == dto.NewPassword)
                return Failed("The new password must be different from the current password.");

            var user = await _identityService.GetUserByIdAsync(userId);

            if (user == null)
                return Failed(UserNotFoundMessage);

            var verification = await VerifyCurrentPasswordAsync(user, dto.CurrentPassword);

            if (verification != null)
                return verification;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var result = await _identityService.ChangePasswordAsync(
                    userId, dto.CurrentPassword, dto.NewPassword);

                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Failed(result.Errors);
                }

                await _refreshTokens.RevokeAllForUserAsync(userId, DateTime.UtcNow);

                await _unitOfWork.CommitTransactionAsync();

                return Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ResultDto> DeactivateMyAccountAsync(
            string userId,
            DeactivateMyAccountDto dto)
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword))
                return Failed("Current password is required.");

            var user = await _identityService.GetUserByIdAsync(userId);

            if (user == null)
                return Failed(UserNotFoundMessage);

            if (await _identityService.IsInRoleAsync(userId, AppRoles.Admin))
                return Failed("Admin accounts cannot be deactivated from here.");

            var verification = await VerifyCurrentPasswordAsync(user, dto.CurrentPassword);

            if (verification != null)
                return verification;

            return await SetStatusAndRevokeAsync(userId, isActive: false);
        }

        
        public Task<PagedResultDto<UserAccountDto>> GetAllUsersAsync(UserListQueryDto query)
        {
            var normalized = new UserListQueryDto
            {
                Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
                Role = string.IsNullOrWhiteSpace(query.Role) ? null : query.Role.Trim(),
                IsActive = query.IsActive,
                PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber,
                PageSize = query.PageSize < 1
                    ? DefaultPageSize
                    : Math.Min(query.PageSize, MaxPageSize)
            };

            return _identityService.GetUsersAsync(normalized);
        }

        public Task<UserAccountDto?> GetUserByIdAsync(string userId)
            => _identityService.GetUserAccountAsync(userId);

        public async Task<ResultDto> UpdateUserStatusAsync(
            string adminUserId,
            string targetUserId,
            UpdateUserStatusDto dto)
        {
            if (adminUserId == targetUserId)
                return Failed("You cannot change the status of your own account.");

            var user = await _identityService.GetUserByIdAsync(targetUserId);

            if (user == null)
                return Failed(UserNotFoundMessage);

            var currentlyActive = await _identityService.IsActiveAsync(targetUserId);

            if (currentlyActive == dto.IsActive)
                return Success();

            return await SetStatusAndRevokeAsync(targetUserId, dto.IsActive);
        }

       
        private async Task<ResultDto> SetStatusAndRevokeAsync(string userId, bool isActive)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var result = await _identityService.SetActiveStatusAsync(userId, isActive);

                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Failed(result.Errors);
                }

                if (!isActive)
                {
                    await _refreshTokens.RevokeAllForUserAsync(userId, DateTime.UtcNow);
                }

                await _unitOfWork.CommitTransactionAsync();

                return Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task<ResultDto?> VerifyCurrentPasswordAsync(
            ApplicationUserDto user,
            string password)
        {
            var check = await _identityService.CheckCredentialsAsync(user.Email, password);

            if (check == CredentialCheckResult.LockedOut)
                return Failed(AuthService.LockedOutMessage);

            if (check != CredentialCheckResult.Success)
                return Failed(WrongPasswordMessage);

            return null;
        }

        private static ResultDto Success() => new ResultDto { Succeeded = true };

        private static ResultDto Failed(string error) =>
            new ResultDto
            {
                Succeeded = false,
                Errors = new List<string> { error }
            };

        private static ResultDto Failed(IEnumerable<string> errors) =>
            new ResultDto
            {
                Succeeded = false,
                Errors = errors.ToList()
            };
    }
}