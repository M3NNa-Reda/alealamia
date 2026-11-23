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
    }
}