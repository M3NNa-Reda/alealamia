using alealamia.Application.DTOs.Track;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackController : ControllerBase
    {
        private readonly ITrackService _trackService;

        public TrackController(ITrackService trackService)
        {
            _trackService = trackService;
        }

        [HttpGet("active")]
        [Authorize(Roles = AppRoles.Freelancer)]
        public async Task<IActionResult> GetActiveTracks()
        {
            var tracks = await _trackService.GetActiveTracksAsync();
            return Ok(tracks);
        }

        [HttpGet("admin")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetAllTracksForAdmin()
        {
            var tracks = await _trackService.GetAllTracksForAdminAsync();
            return Ok(tracks);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetTrackById(int id)
        {
            var track = await _trackService.GetTrackByIdAsync(id);

            if (track == null)
                return NotFound();

            return Ok(track);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateTrack(CreateTrackDto dto)
        {
            var result = await _trackService.CreateTrackAsync(dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateTrack(int id, UpdateTrackDto dto)
        {
            var result = await _trackService.UpdateTrackAsync(id, dto);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("{id:int}/deactivate")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeactivateTrack(int id)
        {
            var result = await _trackService.DeactivateTrackAsync(id);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("{id:int}/activate")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> ActivateTrack(int id)
        {
            var result = await _trackService.ActivateTrackAsync(id);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }
    }
}