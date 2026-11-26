using alealamia.Application.DTOs.Auth;
using alealamia.Application.DTOs.Identity;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;

namespace alealamia.Application.Services
{
    public class AuthService : IAuthService
    {
        public const string InvalidCredentialsMessage = "Invalid email or password.";
        public const string LockedOutMessage = "Too many failed attempts. The account is temporarily locked, please try again later.";

        private readonly IIdentityService _identityService;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IIdentityService identityService,
            IJwtService jwtService,
            IUnitOfWork unitOfWork,
            IFreelancerProfileRepository freelancerProfileRepository)
        {
            _identityService = identityService;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
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

                var response = await GenerateAuthResponseAsync(
                    result.UserId!);

                await _unitOfWork.CommitTransactionAsync();

                return response;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();

                return Failed(new[]
                {"Registration failed. Please try again." });
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

        private async Task<AuthResultDto> GenerateAuthResponseAsync(string userId)
        {
            var user = await _identityService.GetUserByIdAsync(userId);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            var roles = await _identityService.GetUserRolesAsync(userId);

            var token = await _jwtService.GenerateTokenAsync(user, roles);

            return new AuthResultDto
            {
                Succeeded = true,
                Data = new AuthResponseDto
                {
                    Token = token.Token,
                    Expiration = token.Expiration,
                    User = new UserDto
                    {
                        UserId = user.Id,
                        FullName = user.FullName ?? string.Empty,
                        Email = user.Email,
                        Roles = roles
                    }
                }
            };
        }

        private static AuthResultDto Failed(IEnumerable<string> errors)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Errors = errors.ToList()
            };
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

            var roles = await _identityService.GetUserRolesAsync(user.Id);

            var token = await _jwtService.GenerateTokenAsync(user, roles);

            return new AuthResultDto
            {
                Succeeded = true,
                Data = new AuthResponseDto
                {
                    Token = token.Token,
                    Expiration = token.Expiration,
                    User = new UserDto
                    {
                        UserId = user.Id,
                        FullName = user.FullName ?? string.Empty,
                        Email = user.Email,
                        Roles = roles
                    }
                }
            };
        }
    }
}