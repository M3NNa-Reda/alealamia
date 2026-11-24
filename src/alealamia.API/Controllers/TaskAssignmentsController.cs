using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/task-assignments")]
[Authorize(Roles = AppRoles.Admin)]
public class TaskAssignmentsController : ControllerBase
{
    private readonly ITaskAssignmentService _assignmentService;

    public TaskAssignmentsController(ITaskAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpPost]
    public async Task<ActionResult<TaskAssignmentResultDto>> Assign([FromBody] AssignTaskDto dto)
    {
        dto.AssignedByUserId = User.GetUserId();
        return Ok(await _assignmentService.AssignAsync(dto));
    }

    [HttpGet("tasks/{taskId:int}/current")]
    public async Task<ActionResult<TaskAssignmentDto>> GetCurrent(int taskId)
    {
        var assignment = await _assignmentService.GetCurrentAsync(taskId);
        return assignment is null ? NotFound() : Ok(assignment);
    }
}
