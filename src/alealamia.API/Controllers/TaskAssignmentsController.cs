using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/admin/tasks/{taskId:int}")]
[Authorize(Roles = AppRoles.Admin)]
public class TaskAssignmentsController : ControllerBase
{
    private readonly ITaskAssignmentService _assignmentService;
    private readonly ITaskEligibilityService _eligibilityService;

    public TaskAssignmentsController(
        ITaskAssignmentService assignmentService,
        ITaskEligibilityService eligibilityService)
    {
        _assignmentService = assignmentService;
        _eligibilityService = eligibilityService;
    }

    [HttpGet("eligible-freelancers")]
    public async Task<ActionResult<IEnumerable<EligibleFreelancerDto>>> GetEligibleFreelancers(int taskId)
        => Ok(await _eligibilityService.GetEligibleFreelancersAsync(taskId));

    [HttpGet("eligibility/{freelancerProfileId:int}")]
    public async Task<ActionResult<TaskEligibilityDto>> CheckEligibility(int taskId, int freelancerProfileId)
        => Ok(await _eligibilityService.CheckAsync(taskId, freelancerProfileId));

    /// <summary>Assigns the task directly to a freelancer. The task id always comes from the route.</summary>
    [HttpPost("assign")]
    public async Task<ActionResult<TaskAssignmentResultDto>> Assign(int taskId, [FromBody] AssignTaskDto dto)
    {
        dto.TaskId = taskId;
        dto.AssignedByUserId = User.GetUserId();
        return Ok(await _assignmentService.AssignAsync(dto));
    }

    [HttpGet("assignment")]
    public async Task<ActionResult<TaskAssignmentDto>> GetCurrent(int taskId)
    {
        var assignment = await _assignmentService.GetCurrentAsync(taskId);
        return assignment is null ? NotFound() : Ok(assignment);
    }
}
