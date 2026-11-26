using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Portfolio;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers
{
    [Route("api/freelancer/portfolio")]
    [ApiController]
    [Authorize(Roles = AppRoles.Freelancer)]
    public class FreelancerPortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IFreelancerProfileService _freelancerProfileService;

        public FreelancerPortfolioController(
            IPortfolioService portfolioService,
            IFreelancerProfileService freelancerProfileService)
        {
            _portfolioService = portfolioService;
            _freelancerProfileService = freelancerProfileService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePortfolioItem(
            [FromBody] PortfolioItemDto dto)
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var result = await _portfolioService.CreatePortfolioItemAsync(
                freelancerProfileId, dto);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPut("{portfolioItemId:int}")]
        public async Task<IActionResult> UpdatePortfolioItem(
            int portfolioItemId,
            [FromBody] PortfolioItemDto dto)
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var result = await _portfolioService.UpdatePortfolioItemAsync(
                freelancerProfileId, portfolioItemId, dto);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpDelete("{portfolioItemId:int}")]
        public async Task<IActionResult> DeletePortfolioItem(int portfolioItemId)
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var result = await _portfolioService.DeletePortfolioItemAsync(
                freelancerProfileId, portfolioItemId);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyPortfolio()
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var portfolio = await _portfolioService.GetMyPortfolioAsync(
                freelancerProfileId);

            return Ok(portfolio);
        }

        [HttpGet("{portfolioItemId:int}")]
        public async Task<IActionResult> GetPortfolioItem(int portfolioItemId)
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var portfolioItem = await _portfolioService.GetPortfolioItemAsync(
                freelancerProfileId, portfolioItemId);

            if (portfolioItem == null)
                return NotFound();

            return Ok(portfolioItem);
        }

        [HttpPost("{portfolioItemId:int}/files")]
        public async Task<IActionResult> AddFiles(
            int portfolioItemId,
            [FromBody] AddPortfolioFilesDto dto)
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var result = await _portfolioService.AddFilesAsync(
                freelancerProfileId, portfolioItemId, dto.FileIds);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpDelete("{portfolioItemId:int}/files/{fileId:int}")]
        public async Task<IActionResult> RemoveFile(
            int portfolioItemId,
            int fileId)
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var result = await _portfolioService.RemoveFileAsync(
                freelancerProfileId, portfolioItemId, fileId);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPatch("{portfolioItemId:int}/publish")]
        public async Task<IActionResult> PublishPortfolioItem(int portfolioItemId)
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var result = await _portfolioService.PublishPortfolioItemAsync(
                freelancerProfileId, portfolioItemId);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPatch("{portfolioItemId:int}/unpublish")]
        public async Task<IActionResult> UnpublishPortfolioItem(int portfolioItemId)
        {
            var freelancerProfileId =
                await _freelancerProfileService.GetProfileIdAsync(User);

            var result = await _portfolioService.UnpublishPortfolioItemAsync(
                freelancerProfileId, portfolioItemId);

            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }
    }
}