using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/admin/tasks")]
[Authorize(Roles = AppRoles.Admin)]
public class TaskDeadlinesController : ControllerBase
{
    private readonly ITaskDeadlineService _deadlineService;

    public TaskDeadlinesController(ITaskDeadlineService deadlineService)
    {
        _deadlineService = deadlineService;
    }

    [HttpGet("{taskId:int}/deadline")]
    public async Task<ActionResult<TaskDeadlineDto>> Get(int taskId)
        => Ok(await _deadlineService.GetAsync(taskId));

    [HttpPost("{taskId:int}/deadline/extend")]
    public async Task<ActionResult<TaskDeadlineDto>> Extend(int taskId, [FromBody] ExtendTaskDeadlineDto dto)
    {
        dto.ExtendedByUserId = User.GetUserId();
        return Ok(await _deadlineService.ExtendAsync(taskId, dto));
    }

    [HttpGet("deadlines/overdue")]
    public async Task<ActionResult<IEnumerable<TaskDeadlineDto>>> GetOverdue()
        => Ok(await _deadlineService.GetOverdueAsync());

    [HttpGet("deadlines/due-soon")]
    public async Task<ActionResult<IEnumerable<TaskDeadlineDto>>> GetDueSoon([FromQuery] int withinHours = 24)
        => Ok(await _deadlineService.GetDueSoonAsync(withinHours));
}
