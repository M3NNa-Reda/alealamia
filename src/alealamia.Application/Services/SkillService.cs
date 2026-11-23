using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Skill;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;

namespace alealamia.Application.Services
{
    public class SkillService : ISkillService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SkillService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SkillDto>> GetAllSkillsForAdminAsync()
        {
            var skills = await _unitOfWork.Repository<Skill>()
                .GetAllAsync();

            return skills.Select(MapToDto);
        }

        public async Task<SkillDto?> GetSkillByIdAsync(int id)
        {
            var skill = await _unitOfWork.Repository<Skill>()
                .GetByIdAsync(id);

            return skill == null ? null : MapToDto(skill);
        }

        public async Task<IEnumerable<SkillDto>> GetActiveSkillsByTrackIdAsync(int trackId)
        {
            var track = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(trackId);

            if (track == null || !track.IsActive)
                return Enumerable.Empty<SkillDto>();

            var trackSkills = await _unitOfWork.Repository<TrackSkill>()
                .FindByConditionAsync(x => x.TrackId == trackId);

            var skillIds = trackSkills
                .Select(x => x.SkillId)
                .ToList();

            if (!skillIds.Any())
                return Enumerable.Empty<SkillDto>();

            var skills = await _unitOfWork.Repository<Skill>()
                .FindByConditionAsync(x =>
                    skillIds.Contains(x.Id) && x.IsActive);

            return skills.Select(MapToDto);
        }

        public async Task<IEnumerable<SkillDto>> GetAllSkillsByTrackIdForAdminAsync(int trackId)
        {
            var track = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(trackId);

            if (track == null)
                return Enumerable.Empty<SkillDto>();

            var trackSkills = await _unitOfWork.Repository<TrackSkill>()
                .FindByConditionAsync(x => x.TrackId == trackId);

            var skillIds = trackSkills
                .Select(x => x.SkillId)
                .ToList();

            if (!skillIds.Any())
                return Enumerable.Empty<SkillDto>();

            var skills = await _unitOfWork.Repository<Skill>()
                .FindByConditionAsync(x => skillIds.Contains(x.Id));

            return skills.Select(MapToDto);
        }

        public async Task<ResultDto> CreateSkillAsync(CreateSkillDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Failed("Skill name is required.");

            var name = dto.Name.Trim();

            var existingSkills = await _unitOfWork.Repository<Skill>()
                .FindByConditionAsync(x =>
                    x.Name.ToLower() == name.ToLower());

            if (existingSkills.Any())
                return Failed("Skill already exists.");

            var skill = new Skill
            {
                Name = name,
                Description = dto.Description?.Trim(),
                IsActive = true
            };

            await _unitOfWork.Repository<Skill>().AddAsync(skill);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> UpdateSkillAsync(int id, UpdateSkillDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Failed("Skill name is required.");

            var repository = _unitOfWork.Repository<Skill>();

            var skill = await repository.GetByIdAsync(id);

            if (skill == null)
                return Failed("Skill not found.");

            var name = dto.Name.Trim();

            var existingSkills = await repository.FindByConditionAsync(
                x => x.Id != id && x.Name.ToLower() == name.ToLower());

            if (existingSkills.Any())
                return Failed("Another skill already has this name.");

            skill.Name = name;
            skill.Description = dto.Description?.Trim();

            repository.Update(skill);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> DeactivateSkillAsync(int id)
        {
            var repository = _unitOfWork.Repository<Skill>();

            var skill = await repository.GetByIdAsync(id);

            if (skill == null)
                return Failed("Skill not found.");

            if (!skill.IsActive)
                return Failed("Skill is already inactive.");

            skill.IsActive = false;

            repository.Update(skill);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> ActivateSkillAsync(int id)
        {
            var repository = _unitOfWork.Repository<Skill>();

            var skill = await repository.GetByIdAsync(id);

            if (skill == null)
                return Failed("Skill not found.");

            if (skill.IsActive)
                return Failed("Skill is already active.");

            skill.IsActive = true;

            repository.Update(skill);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> AssignSkillToTrackAsync(int trackId, int skillId)
        {
            var track = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(trackId);

            if (track == null || !track.IsActive)
                return Failed("Track not found or inactive.");

            var skill = await _unitOfWork.Repository<Skill>()
                .GetByIdAsync(skillId);

            if (skill == null || !skill.IsActive)
                return Failed("Skill not found or inactive.");

            var trackSkillRepository = _unitOfWork.Repository<TrackSkill>();

            var existingRelation = await trackSkillRepository
                .FindByConditionAsync(x =>
                    x.TrackId == trackId && x.SkillId == skillId);

            if (existingRelation.Any())
                return Failed("This skill is already assigned to this Track.");

            var trackSkill = new TrackSkill
            {
                TrackId = trackId,
                SkillId = skillId
            };

            await trackSkillRepository.AddAsync(trackSkill);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> RemoveSkillFromTrackAsync(int trackId, int skillId)
        {
            var repository = _unitOfWork.Repository<TrackSkill>();

            var relation = await repository.FindByConditionAsync(
                x => x.TrackId == trackId && x.SkillId == skillId);

            var trackSkill = relation.FirstOrDefault();

            if (trackSkill == null)
                return Failed("Skill is not assigned to this Track.");

            repository.Delete(trackSkill);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        private static SkillDto MapToDto(Skill skill)
        {
            return new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description,
                IsActive = skill.IsActive
            };
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