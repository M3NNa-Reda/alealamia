using alealamia.Application.DTOs.Admin;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers
{
    [Route("api/admin/freelancers")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminFreelancerController : ControllerBase
    {
        private readonly IAdminFreelancerService _adminFreelancerService;

        public AdminFreelancerController(
            IAdminFreelancerService adminFreelancerService)
        {
            _adminFreelancerService = adminFreelancerService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingFreelancers()
        {
            var freelancers = await _adminFreelancerService
                .GetPendingFreelancersAsync();

            return Ok(freelancers);
        }

        [HttpGet("{freelancerId:int}")]
        public async Task<IActionResult> GetFreelancerDetails(int freelancerId)
        {
            var freelancer = await _adminFreelancerService
                .GetFreelancerDetailsAsync(freelancerId);

            if (freelancer == null)
                return NotFound(new
                {
                    Message = "Freelancer profile does not exist."
                });

            return Ok(freelancer);
        }

        [HttpPatch("{freelancerId:int}/approve")]
        public async Task<IActionResult> ApproveFreelancer(int freelancerId)
        {
            var result = await _adminFreelancerService
                .ApproveFreelancerAsync(freelancerId);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = "Freelancer approved successfully."
            });
        }

        [HttpPatch("{freelancerId:int}/reject")]
        public async Task<IActionResult> RejectFreelancer(int freelancerId)
        {
            var result = await _adminFreelancerService
                .RejectFreelancerAsync(freelancerId);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = "Freelancer rejected successfully."
            });
        }

        [HttpPatch("{freelancerId:int}/skills/{skillId:int}/level")]
        public async Task<IActionResult> UpdateSkillLevel(
            int freelancerId,
            int skillId,
            [FromBody] UpdateFreelancerSkillLevelDto dto)
        {
            var result = await _adminFreelancerService
                .UpdateFreelancerSkillLevelAsync(
                    freelancerId,
                    skillId,
                    dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = "Freelancer skill level updated successfully."
            });
        }
    }
}
