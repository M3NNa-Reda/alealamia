using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/task-deadlines")]
[Authorize(Roles = AppRoles.Admin)]
public class TaskDeadlinesController : ControllerBase
{
    private readonly ITaskDeadlineService _deadlineService;

    public TaskDeadlinesController(ITaskDeadlineService deadlineService)
    {
        _deadlineService = deadlineService;
    }

    [HttpGet("tasks/{taskId:int}")]
    public async Task<ActionResult<TaskDeadlineDto>> Get(int taskId)
        => Ok(await _deadlineService.GetAsync(taskId));

    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<TaskDeadlineDto>>> GetOverdue()
        => Ok(await _deadlineService.GetOverdueAsync());

    [HttpGet("due-soon")]
    public async Task<ActionResult<IEnumerable<TaskDeadlineDto>>> GetDueSoon([FromQuery] int withinHours = 24)
        => Ok(await _deadlineService.GetDueSoonAsync(withinHours));

    [HttpPost("tasks/{taskId:int}/extend")]
    public async Task<ActionResult<TaskDeadlineDto>> Extend(int taskId, [FromBody] ExtendTaskDeadlineDto dto)
    {
        dto.ExtendedByUserId = User.GetUserId();
        return Ok(await _deadlineService.ExtendAsync(taskId, dto));
    }
}
