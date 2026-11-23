using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Freelancer;
using alealamia.Application.DTOs.Identity;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Services
{
    public class FreelancerProfileService : IFreelancerProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly ITaskRepository _taskRepository;

        public FreelancerProfileService(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            ITaskRepository taskRepository)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _taskRepository = taskRepository;
        }

        public async Task<ResultDto> CompleteProfileAsync(
            string userId,
            CompleteFreelancerProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return Failed("Title is required.");

            if (string.IsNullOrWhiteSpace(dto.About))
                return Failed("About is required.");

            if (dto.SkillIds == null || !dto.SkillIds.Any())
                return Failed("At least one skill is required.");

            var skillIds = dto.SkillIds.Distinct().ToList();

            var existingProfile =
                await _unitOfWork.FreelancerProfiles.GetByUserIdAsync(userId);

            if (existingProfile != null)
                return Failed("Freelancer profile already exists.");

            var track = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(dto.TrackId);

            if (track == null || !track.IsActive)
                return Failed("Invalid or inactive track.");

            var validation = await ValidateSkillsAsync(dto.TrackId, skillIds);

            if (!validation.Succeeded)
                return validation;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var profile = new FreelancerProfile
                {
                    UserId = userId,
                    TrackId = dto.TrackId,
                    Title = dto.Title.Trim(),
                    About = dto.About.Trim(),
                    ApprovalStatus = FreelancerApprovalStatus.Pending,
                    IsAvailable = false
                };

                await _unitOfWork.FreelancerProfiles.AddAsync(profile);
                await _unitOfWork.CompleteAsync();

                foreach (var skillId in skillIds)
                {
                    await _unitOfWork.Repository<FreelancerSkill>()
                        .AddAsync(new FreelancerSkill
                        {
                            FreelancerProfileId = profile.Id,
                            SkillId = skillId,
                            ProficiencyLevel = ProficiencyLevel.Beginner
                        });
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                return Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ResultDto> UpdateProfileAsync(
            string userId,
            UpdateFreelancerProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName))
                return Failed("First name is required.");

            if (string.IsNullOrWhiteSpace(dto.LastName))
                return Failed("Last name is required.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return Failed("Title is required.");

            if (string.IsNullOrWhiteSpace(dto.About))
                return Failed("About is required.");

            if (dto.SkillIds == null || !dto.SkillIds.Any())
                return Failed("At least one skill is required.");

            var profile =
                await _unitOfWork.FreelancerProfiles.GetByUserIdAsync(userId);

            if (profile == null)
                return Failed("Freelancer profile does not exist.");

            var skillIds = dto.SkillIds.Distinct().ToList();

            var validation = await ValidateSkillsAsync(
                profile.TrackId,
                skillIds);

            if (!validation.Succeeded)
                return validation;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var identityResult =
                    await _identityService.UpdateUserNameAsync(
                        userId,
                        new UpdateUserNameDto
                        {
                            FirstName = dto.FirstName.Trim(),
                            LastName = dto.LastName.Trim()
                        });

                if (!identityResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();

                    return Failed(identityResult.Errors);
                }

                profile.Title = dto.Title.Trim();
                profile.About = dto.About.Trim();
                profile.ProfileImageUrl = dto.ProfileImageUrl;
                profile.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.FreelancerProfiles.Update(profile);

                await UpdateFreelancerSkillsAsync(
                    profile.Id,
                    skillIds);

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                return Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ResultDto> ChangeTrackAsync(
            string userId,
            ChangeFreelancerTrackDto dto)
        {
            if (dto.TrackId <= 0)
                return Failed("Invalid track.");

            if (dto.SkillIds == null || !dto.SkillIds.Any())
                return Failed("At least one skill is required.");

            var profile =
                await _unitOfWork.FreelancerProfiles.GetByUserIdAsync(userId);

            if (profile == null)
                return Failed("Freelancer profile does not exist.");

            if (profile.ApprovalStatus == FreelancerApprovalStatus.Pending)
                return Failed("Your profile is already waiting for admin approval.");

            if (profile.TrackId == dto.TrackId)
                return Failed("The selected track is already your current track.");

            var utcNow = DateTime.UtcNow;

            if (profile.LastTrackChangeAt.HasValue &&
                utcNow < profile.LastTrackChangeAt.Value.AddDays(60))
            {
                var nextAllowedDate =
                    profile.LastTrackChangeAt.Value.AddDays(60);

                return Failed(
                    $"You can change your track again on {nextAllowedDate:yyyy-MM-dd}.");
            }

            var hasActiveTasks =
                await _taskRepository.HasActiveTasksAsync(profile.Id);

            if (hasActiveTasks)
            {
                return Failed(
                    "You cannot change your track while you have active tasks. Please complete your current tasks first.");
            }

            var newTrack = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(dto.TrackId);

            if (newTrack == null || !newTrack.IsActive)
                return Failed("Invalid or inactive track.");

            var skillIds = dto.SkillIds.Distinct().ToList();

            var validation = await ValidateSkillsAsync(
                dto.TrackId,
                skillIds);

            if (!validation.Succeeded)
                return validation;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var oldSkills = await _unitOfWork.Repository<FreelancerSkill>()
                    .FindByConditionAsync(
                        x => x.FreelancerProfileId == profile.Id);

                foreach (var oldSkill in oldSkills)
                {
                    _unitOfWork.Repository<FreelancerSkill>().Delete(oldSkill);
                }

                foreach (var skillId in skillIds)
                {
                    await _unitOfWork.Repository<FreelancerSkill>()
                        .AddAsync(new FreelancerSkill
                        {
                            FreelancerProfileId = profile.Id,
                            SkillId = skillId,
                            ProficiencyLevel = ProficiencyLevel.Beginner
                        });
                }

                profile.TrackId = dto.TrackId;
                profile.LastTrackChangeAt = utcNow;
                profile.ApprovalStatus = FreelancerApprovalStatus.Pending;
                profile.UpdatedAt = utcNow;

                _unitOfWork.FreelancerProfiles.Update(profile);

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                return Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<FreelancerDetailsDto?> GetFreelancerProfileAsync(
            string userId)
        {
            var profile =
                await _unitOfWork.FreelancerProfiles.GetByUserIdAsync(userId);

            if (profile == null)
                return null;

            var user = await _identityService.GetUserByIdAsync(userId);

            if (user == null)
                return null;

            var track = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(profile.TrackId);

            var freelancerSkills =
                await _unitOfWork.Repository<FreelancerSkill>()
                    .FindByConditionAsync(
                        x => x.FreelancerProfileId == profile.Id);

            var skills = new List<FreelancerSkillDto>();

            foreach (var freelancerSkill in freelancerSkills)
            {
                var skill = await _unitOfWork.Repository<Skill>()
                    .GetByIdAsync(freelancerSkill.SkillId);

                if (skill == null)
                    continue;

                skills.Add(new FreelancerSkillDto
                {
                    SkillId = skill.Id,
                    SkillName = skill.Name,
                    ProficiencyLevel = freelancerSkill.ProficiencyLevel
                });
            }

            return new FreelancerDetailsDto
            {
                Id = profile.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Title = profile.Title,
                About = profile.About,
                ProfileImageUrl = profile.ProfileImageUrl,
                TrackId = profile.TrackId,
                TrackName = track?.Name ?? string.Empty,
                Skills = skills
            };
        }

        public async Task<FreelancerStatusDto?> GetFreelancerStatusAsync(
            string userId)
        {
            var profile = await _unitOfWork.FreelancerProfiles
                .GetByUserIdAsync(userId);

            if (profile == null)
                return null;

            return new FreelancerStatusDto
            {
                ApprovalStatus = profile.ApprovalStatus,
                IsAvailable = profile.IsAvailable
            };
        }

        private async Task<ResultDto> ValidateSkillsAsync(
            int trackId,
            List<int> skillIds)
        {
            if (!skillIds.Any())
                return Failed("At least one skill is required.");

            var skills = await _unitOfWork.Repository<Skill>()
                .FindByConditionAsync(
                    x => skillIds.Contains(x.Id) && x.IsActive);

            if (skills.Count() != skillIds.Count)
                return Failed("One or more selected skills are invalid or inactive.");

            var trackSkills = await _unitOfWork.Repository<TrackSkill>()
                .FindByConditionAsync(
                    x => x.TrackId == trackId &&
                         skillIds.Contains(x.SkillId));

            if (trackSkills.Count() != skillIds.Count)
                return Failed("One or more skills do not belong to the selected track.");

            return Success();
        }

        private async System.Threading.Tasks.Task UpdateFreelancerSkillsAsync(
            int freelancerProfileId,
            List<int> newSkillIds)
        {
            var repository = _unitOfWork.Repository<FreelancerSkill>();

            var existingSkills = await repository.FindByConditionAsync(
                x => x.FreelancerProfileId == freelancerProfileId);

            var existingSkillIds = existingSkills
                .Select(x => x.SkillId)
                .ToHashSet();

            var newSkillIdSet = newSkillIds.ToHashSet();

            foreach (var existingSkill in existingSkills)
            {
                if (!newSkillIdSet.Contains(existingSkill.SkillId))
                    repository.Delete(existingSkill);
            }

            foreach (var skillId in newSkillIds)
            {
                if (!existingSkillIds.Contains(skillId))
                {
                    await repository.AddAsync(new FreelancerSkill
                    {
                        FreelancerProfileId = freelancerProfileId,
                        SkillId = skillId,
                        ProficiencyLevel = ProficiencyLevel.Beginner
                    });
                }
            }
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

        private static ResultDto Failed(IEnumerable<string> errors)
        {
            return new ResultDto
            {
                Succeeded = false,
                Errors = errors.ToList()
            };
        }
    }
}