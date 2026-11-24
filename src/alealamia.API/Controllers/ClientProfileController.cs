using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Client;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers
{
    [Route("api/client-profile")]
    [ApiController]
    [Authorize(Roles = AppRoles.Client)]
    public class ClientProfileController : ControllerBase
    {
        private readonly IClientProfileService _clientProfileService;

        public ClientProfileController(
            IClientProfileService clientProfileService)
        {
            _clientProfileService = clientProfileService;
        }

        [HttpPost]
        public async Task<IActionResult> CompleteProfile(
            [FromBody] ClientProfileDto dto)
        {
            var userId = User.GetUserId();

            var result = await _clientProfileService
                .CompleteProfileAsync(userId, dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = "Client profile completed successfully."
            });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.GetUserId();

            var profile = await _clientProfileService
                .GetClientProfileAsync(userId);

            if (profile == null)
                return NotFound(new
                {
                    Message = "Client profile does not exist."
                });

            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] ClientProfileDto dto)
        {
            var userId = User.GetUserId();

            var result = await _clientProfileService
                .UpdateProfileAsync(userId, dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(new
            {
                Message = "Client profile updated successfully."
            });
        }
    }
}
