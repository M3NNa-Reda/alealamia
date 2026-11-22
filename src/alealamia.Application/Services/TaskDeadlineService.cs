using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Mappings;
using alealamia.Domain.Constants;
using alealamia.Domain.Entities;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Services;

public class TaskDeadlineService : ITaskDeadlineService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskStatusHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public TaskDeadlineService(
        ITaskRepository taskRepository,
        ITaskStatusHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<TaskDeadlineDto> GetAsync(int taskId)
    {
        var task = await _taskRepository.GetReadOnlyByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        return task.ToDeadlineDto(Now());
    }

    public async Task<IEnumerable<TaskDeadlineDto>> GetOverdueAsync()
    {
        var now = Now();
        var tasks = await _taskRepository.GetOverdueAsync(now);
        return tasks.Select(t => t.ToDeadlineDto(now)).ToList();
    }

    public async Task<IEnumerable<TaskDeadlineDto>> GetDueSoonAsync(int withinHours)
    {
        if (withinHours < 1 || withinHours > TaskRules.MaxDueSoonHours)
            throw new ArgumentException($"The window must be between 1 and {TaskRules.MaxDueSoonHours} hours.");

        var now = Now();
        var tasks = await _taskRepository.GetDueBetweenAsync(now, now.AddHours(withinHours));
        return tasks.Select(t => t.ToDeadlineDto(now)).ToList();
    }

    public async Task<TaskDeadlineDto> ExtendAsync(int taskId, ExtendTaskDeadlineDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ExtendedByUserId))
            throw new ArgumentException("ExtendedByUserId is required.");

        var task = await _taskRepository.GetByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        if (task.Status != TaskStatus.Assigned && task.Status != TaskStatus.RevisionRequested)
            throw new InvalidOperationException("The deadline can only be extended for a task that is in progress.");

        var now = Now();
        var current = task.ExtensionDeadline ?? task.PublishedDeadline;
        if (dto.NewDeadline <= current || dto.NewDeadline <= now)
            throw new ArgumentException("The new deadline must be later than the current deadline and in the future.");

        task.ExtensionDeadline = dto.NewDeadline;
        task.UpdatedAt = now;

        var notes = $"Deadline extended to {dto.NewDeadline:yyyy-MM-dd HH:mm} UTC.";
        if (!string.IsNullOrWhiteSpace(dto.Reason))
            notes = $"{notes} {dto.Reason.Trim()}";

        await _historyRepository.AddAsync(new TaskStatusHistory
        {
            TaskId = taskId,
            OldStatus = task.Status,
            NewStatus = task.Status,
            ChangedByUserId = dto.ExtendedByUserId,
            Notes = notes,
            ChangedAt = now
        });

        await _unitOfWork.CompleteAsync();
        return task.ToDeadlineDto(now);
    }

    public async Task<TaskDeadlineDto> RecordActualDurationAsync(int taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        if (task.Status != TaskStatus.Completed)
            throw new InvalidOperationException("The actual duration can only be recorded for a completed task.");

        var now = Now();
        var assigned = await _historyRepository.GetLatestAsync(taskId, TaskStatus.Assigned)
            ?? throw new InvalidOperationException("The task has no assignment record.");
        var completed = await _historyRepository.GetLatestAsync(taskId, TaskStatus.Completed);

        var finishedAt = completed?.ChangedAt ?? now;
        var days = (int)Math.Ceiling((finishedAt - assigned.ChangedAt).TotalDays);

        task.ActualDurationDays = Math.Max(1, days);
        task.UpdatedAt = now;

        await _unitOfWork.CompleteAsync();
        return task.ToDeadlineDto(now);
    }

    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;
}
