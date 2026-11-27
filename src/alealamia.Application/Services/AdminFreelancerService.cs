using alealamia.Application.DTOs.Admin;
using alealamia.Application.DTOs.Common;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Services
{
    public class AdminFreelancerService : IAdminFreelancerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public AdminFreelancerService(
            IUnitOfWork unitOfWork,
            IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<IEnumerable<FreelancerReviewListDto>> GetPendingFreelancersAsync()
        {
            var freelancers = await _unitOfWork.FreelancerProfiles
                .GetFreelancersByApprovalStatusAsync(
                    FreelancerApprovalStatus.Pending);

            var result = new List<FreelancerReviewListDto>();

            foreach (var freelancer in freelancers)
            {
                var user = await _identityService
                    .GetUserByIdAsync(freelancer.UserId);

                var track = await _unitOfWork.Repository<Track>()
                    .GetByIdAsync(freelancer.TrackId);

                if (user == null)
                    continue;

                result.Add(new FreelancerReviewListDto
                {
                    Id = freelancer.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? string.Empty,
                    Title = freelancer.Title,
                    TrackName = track?.Name ?? string.Empty,
                    CreatedAt = freelancer.CreatedAt
                });
            }

            return result;
        }

        public async Task<FreelancerReviewDetailsDto?> GetFreelancerDetailsAsync(int id)
        {
            var freelancer = await _unitOfWork.FreelancerProfiles
                .GetByIdAsync(id);

            if (freelancer == null)
                return null;

            var user = await _identityService
                .GetUserByIdAsync(freelancer.UserId);

            if (user == null)
                return null;

            var track = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(freelancer.TrackId);

            var freelancerSkills = await _unitOfWork
                .Repository<FreelancerSkill>()
                .FindByConditionAsync(
                    x => x.FreelancerProfileId == freelancer.Id);

            var skills = new List<FreelancerReviewSkillDto>();

            foreach (var freelancerSkill in freelancerSkills)
            {
                var skill = await _unitOfWork.Repository<Skill>()
                    .GetByIdAsync(freelancerSkill.SkillId);

                if (skill == null)
                    continue;

                skills.Add(new FreelancerReviewSkillDto
                {
                    SkillId = skill.Id,
                    SkillName = skill.Name,
                    ProficiencyLevel =
                        freelancerSkill.ProficiencyLevel.ToString()
                });
            }

            return new FreelancerReviewDetailsDto
            {
                Id = freelancer.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                Title = freelancer.Title,
                About = freelancer.About,
                ProfileImageUrl = freelancer.ProfileImageUrl,
                TrackName = track?.Name ?? string.Empty,
                Skills = skills
            };
        }

        public async Task<ResultDto> ApproveFreelancerAsync(int freelancerId)
        {
            return await UpdateApprovalStatusAsync(
                freelancerId,
                FreelancerApprovalStatus.Approved);
        }

        public async Task<ResultDto> RejectFreelancerAsync(int freelancerId)
        {
            return await UpdateApprovalStatusAsync(
                freelancerId,
                FreelancerApprovalStatus.Rejected);
        }

        public async Task<ResultDto> UpdateFreelancerSkillLevelAsync(
            int freelancerId,
            int skillId,
            UpdateFreelancerSkillLevelDto dto)
        {
            if (!Enum.IsDefined(typeof(ProficiencyLevel), dto.ProficiencyLevel))
                return Failed("Invalid proficiency level.");

            var freelancer = await _unitOfWork.FreelancerProfiles
                .GetByIdAsync(freelancerId);

            if (freelancer == null)
                return Failed("Freelancer profile does not exist.");

            var freelancerSkill = (await _unitOfWork
                .Repository<FreelancerSkill>()
                .FindByConditionAsync(x =>
                    x.FreelancerProfileId == freelancerId &&
                    x.SkillId == skillId))
                .FirstOrDefault();

            if (freelancerSkill == null)
                return Failed("This skill is not assigned to this freelancer.");

            freelancerSkill.ProficiencyLevel = dto.ProficiencyLevel;

            _unitOfWork.Repository<FreelancerSkill>()
                .Update(freelancerSkill);

            await _unitOfWork.CompleteAsync();

            return Success();
        }

        private async Task<ResultDto> UpdateApprovalStatusAsync(
            int freelancerId,
            FreelancerApprovalStatus newStatus)
        {
            var freelancer = await _unitOfWork.FreelancerProfiles
                .GetByIdAsync(freelancerId);

            if (freelancer == null)
                return Failed("Freelancer profile does not exist.");

            if (freelancer.ApprovalStatus !=
                FreelancerApprovalStatus.Pending)
            {
                return Failed(
                    "Only pending freelancers can be approved or rejected.");
            }

            freelancer.ApprovalStatus = newStatus;

            // An approved freelancer starts receiving offers right away and can switch it off from their profile.
            // A rejected profile is never available.
            freelancer.IsAvailable = newStatus == FreelancerApprovalStatus.Approved;
            freelancer.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.FreelancerProfiles.Update(freelancer);

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
