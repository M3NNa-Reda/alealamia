using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace alealamia.API.Controllers;

[ApiController]
[Route("api/tasks/{taskId:int}/files")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Freelancer}")]
public class TaskFilesController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskOfferService _offerService;
    private readonly IFileService _fileService;
    private readonly IFreelancerProfileService _freelancerProfileService;

    public TaskFilesController(
        ITaskService taskService,
        ITaskOfferService offerService,
        IFileService fileService,
        IFreelancerProfileService freelancerProfileService)
    {
        _taskService = taskService;
        _offerService = offerService;
        _fileService = fileService;
        _freelancerProfileService = freelancerProfileService;
    }

    /// <summary>Admins see the files of any task. A freelancer sees them only for a task assigned to them or offered to them.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskFileDto>>> GetFiles(int taskId)
    {
        var task = await _taskService.GetDetailsAsync(taskId);
        if (task is null)
            return NotFound();

        if (User.IsInRole(AppRoles.Freelancer))
        {
            var me = await _freelancerProfileService.GetApprovedFreelancerAsync(User);
            var related = task.AssignedFreelancerId == me.Id
                || await _offerService.HasOfferAsync(taskId, me.Id);

            if (!related)
                return NotFound();
        }

        return Ok(await _taskService.GetFilesAsync(taskId));
    }

    /// <summary>
    /// Downloads a file attached to the task. Same access rules as the file list: admins for any task, a freelancer
    /// only for a task assigned or offered to them. The file is always sent as an attachment, never rendered.
    /// </summary>
    [HttpGet("{fileId:int}/download")]
    public async Task<IActionResult> Download(int taskId, int fileId)
    {
        var task = await _taskService.GetDetailsAsync(taskId);
        if (task is null)
            return NotFound();

        if (User.IsInRole(AppRoles.Freelancer))
        {
            var me = await _freelancerProfileService.GetApprovedFreelancerAsync(User);
            var related = task.AssignedFreelancerId == me.Id
                || await _offerService.HasOfferAsync(taskId, me.Id);

            if (!related)
                return NotFound();
        }

        var attached = (await _taskService.GetFilesAsync(taskId)).Any(f => f.FileId == fileId);
        if (!attached)
            return NotFound();

        var content = await _fileService.OpenAsync(fileId);
        if (content is null)
            return NotFound();

        return File(content.Content, content.ContentType, content.FileName);
    }

    /// <summary>
    /// Attaches an already uploaded file to the task. A freelancer may do this only for the task
    /// assigned to them and only with a file they uploaded themselves.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskFileDto>> AddFile(int taskId, [FromBody] AddTaskFileDto dto)
    {
        if (User.IsInRole(AppRoles.Freelancer))
        {
            var me = await _freelancerProfileService.GetApprovedFreelancerAsync(User);

            var task = await _taskService.GetDetailsAsync(taskId);
            if (task is null || task.AssignedFreelancerId != me.Id)
                return NotFound();

            var file = await _fileService.GetByIdAsync(dto.FileId);
            if (file is null || file.UploadedByUserId != User.GetUserId())
                return NotFound();
        }

        var created = await _taskService.AddFileAsync(taskId, dto);
        return CreatedAtAction(nameof(GetFiles), new { taskId }, created);
    }
}
