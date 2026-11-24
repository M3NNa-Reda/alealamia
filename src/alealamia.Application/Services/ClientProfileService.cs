
using alealamia.Application.DTOs.Client;
using alealamia.Application.DTOs.Common;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;

namespace alealamia.Application.Services
{
    public class ClientProfileService : IClientProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public ClientProfileService(
            IUnitOfWork unitOfWork,
            IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<ResultDto> CompleteProfileAsync(
            string userId,
            ClientProfileDto dto)
        {
            var user = await _identityService.GetUserByIdAsync(userId);

            if (user == null)
                return Failed("User does not exist.");

            var existingProfile = (await _unitOfWork
                .Repository<ClientProfile>()
                .FindByConditionAsync(x => x.UserId == userId))
                .FirstOrDefault();

            if (existingProfile != null)
                return Failed("Client profile already exists.");

            var profile = new ClientProfile
            {
                UserId = userId,
                BusinessName = dto.BusinessName,
                BusinessType = dto.BusinessType,
                WebsiteUrl = dto.WebsiteUrl
            };

            await _unitOfWork.Repository<ClientProfile>()
                .AddAsync(profile);

            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ClientProfileDetailsDto?> GetClientProfileAsync(
            string userId)
        {
            var profile = (await _unitOfWork
                .Repository<ClientProfile>()
                .FindByConditionAsync(x => x.UserId == userId))
                .FirstOrDefault();

            if (profile == null)
                return null;

            return new ClientProfileDetailsDto
            {
                Id = profile.Id,
                BusinessName = profile.BusinessName,
                BusinessType = profile.BusinessType,
                WebsiteUrl = profile.WebsiteUrl,
                CreatedAt = profile.CreatedAt
            };
        }

        public async Task<ResultDto> UpdateProfileAsync(
            string userId,
            ClientProfileDto dto)
        {
            var profile = (await _unitOfWork
                .Repository<ClientProfile>()
                .FindByConditionAsync(x => x.UserId == userId))
                .FirstOrDefault();

            if (profile == null)
                return Failed("Client profile does not exist.");

            profile.BusinessName = dto.BusinessName;
            profile.BusinessType = dto.BusinessType;
            profile.WebsiteUrl = dto.WebsiteUrl;

            _unitOfWork.Repository<ClientProfile>().Update(profile);

            await _unitOfWork.CompleteAsync();

            return Success();
        }

        private static ResultDto Success()
        {
            return new ResultDto
            {
                Succeeded = true
            };
        }

        private static ResultDto Failed(string error)
        {
            return new ResultDto
            {
                Succeeded = false,
                Errors = new List<string> { error }
            };
        }
    }
}
