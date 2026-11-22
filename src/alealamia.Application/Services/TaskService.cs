using alealamia.Application.Common;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Mappings;
using alealamia.Domain.Constants;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using FileEntity = alealamia.Domain.Entities.File;
using Task = System.Threading.Tasks.Task;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Services;

public class TaskService : ITaskService
{
    private static readonly IReadOnlyDictionary<TaskStatus, TaskStatus[]> AllowedTransitions =
        new Dictionary<TaskStatus, TaskStatus[]>
        {
            [TaskStatus.Draft] = [TaskStatus.Published, TaskStatus.Cancelled],
            [TaskStatus.Published] = [TaskStatus.Cancelled],
            [TaskStatus.Assigned] = [TaskStatus.Submitted, TaskStatus.Cancelled],
            [TaskStatus.Submitted] = [TaskStatus.RevisionRequested, TaskStatus.Completed],
            [TaskStatus.RevisionRequested] = [TaskStatus.Submitted, TaskStatus.Cancelled],
            [TaskStatus.Completed] = [],
            [TaskStatus.Cancelled] = []
        };

    private readonly ITaskRepository _taskRepository;
    private readonly ITaskRequiredSkillRepository _requiredSkillRepository;
    private readonly ITaskStatusHistoryRepository _historyRepository;
    private readonly ITaskFileRepository _taskFileRepository;
    private readonly ITaskOfferRepository _offerRepository;
    private readonly ITaskOfferService _offerService;
    private readonly IProjectRepository _projectRepository;
    private readonly IGenericRepository<Track> _trackRepository;
    private readonly IGenericRepository<Skill> _skillRepository;
    private readonly IGenericRepository<FileEntity> _fileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public TaskService(
        ITaskRepository taskRepository,
        ITaskRequiredSkillRepository requiredSkillRepository,
        ITaskStatusHistoryRepository historyRepository,
        ITaskFileRepository taskFileRepository,
        ITaskOfferRepository offerRepository,
        ITaskOfferService offerService,
        IProjectRepository projectRepository,
        IGenericRepository<Track> trackRepository,
        IGenericRepository<Skill> skillRepository,
        IGenericRepository<FileEntity> fileRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _requiredSkillRepository = requiredSkillRepository;
        _historyRepository = historyRepository;
        _taskFileRepository = taskFileRepository;
        _offerRepository = offerRepository;
        _offerService = offerService;
        _projectRepository = projectRepository;
        _trackRepository = trackRepository;
        _skillRepository = skillRepository;
        _fileRepository = fileRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<TaskDetailsDto?> GetDetailsAsync(int id)
    {
        var task = await _taskRepository.GetDetailsByIdAsync(id);
        return task?.ToDetailsDto();
    }

    public async Task<PagedResultDto<TaskDto>> SearchAsync(TaskFilterDto filter)
    {
        var (page, pageSize) = Paging.Normalize(filter.Page, filter.PageSize);
        var (items, total) = await _taskRepository.SearchAsync(
            filter.Status,
            filter.ProjectId,
            filter.TrackId,
            filter.AssignedFreelancerId,
            page,
            pageSize);

        return new PagedResultDto<TaskDto>
        {
            Items = items.Select(t => t.ToDto()).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<TaskDto>> GetByProjectAsync(int projectId)
    {
        var tasks = await _taskRepository.GetByProjectIdAsync(projectId);
        return tasks.Select(t => t.ToDto()).ToList();
    }

    public async Task<TaskDto> CreateAsync(CreateTaskDto dto)
    {
        var now = Now();

        ValidateContent(dto.Title, dto.Description);
        if (string.IsNullOrWhiteSpace(dto.CreatedByUserId))
            throw new ArgumentException("CreatedByUserId is required.");
        if (dto.PublishedDeadline <= now)
            throw new ArgumentException("The deadline must be in the future.");

        var skillIds = NormalizeSkillIds(dto.RequiredSkillIds);

        var project = await _projectRepository.GetByIdAsync(dto.ProjectId)
            ?? throw new KeyNotFoundException($"Project {dto.ProjectId} was not found.");

        if (project.Status != ProjectStatus.PriceSet && project.Status != ProjectStatus.InProgress)
            throw new InvalidOperationException("Tasks can only be created for a project whose price has been approved.");

        var track = await _trackRepository.GetByIdAsync(dto.TrackId);
        if (track is null || !track.IsActive)
            throw new InvalidOperationException($"Track {dto.TrackId} is not available.");

        await EnsureSkillsAvailableAsync(skillIds);

        var task = new TaskEntity
        {
            ProjectId = dto.ProjectId,
            TrackId = dto.TrackId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Status = TaskStatus.Draft,
            PublishedDeadline = dto.PublishedDeadline,
            CreatedByUserId = dto.CreatedByUserId,
            CreatedAt = now
        };

        foreach (var skillId in skillIds)
            task.TaskRequiredSkills.Add(new TaskRequiredSkill { SkillId = skillId });

        task.TaskStatusHistories.Add(new TaskStatusHistory
        {
            OldStatus = null,
            NewStatus = TaskStatus.Draft,
            ChangedByUserId = dto.CreatedByUserId,
            Notes = "Task created.",
            ChangedAt = now
        });

        await _taskRepository.AddAsync(task);
        await _unitOfWork.CompleteAsync();
        return task.ToDto();
    }

    public async Task<TaskDto> UpdateAsync(int id, UpdateTaskDto dto)
    {
        var task = await _taskRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");

        if (task.Status != TaskStatus.Draft)
            throw new InvalidOperationException("Only a draft task can be edited.");

        ValidateContent(dto.Title, dto.Description);
        if (dto.PublishedDeadline <= Now())
            throw new ArgumentException("The deadline must be in the future.");

        task.Title = dto.Title.Trim();
        task.Description = dto.Description.Trim();
        task.PublishedDeadline = dto.PublishedDeadline;
        task.UpdatedAt = Now();

        await _unitOfWork.CompleteAsync();
        return task.ToDto();
    }

    public async Task<IEnumerable<TaskRequiredSkillDto>> UpdateRequiredSkillsAsync(int taskId, UpdateTaskRequiredSkillsDto dto)
    {
        var task = await _taskRepository.GetReadOnlyByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        if (task.Status != TaskStatus.Draft)
            throw new InvalidOperationException("Required skills can only be changed while the task is a draft.");

        var skillIds = NormalizeSkillIds(dto.SkillIds);
        await EnsureSkillsAvailableAsync(skillIds);

        var current = await _requiredSkillRepository.GetByTaskIdAsync(taskId);
        var currentIds = current.Select(s => s.SkillId).ToHashSet();

        var toRemove = current.Where(s => !skillIds.Contains(s.SkillId)).ToList();
        var toAdd = skillIds
            .Where(id => !currentIds.Contains(id))
            .Select(id => new TaskRequiredSkill { TaskId = taskId, SkillId = id })
            .ToList();

        _requiredSkillRepository.DeleteRange(toRemove);
        await _requiredSkillRepository.AddRangeAsync(toAdd);
        await _unitOfWork.CompleteAsync();

        var updated = await _requiredSkillRepository.GetByTaskIdAsync(taskId);
        return updated.Select(s => s.ToDto()).ToList();
    }

    public async Task<TaskDto> UpdateStatusAsync(int taskId, UpdateTaskStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ChangedByUserId))
            throw new ArgumentException("ChangedByUserId is required.");

        var task = await _taskRepository.GetReadOnlyByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        var oldStatus = task.Status;
        if (dto.NewStatus == oldStatus)
            throw new InvalidOperationException("The task is already in this status.");

        if (!AllowedTransitions[oldStatus].Contains(dto.NewStatus))
            throw new InvalidOperationException($"Cannot move a task from {oldStatus} to {dto.NewStatus}.");

        var now = Now();
        if (dto.NewStatus == TaskStatus.Published && task.PublishedDeadline <= now)
            throw new InvalidOperationException("The deadline has already passed. Update it before publishing.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var changed = await _taskRepository.TryChangeStatusAsync(taskId, oldStatus, dto.NewStatus, now);
            if (!changed)
                throw new InvalidOperationException("The task status was changed by someone else. Reload and try again.");

            await _historyRepository.AddAsync(new TaskStatusHistory
            {
                TaskId = taskId,
                OldStatus = oldStatus,
                NewStatus = dto.NewStatus,
                ChangedByUserId = dto.ChangedByUserId,
                Notes = dto.Notes,
                ChangedAt = now
            });
            await _unitOfWork.CompleteAsync();

            if (dto.NewStatus == TaskStatus.Published)
                await _offerService.SendOffersAsync(taskId);

            if (dto.NewStatus == TaskStatus.Cancelled)
                await _offerRepository.ClosePendingAsync(taskId, null, now);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        task.Status = dto.NewStatus;
        task.UpdatedAt = now;
        return task.ToDto();
    }

    public async Task<IEnumerable<TaskStatusDto>> GetStatusHistoryAsync(int taskId)
    {
        var history = await _historyRepository.GetByTaskIdAsync(taskId);
        return history.Select(h => h.ToDto()).ToList();
    }

    public async Task<IEnumerable<TaskFileDto>> GetFilesAsync(int taskId)
    {
        var files = await _taskFileRepository.GetByTaskIdAsync(taskId);
        return files.Select(f => f.ToDto()).ToList();
    }

    public async Task<TaskFileDto> AddFileAsync(int taskId, AddTaskFileDto dto)
    {
        var task = await _taskRepository.GetReadOnlyByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        if (task.Status == TaskStatus.Completed || task.Status == TaskStatus.Cancelled)
            throw new InvalidOperationException("Files cannot be added to a closed task.");

        var file = await _fileRepository.GetByIdAsync(dto.FileId)
            ?? throw new KeyNotFoundException($"File {dto.FileId} was not found.");

        var existing = await _taskFileRepository.GetAsync(taskId, dto.FileId);
        if (existing is not null)
            throw new InvalidOperationException("This file is already attached to the task.");

        var taskFile = new TaskFile
        {
            TaskId = taskId,
            FileId = dto.FileId,
            AddedAt = Now()
        };

        await _taskFileRepository.AddAsync(taskFile);
        await _unitOfWork.CompleteAsync();

        taskFile.File = file;
        return taskFile.ToDto();
    }

    public async Task RemoveFileAsync(int taskId, int fileId)
    {
        var taskFile = await _taskFileRepository.GetAsync(taskId, fileId)
            ?? throw new KeyNotFoundException("The file is not attached to this task.");

        _taskFileRepository.Delete(taskFile);
        await _unitOfWork.CompleteAsync();
    }

    private async Task EnsureSkillsAvailableAsync(IReadOnlyCollection<int> skillIds)
    {
        foreach (var skillId in skillIds)
        {
            var skill = await _skillRepository.GetByIdAsync(skillId);
            if (skill is null || !skill.IsActive)
                throw new InvalidOperationException($"Skill {skillId} is not available.");
        }
    }

    private static List<int> NormalizeSkillIds(IReadOnlyCollection<int>? skillIds)
    {
        var ids = skillIds?.ToList() ?? new List<int>();
        if (ids.Count != ids.Distinct().Count())
            throw new ArgumentException("The same skill cannot be selected twice.");
        return ids;
    }

    private static void ValidateContent(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.");
        if (title.Trim().Length > TaskRules.TitleMaxLength)
            throw new ArgumentException($"Task title cannot exceed {TaskRules.TitleMaxLength} characters.");
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Task description is required.");
    }

    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;
}
