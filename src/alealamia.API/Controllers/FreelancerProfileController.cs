using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Freelancer;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Freelancer)]
    public class FreelancerProfileController : ControllerBase
    {
        private readonly IFreelancerProfileService _freelancerProfileService;

        public FreelancerProfileController(
            IFreelancerProfileService freelancerProfileService)
        {
            _freelancerProfileService = freelancerProfileService;
        }

        [HttpPost]
        public async Task<IActionResult> CompleteProfile(
            [FromBody] CompleteFreelancerProfileDto dto)
        {
            var userId = User.GetUserId();

            var result = await _freelancerProfileService
                .CompleteProfileAsync(userId, dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = "Freelancer profile completed successfully."
            });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.GetUserId();

            var profile = await _freelancerProfileService
                .GetFreelancerProfileAsync(userId);

            if (profile == null)
                return NotFound(new
                {
                    Message = "Freelancer profile does not exist."
                });

            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateFreelancerProfileDto dto)
        {
            var userId = User.GetUserId();

            var result = await _freelancerProfileService
                .UpdateProfileAsync(userId, dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = "Freelancer profile updated successfully."
            });
        }

        [HttpPatch("change-track")]
        public async Task<IActionResult> ChangeTrack(
            [FromBody] ChangeFreelancerTrackDto dto)
        {
            var userId = User.GetUserId();

            var result = await _freelancerProfileService
                .ChangeTrackAsync(userId, dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = "Track change request submitted successfully. Your profile is pending admin approval."
            });
        }

        [HttpPatch("availability")]
        public async Task<IActionResult> SetAvailability(
            [FromBody] SetAvailabilityDto dto)
        {
            var userId = User.GetUserId();

            var result = await _freelancerProfileService
                .SetAvailabilityAsync(userId, dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = dto.IsAvailable
                    ? "You are now available for new task offers."
                    : "You will not receive new task offers until you switch availability back on."
            });
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var userId = User.GetUserId();

            var status = await _freelancerProfileService
                .GetFreelancerStatusAsync(userId);

            if (status == null)
                return NotFound(new
                {
                    Message = "Freelancer profile does not exist."
                });

            return Ok(status);
        }
    }
}
