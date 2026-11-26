using alealamia.API.Security;
using alealamia.Application.DTOs.Auth;
using alealamia.Application.DTOs.Common;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace alealamia.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterFreelancer(RegisterRequestDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Succeeded)
                return Unauthorized(result);

            return Ok(result);
        }
        [HttpPost("refresh")]
        [AllowAnonymous] 
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshAsync(dto);

            if (result.Succeeded)
                return Ok(result);

           
            return result.Errors.Contains(AuthService.InvalidRefreshTokenMessage)
                ? Unauthorized(result)
                : StatusCode(StatusCodes.Status500InternalServerError, result);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.LogoutAsync(dto);

            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}