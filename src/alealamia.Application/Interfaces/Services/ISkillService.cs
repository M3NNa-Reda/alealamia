using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Skill;

namespace alealamia.Application.Interfaces.Services
{
    public interface ISkillService
    {
        Task<IEnumerable<SkillDto>> GetAllSkillsForAdminAsync();
        Task<SkillDto?> GetSkillByIdAsync(int id);

        Task<IEnumerable<SkillDto>> GetActiveSkillsByTrackIdAsync(int trackId);
        Task<IEnumerable<SkillDto>> GetAllSkillsByTrackIdForAdminAsync(int trackId);

        Task<ResultDto> CreateSkillAsync(CreateSkillDto dto);
        Task<ResultDto> UpdateSkillAsync(int id, UpdateSkillDto dto);
        Task<ResultDto> DeactivateSkillAsync(int id);
        Task<ResultDto> ActivateSkillAsync(int id);

        Task<ResultDto> AssignSkillToTrackAsync(int trackId, int skillId);
        Task<ResultDto> RemoveSkillFromTrackAsync(int trackId, int skillId);
    }
}