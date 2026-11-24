using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
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

    [HttpPost("api/admin/tasks/{taskId:int}/reassign")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<TaskReassignmentDto>> Reassign(int taskId, [FromBody] CreateTaskReassignmentDto dto)
    {
        dto.TaskId = taskId;
        dto.ReassignedByUserId = User.GetUserId();
        return Ok(await _reassignmentService.ReassignAsync(dto));
    }

    [HttpGet("api/admin/tasks/{taskId:int}/reassignments")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IEnumerable<TaskReassignmentDto>>> GetByTask(int taskId)
        => Ok(await _reassignmentService.GetByTaskAsync(taskId));

    /// <summary>The assigned freelancer gives the task back. The freelancer id always comes from the token.</summary>
    [HttpPost("api/tasks/{taskId:int}/withdraw")]
    [Authorize(Roles = AppRoles.Freelancer)]
    public async Task<ActionResult<TaskReassignmentDto>> Withdraw(int taskId, [FromBody] WithdrawTaskDto dto)
    {
        dto.TaskId = taskId;
        dto.FreelancerProfileId = await _freelancerProfileService.GetProfileIdAsync(User);
        return Ok(await _reassignmentService.WithdrawAsync(dto));
    }
}
