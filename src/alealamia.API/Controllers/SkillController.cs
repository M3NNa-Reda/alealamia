using alealamia.Application.DTOs.Skill;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkillController : ControllerBase
    {
        private readonly ISkillService _skillService;

        public SkillController(ISkillService skillService)
        {
            _skillService = skillService;
        }

        [HttpGet("admin")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetAllSkillsForAdmin()
        {
            var skills = await _skillService.GetAllSkillsForAdminAsync();

            return Ok(skills);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetSkillById(int id)
        {
            var skill = await _skillService.GetSkillByIdAsync(id);

            if (skill == null)
                return NotFound();

            return Ok(skill);
        }

        [HttpGet("track/{trackId:int}/active")]
        [Authorize(Roles = AppRoles.Freelancer)]
        public async Task<IActionResult> GetActiveSkillsByTrack(int trackId)
        {
            var skills = await _skillService
                .GetActiveSkillsByTrackIdAsync(trackId);

            return Ok(skills);
        }

        [HttpGet("admin/track/{trackId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetAllSkillsByTrack(int trackId)
        {
            var skills = await _skillService
                .GetAllSkillsByTrackIdForAdminAsync(trackId);

            return Ok(skills);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateSkill(CreateSkillDto dto)
        {
            var result = await _skillService.CreateSkillAsync(dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateSkill(
            int id,
            UpdateSkillDto dto)
        {
            var result = await _skillService.UpdateSkillAsync(id, dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("{id:int}/deactivate")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeactivateSkill(int id)
        {
            var result = await _skillService.DeactivateSkillAsync(id);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("{id:int}/activate")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> ActivateSkill(int id)
        {
            var result = await _skillService.ActivateSkillAsync(id);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("track/{trackId:int}/{skillId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> AssignSkillToTrack(
            int trackId,
            int skillId)
        {
            var result = await _skillService
                .AssignSkillToTrackAsync(trackId, skillId);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("track/{trackId:int}/{skillId:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> RemoveSkillFromTrack(
            int trackId,
            int skillId)
        {
            var result = await _skillService
                .RemoveSkillFromTrackAsync(trackId, skillId);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }
    }
}