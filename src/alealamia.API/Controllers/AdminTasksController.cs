using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/admin/tasks")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminTasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public AdminTasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<TaskDto>>> Search([FromQuery] TaskFilterDto filter)
        => Ok(await _taskService.SearchAsync(filter));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDetailsDto>> GetById(int id)
    {
        var task = await _taskService.GetDetailsAsync(id);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpGet("by-project/{projectId:int}")]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetByProject(int projectId)
        => Ok(await _taskService.GetByProjectAsync(projectId));

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskDto dto)
    {
        dto.CreatedByUserId = User.GetUserId();
        var created = await _taskService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskDto>> Update(int id, [FromBody] UpdateTaskDto dto)
        => Ok(await _taskService.UpdateAsync(id, dto));

    /// <summary>
    /// Cancels the task. Tasks are never physically deleted so the history, the offers and the
    /// money trail stay intact. A task that was already submitted or completed cannot be cancelled (409).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string? reason)
    {
        await _taskService.UpdateStatusAsync(id, new UpdateTaskStatusDto
        {
            NewStatus = TaskStatus.Cancelled,
            Notes = string.IsNullOrWhiteSpace(reason) ? "Cancelled by admin." : reason.Trim(),
            ChangedByUserId = User.GetUserId()
        });

        return NoContent();
    }

    [HttpPut("{id:int}/required-skills")]
    public async Task<ActionResult<IEnumerable<TaskRequiredSkillDto>>> UpdateRequiredSkills(
        int id,
        [FromBody] UpdateTaskRequiredSkillsDto dto)
        => Ok(await _taskService.UpdateRequiredSkillsAsync(id, dto));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TaskDto>> UpdateStatus(int id, [FromBody] UpdateTaskStatusDto dto)
    {
        dto.ChangedByUserId = User.GetUserId();
        return Ok(await _taskService.UpdateStatusAsync(id, dto));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IEnumerable<TaskStatusDto>>> GetHistory(int id)
        => Ok(await _taskService.GetStatusHistoryAsync(id));

    [HttpDelete("{id:int}/files/{fileId:int}")]
    public async Task<IActionResult> RemoveFile(int id, int fileId)
    {
        await _taskService.RemoveFileAsync(id, fileId);
        return NoContent();
    }
}
