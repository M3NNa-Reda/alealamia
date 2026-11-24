using alealamia.API.Helpers.Extensions;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.API.Controllers;

/// <summary>Task endpoints for approved freelancers.</summary>
[ApiController]
[Route("api/tasks")]
[Authorize(Roles = AppRoles.Freelancer)]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskOfferService _offerService;
    private readonly IFreelancerProfileService _freelancerProfileService;

    public TasksController(
        ITaskService taskService,
        ITaskOfferService offerService,
        IFreelancerProfileService freelancerProfileService)
    {
        _taskService = taskService;
        _offerService = offerService;
        _freelancerProfileService = freelancerProfileService;
    }

    /// <summary>Published tasks in the freelancer's own track.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<FreelancerTaskDto>>> GetAvailable(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var me = await _freelancerProfileService.GetApprovedFreelancerAsync(User);

        var result = await _taskService.SearchAsync(new TaskFilterDto
        {
            Status = TaskStatus.Published,
            TrackId = me.TrackId,
            Page = page,
            PageSize = pageSize
        });

        return Ok(Map(result, me.Id));
    }

    /// <summary>Tasks assigned to the calling freelancer, optionally filtered by status.</summary>
    [HttpGet("my-tasks")]
    public async Task<ActionResult<PagedResultDto<FreelancerTaskDto>>> GetMyTasks(
        [FromQuery] TaskStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var me = await _freelancerProfileService.GetApprovedFreelancerAsync(User);

        var result = await _taskService.SearchAsync(new TaskFilterDto
        {
            AssignedFreelancerId = me.Id,
            Status = status,
            Page = page,
            PageSize = pageSize
        });

        return Ok(Map(result, me.Id));
    }

    /// <summary>
    /// A freelancer can open a task only when it is assigned to them, when they received an offer
    /// for it, or when it is published in their track. Anything else returns 404.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FreelancerTaskDetailsDto>> GetById(int id)
    {
        var me = await _freelancerProfileService.GetApprovedFreelancerAsync(User);

        var task = await _taskService.GetDetailsAsync(id);
        if (task is null)
            return NotFound();

        var visible = task.AssignedFreelancerId == me.Id
            || (task.Status == TaskStatus.Published && task.TrackId == me.TrackId)
            || await _offerService.HasOfferAsync(id, me.Id);

        return visible ? Ok(FreelancerTaskDetailsDto.FromDetails(task, me.Id)) : NotFound();
    }

    private static PagedResultDto<FreelancerTaskDto> Map(PagedResultDto<TaskDto> source, int freelancerProfileId)
        => new()
        {
            Items = source.Items.Select(t => FreelancerTaskDto.From(t, freelancerProfileId)).ToList(),
            TotalCount = source.TotalCount,
            Page = source.Page,
            PageSize = source.PageSize
        };
}
