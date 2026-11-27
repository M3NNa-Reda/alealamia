using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Users;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace alealamia.API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminUsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<UserAccountDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] UserListQueryDto query)
        {
            return Ok(await _userService.GetAllUsersAsync(query));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserAccountDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);

            return user == null ? NotFound() : Ok(user);
        }

        [HttpPatch("{id}/status")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateUserStatusDto dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (adminId == null) return Unauthorized();

            var result = await _userService.UpdateUserStatusAsync(adminId, id, dto);

            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}