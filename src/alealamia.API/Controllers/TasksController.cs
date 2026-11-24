using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Roles = AppRoles.Admin)]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskEligibilityService _eligibilityService;

    public TasksController(ITaskService taskService, ITaskEligibilityService eligibilityService)
    {
        _taskService = taskService;
        _eligibilityService = eligibilityService;
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

    [HttpGet("{id:int}/files")]
    public async Task<ActionResult<IEnumerable<TaskFileDto>>> GetFiles(int id)
        => Ok(await _taskService.GetFilesAsync(id));

    [HttpPost("{id:int}/files")]
    public async Task<ActionResult<TaskFileDto>> AddFile(int id, [FromBody] AddTaskFileDto dto)
    {
        var file = await _taskService.AddFileAsync(id, dto);
        return CreatedAtAction(nameof(GetFiles), new { id }, file);
    }

    [HttpDelete("{id:int}/files/{fileId:int}")]
    public async Task<IActionResult> RemoveFile(int id, int fileId)
    {
        await _taskService.RemoveFileAsync(id, fileId);
        return NoContent();
    }

    [HttpGet("{id:int}/eligible-freelancers")]
    public async Task<ActionResult<IEnumerable<EligibleFreelancerDto>>> GetEligibleFreelancers(int id)
        => Ok(await _eligibilityService.GetEligibleFreelancersAsync(id));

    [HttpGet("{id:int}/eligibility/{freelancerProfileId:int}")]
    public async Task<ActionResult<TaskEligibilityDto>> CheckEligibility(int id, int freelancerProfileId)
        => Ok(await _eligibilityService.CheckAsync(id, freelancerProfileId));
}
