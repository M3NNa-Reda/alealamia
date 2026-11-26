using alealamia.Application.DTOs.FreelancerBlock;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace alealamia.API.Controllers
{
    [ApiController]
    [Route("api/admin/freelancer-blocks")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminFreelancerBlockController : ControllerBase
    {
        private readonly IFreelancerBlockService _freelancerBlockService;

        public AdminFreelancerBlockController(
            IFreelancerBlockService freelancerBlockService)
        {
            _freelancerBlockService = freelancerBlockService;
        }

        // Create a block manually by admin
        [HttpPost]
        public async Task<IActionResult> CreateBlock(
            [FromBody] CreateFreelancerBlockDto dto)
        {
            dto.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _freelancerBlockService.CreateBlockAsync(dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("freelancers/{freelancerProfileId:int}/active")]
        public async Task<IActionResult> GetActiveBlocks(int freelancerProfileId)
        {
            var blocks = await _freelancerBlockService
                .GetActiveBlocksByFreelancerIdAsync(freelancerProfileId);

            return Ok(blocks);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetAllActiveBlocks()
        {
            var blocks = await _freelancerBlockService
                .GetAllActiveBlocksAsync();

            return Ok(blocks);
        }

        [HttpGet("freelancers/{freelancerProfileId:int}/history")]
        public async Task<IActionResult> GetBlockHistory(int freelancerProfileId)
        {
            var blocks = await _freelancerBlockService
                .GetBlockHistoryByFreelancerIdAsync(freelancerProfileId);

            return Ok(blocks);
        }
    }
}
