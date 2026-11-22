using alealamia.Application.DTOs.Auth;
using alealamia.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("register/client")]
        public async Task<IActionResult> RegisterClient(RegisterClientRequestDto dto)
        {
            var result = await _authService.RegisterClientAsync(dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("register/freelancer")]
        public async Task<IActionResult> RegisterFreelancer(RegisterFreelancerRequestDto dto)
        {
            var result = await _authService.RegisterFreelancerAsync(dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
