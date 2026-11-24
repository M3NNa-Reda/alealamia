using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/task-offers")]
public class TaskOffersController : ControllerBase
{
    private readonly ITaskOfferService _offerService;
    private readonly ITaskEligibilityService _eligibilityService;
    private readonly IFreelancerProfileService _freelancerProfileService;

    public TaskOffersController(
        ITaskOfferService offerService,
        ITaskEligibilityService eligibilityService,
        IFreelancerProfileService freelancerProfileService)
    {
        _offerService = offerService;
        _eligibilityService = eligibilityService;
        _freelancerProfileService = freelancerProfileService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Freelancer}")]
    public async Task<ActionResult<PagedResultDto<TaskOfferDetailsDto>>> Search([FromQuery] TaskOfferFilterDto filter)
    {
        if (User.IsInRole(AppRoles.Freelancer))
            filter.FreelancerProfileId = await _freelancerProfileService.GetProfileIdAsync(User);

        return Ok(await _offerService.SearchAsync(filter));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Freelancer}")]
    public async Task<ActionResult<TaskOfferDetailsDto>> GetById(int id)
    {
        var offer = await _offerService.GetDetailsAsync(id);
        if (offer is null)
            return NotFound();

        if (User.IsInRole(AppRoles.Freelancer)
            && offer.FreelancerProfileId != await _freelancerProfileService.GetProfileIdAsync(User))
            return NotFound();

        return Ok(offer);
    }

    [HttpPost("{id:int}/respond")]
    [Authorize(Roles = AppRoles.Freelancer)]
    public async Task<ActionResult<TaskOfferResponseDto>> Respond(int id, [FromBody] RespondToTaskOfferDto dto)
    {
        dto.FreelancerProfileId = await _freelancerProfileService.GetProfileIdAsync(User);
        return Ok(await _offerService.RespondAsync(id, dto));
    }

    [HttpGet("my-limit")]
    [Authorize(Roles = AppRoles.Freelancer)]
    public async Task<ActionResult<FreelancerTaskLimitDto>> GetMyLimit()
    {
        var profileId = await _freelancerProfileService.GetProfileIdAsync(User);
        return Ok(await _eligibilityService.GetMonthlyLimitAsync(profileId));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<TaskOfferDto>> Create([FromBody] CreateTaskOfferDto dto)
    {
        var created = await _offerService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("tasks/{taskId:int}/send")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IEnumerable<TaskOfferDto>>> SendOffers(int taskId)
        => Ok(await _offerService.SendOffersAsync(taskId));
}
