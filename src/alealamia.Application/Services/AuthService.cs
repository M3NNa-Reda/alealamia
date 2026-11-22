using alealamia.Application.DTOs.Auth;
using alealamia.Application.DTOs.Identity;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFreelancerProfileRepository _freelancerProfileRepository;

        public AuthService(
            IIdentityService identityService,
            IJwtService jwtService,
            IUnitOfWork unitOfWork,
            IFreelancerProfileRepository freelancerProfileRepository)
        {
            _identityService = identityService;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
            _freelancerProfileRepository = freelancerProfileRepository;
        }

        public async Task<AuthResultDto> RegisterClientAsync(
            RegisterClientRequestDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var result = await CreateUserAsync(dto.FirstName, dto.LastName,
                    dto.Email, dto.Password);

                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Failed(result.Errors);
                }

                var roleAdded = await _identityService.AddToRoleAsync(
                    result.UserId!, AppRoles.Client);

                if (!roleAdded)
                    throw new InvalidOperationException(
                        "Failed to assign Client role.");

                var clientProfile = new ClientProfile
                {
                    UserId = result.UserId!,
                    BusinessName = dto.BusinessName,
                    BusinessType = dto.BusinessType,
                    WebsiteUrl = dto.WebsiteUrl
                };

                await _unitOfWork.Repository<ClientProfile>()
                    .AddAsync(clientProfile);

                await _unitOfWork.CompleteAsync();

                var response = await GenerateAuthResponseAsync(result.UserId!);

                await _unitOfWork.CommitTransactionAsync();

                return response;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Failed(new[] { "Registration failed. Please try again." });
            }
        }

        public async Task<AuthResultDto> RegisterFreelancerAsync(
            RegisterFreelancerRequestDto dto)
        {
            var track = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(dto.TrackId);

            if (track == null)
                return Failed(new[] { "Invalid Track ID." });

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var result = await CreateUserAsync(dto.FirstName, dto.LastName,
                    dto.Email, dto.Password);

                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Failed(result.Errors);
                }

                var roleAdded = await _identityService.AddToRoleAsync(
                    result.UserId!, AppRoles.Freelancer);

                if (!roleAdded)
                    throw new InvalidOperationException(
                        "Failed to assign Freelancer role.");

                var freelancerProfile = new FreelancerProfile
                {
                    UserId = result.UserId!,
                    TrackId = dto.TrackId,
                    ApprovalStatus = FreelancerApprovalStatus.Pending,
                    IsAvailable = false
                };

                await _freelancerProfileRepository.AddAsync(freelancerProfile);

                await _unitOfWork.CompleteAsync();

                var response = await GenerateAuthResponseAsync(result.UserId!);

                await _unitOfWork.CommitTransactionAsync();

                return response;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Failed(new[] { "Registration failed. Please try again." });
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

        public Task<AuthResultDto> LoginAsync(LoginRequestDto dto)
        {
            throw new NotImplementedException();
        }
    }
}