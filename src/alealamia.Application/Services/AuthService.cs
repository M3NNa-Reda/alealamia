using alealamia.Application.DTOs.Auth;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Identity;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using alealamia.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace alealamia.Application.Services
{
    public class AuthService : IAuthService
    {
        public const string InvalidCredentialsMessage = "Invalid email or password.";
        public const string LockedOutMessage = "Too many failed attempts. The account is temporarily locked, please try again later.";
        public const string InvalidRefreshTokenMessage = "Invalid or expired refresh token.";
        public const string RefreshFailedMessage = "Token refresh failed. Please try again.";
        public const string LoginFailedMessage = "Login failed. Please try again.";
        public const string RegistrationFailedMessage = "Registration failed. Please try again.";
        public const string LogoutFailedMessage = "Logout failed. Please try again.";

        private readonly IIdentityService _identityService;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IIdentityService identityService,
            IJwtService jwtService,
            IUnitOfWork unitOfWork,
            IRefreshTokenRepository refreshTokens,
            ILogger<AuthService> logger)
        {
            _identityService = identityService;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
            _refreshTokens = refreshTokens;
            _logger = logger;
        }

        public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto dto)
        {
            if (dto.Role != AppRoles.Client &&
                dto.Role != AppRoles.Freelancer)
            {
                return Failed(new[] { "Invalid account type." });
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var result = await CreateUserAsync(
                    dto.FirstName,
                    dto.LastName,
                    dto.Email,
                    dto.Password);

                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Failed(result.Errors);
                }

                var roleAdded = await _identityService.AddToRoleAsync(
                    result.UserId!,
                    dto.Role);

                if (!roleAdded)
                {
                    throw new InvalidOperationException(
                        "Failed to assign user role.");
                }

                var response = await GenerateAuthResponseAsync(result.UserId!);

                await _unitOfWork.CommitTransactionAsync();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed.");

                await SafeRollbackAsync();

                return Failed(new[] { RegistrationFailedMessage });
            }
        }

        private async Task<IdentityResultDto> CreateUserAsync(
            string firstName,
            string lastName,
            string email,
            string password)
        {
            return await _identityService.CreateUserAsync(
                new CreateUserDto
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Password = password
                });
        }

       
        public async Task<AuthResultDto> LoginAsync(LoginRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                return Failed(new[] { InvalidCredentialsMessage });
            }

            var user = await _identityService.GetUserByEmailAsync(dto.Email);

            if (user == null)
            {
                return Failed(new[] { InvalidCredentialsMessage });
            }

            // The password is verified first so that the response never reveals the state of an account
            // (for example "inactive") to someone who does not know its password.
            var credentials = await _identityService.CheckCredentialsAsync(
                dto.Email,
                dto.Password);

            if (credentials == CredentialCheckResult.LockedOut)
            {
                return Failed(new[] { LockedOutMessage });
            }

            if (credentials != CredentialCheckResult.Success)
            {
                return Failed(new[] { InvalidCredentialsMessage });
            }

            var isActive = await _identityService.IsActiveAsync(user.Id);

            if (!isActive)
            {
                return Failed(new[] { "Your account is inactive." });
            }

            try
            {
                // Every login creates its own token family (independent device session).
                return await GenerateAuthResponseAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed while issuing tokens for user {UserId}.", user.Id);

                return Failed(new[] { LoginFailedMessage });
            }
        }

        // ------------------------------------------------------------------
        // Refresh (rotation + reuse detection)
        // ------------------------------------------------------------------
        public async Task<AuthResultDto> RefreshAsync(RefreshTokenRequestDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return Failed(new[] { InvalidRefreshTokenMessage });

            var now = DateTime.UtcNow;
            var tokenHash = HashToken(dto.RefreshToken);

            try
            {
                var stored = await _refreshTokens.GetByTokenHashAsync(tokenHash);

                if (stored == null)
                    return Failed(new[] { InvalidRefreshTokenMessage });

                // A revoked token coming back = replay / possible theft.
                if (stored.RevokedAt != null)
                {
                    await HandleReuseAsync(stored, now);
                    return Failed(new[] { InvalidRefreshTokenMessage });
                }

                if (stored.ExpiresAt <= now)
                    return Failed(new[] { InvalidRefreshTokenMessage });

                var user = await _identityService.GetUserByIdAsync(stored.UserId);

                if (user == null ||
                    !await _identityService.IsActiveAsync(stored.UserId))
                {
                    // Deactivated/deleted user: kill this session, return the same generic error.
                    await _refreshTokens.RevokeFamilyAsync(stored.TokenFamilyId, now);
                    return Failed(new[] { InvalidRefreshTokenMessage });
                }

                // Current roles, so role changes take effect on the next refresh.
                var roles = await _identityService.GetUserRolesAsync(stored.UserId);

                var tokens = await _jwtService.GenerateTokensAsync(user, roles);
                var newTokenHash = HashToken(tokens.RefreshToken);

                var rotated = false;

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Atomic claim of the old token. Only one concurrent caller gets true.
                    rotated = await _refreshTokens.TryRevokeForRotationAsync(
                        tokenHash, newTokenHash, now);

                    if (rotated)
                    {
                        await _unitOfWork.Repository<RefreshToken>().AddAsync(
                            new RefreshToken
                            {
                                UserId = stored.UserId,
                                TokenHash = newTokenHash,
                                TokenFamilyId = stored.TokenFamilyId, // same session
                                ExpiresAt = tokens.RefreshTokenExpiration
                            });

                        await _unitOfWork.CompleteAsync();
                        await _unitOfWork.CommitTransactionAsync();
                    }
                    else
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                    }
                }
                catch
                {
                    await SafeRollbackAsync();
                    throw;
                }

                if (!rotated)
                {
                    // Lost the race (or token was revoked/expired in between): same handling as replay.
                    // Done after the transaction ended so the revocation is not rolled back.
                    await HandleReuseAsync(stored, now);
                    return Failed(new[] { InvalidRefreshTokenMessage });
                }

                return BuildAuthResponse(user, roles, tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token rotation failed.");

                return Failed(new[] { RefreshFailedMessage });
            }
        }

        // ------------------------------------------------------------------
        // Logout (revokes only the caller's token family)
        // ------------------------------------------------------------------
        public async Task<ResultDto> LogoutAsync(RefreshTokenRequestDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return FailedResult(new[] { InvalidRefreshTokenMessage });

            try
            {
                var tokenHash = HashToken(dto.RefreshToken);

                var stored = await _refreshTokens.GetByTokenHashAsync(tokenHash);

                // Unknown/already-revoked tokens still return success: logout is idempotent
                // and must not reveal whether a token exists.
                if (stored != null)
                {
                    await _refreshTokens.RevokeFamilyAsync(
                        stored.TokenFamilyId, DateTime.UtcNow);
                }

                return new ResultDto { Succeeded = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed.");

                return FailedResult(new[] { LogoutFailedMessage });
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private async System.Threading.Tasks.Task HandleReuseAsync(RefreshToken stored, DateTime now)
        {
            var revoked = await _refreshTokens.RevokeFamilyAsync(stored.TokenFamilyId, now);

            // Never log the raw token or its hash.
            _logger.LogWarning(
                "Refresh token reuse detected. UserId={UserId}, FamilyId={FamilyId}, RevokedTokens={Count}",
                stored.UserId, stored.TokenFamilyId, revoked);
        }

        // Issues a brand-new session (new family) for the user and persists its refresh token.
        private async Task<AuthResultDto> GenerateAuthResponseAsync(string userId)
        {
            var user = await _identityService.GetUserByIdAsync(userId);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            var roles = await _identityService.GetUserRolesAsync(userId);

            var tokens = await _jwtService.GenerateTokensAsync(user, roles);

            await _unitOfWork.Repository<RefreshToken>().AddAsync(
                new RefreshToken
                {
                    UserId = userId,
                    TokenHash = HashToken(tokens.RefreshToken),
                    TokenFamilyId = Guid.NewGuid(),
                    ExpiresAt = tokens.RefreshTokenExpiration
                });

            await _unitOfWork.CompleteAsync();

            return BuildAuthResponse(user, roles, tokens);
        }

        private static AuthResultDto BuildAuthResponse(
            ApplicationUserDto user,
            IList<string> roles,
            GeneratedTokensDto tokens)
        {
            return new AuthResultDto
            {
                Succeeded = true,
                Data = new AuthResponseDto
                {
                    Token = tokens.AccessToken,
                    Expiration = tokens.AccessTokenExpiration,
                    RefreshToken = tokens.RefreshToken,
                    RefreshTokenExpiration = tokens.RefreshTokenExpiration,

                    User = new UserDto
                    {
                        UserId = user.Id,
                        FullName = user.FullName ?? string.Empty,
                        Email = user.Email,
                        Roles = roles.ToList()
                    }
                }
            };
        }

        private async System.Threading.Tasks.Task SafeRollbackAsync()
        {
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction rollback failed.");
            }
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }

        private static AuthResultDto Failed(IEnumerable<string> errors)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Errors = errors.ToList()
            };
        }

        private static ResultDto FailedResult(IEnumerable<string> errors)
        {
            return new ResultDto
            {
                Succeeded = false,
                Errors = errors.ToList()
            };
        }
    }
}