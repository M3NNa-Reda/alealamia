using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/task-reassignments")]
public class TaskReassignmentsController : ControllerBase
{
    private readonly ITaskReassignmentService _reassignmentService;
    private readonly IFreelancerProfileService _freelancerProfileService;

    public TaskReassignmentsController(
        ITaskReassignmentService reassignmentService,
        IFreelancerProfileService freelancerProfileService)
    {
        _reassignmentService = reassignmentService;
        _freelancerProfileService = freelancerProfileService;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<TaskReassignmentDto>> Reassign([FromBody] CreateTaskReassignmentDto dto)
    {
        dto.ReassignedByUserId = User.GetUserId();
        return Ok(await _reassignmentService.ReassignAsync(dto));
    }

    [HttpPost("withdraw")]
    [Authorize(Roles = AppRoles.Freelancer)]
    public async Task<ActionResult<TaskReassignmentDto>> Withdraw([FromBody] WithdrawTaskDto dto)
    {
        dto.FreelancerProfileId = await _freelancerProfileService.GetProfileIdAsync(User);
        return Ok(await _reassignmentService.WithdrawAsync(dto));
    }

    [HttpGet("tasks/{taskId:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IEnumerable<TaskReassignmentDto>>> GetByTask(int taskId)
        => Ok(await _reassignmentService.GetByTaskAsync(taskId));
}
