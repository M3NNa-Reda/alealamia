using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Users;
using alealamia.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace alealamia.API.Controllers
{
    [ApiController]
    [Route("api/account")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet("me")]
        [ProducesResponseType(typeof(UserAccountDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAccount()
        {
            if (CurrentUserId == null) return Unauthorized();

            var account = await _userService.GetMyAccountAsync(CurrentUserId);

            return account == null ? NotFound() : Ok(account);
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMyAccount([FromBody] UpdateMyAccountDto dto)
        {
            if (CurrentUserId == null) return Unauthorized();

            var result = await _userService.UpdateMyAccountAsync(CurrentUserId, dto);

            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (CurrentUserId == null) return Unauthorized();

            var result = await _userService.ChangePasswordAsync(CurrentUserId, dto);

            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPost("deactivate")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeactivateMyAccount([FromBody] DeactivateMyAccountDto dto)
        {
            if (CurrentUserId == null) return Unauthorized();

            var result = await _userService.DeactivateMyAccountAsync(CurrentUserId, dto);

            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}