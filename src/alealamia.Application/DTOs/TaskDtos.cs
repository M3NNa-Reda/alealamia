using alealamia.Application.DTOs.TaskAssignments;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.DTOs.Tasks;

public class TaskDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int TrackId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
    public int? ActualDurationDays { get; set; }
    public DateTime PublishedDeadline { get; set; }
    public DateTime? ExtensionDeadline { get; set; }
    public DateTime EffectiveDeadline => ExtensionDeadline ?? PublishedDeadline;
    public int? AssignedFreelancerId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TaskDetailsDto : TaskDto
{
    public List<TaskRequiredSkillDto> RequiredSkills { get; set; } = new();
    public List<TaskFileDto> Files { get; set; } = new();
    public List<TaskStatusDto> StatusHistory { get; set; } = new();
    public List<TaskReassignmentDto> Reassignments { get; set; } = new();
}

public class CreateTaskDto
{
    public int ProjectId { get; set; }
    public int TrackId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime PublishedDeadline { get; set; }
    public List<int> RequiredSkillIds { get; set; } = new();
    public string CreatedByUserId { get; set; } = string.Empty;
}

public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime PublishedDeadline { get; set; }
}

public class TaskFilterDto
{
    public TaskStatus? Status { get; set; }
    public int? ProjectId { get; set; }
    public int? TrackId { get; set; }
    public int? AssignedFreelancerId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class TaskRequiredSkillDto
{
    public int TaskId { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
}

public class UpdateTaskRequiredSkillsDto
{
    public List<int> SkillIds { get; set; } = new();
}

public class TaskStatusDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public TaskStatus? OldStatus { get; set; }
    public TaskStatus NewStatus { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class UpdateTaskStatusDto
{
    public TaskStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
}

public class TaskFileDto
{
    public int TaskId { get; set; }
    public int FileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTime AddedAt { get; set; }
}

public class AddTaskFileDto
{
    public int FileId { get; set; }
}

public class TaskDeadlineDto
{
    public int TaskId { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime PublishedDeadline { get; set; }
    public DateTime? ExtensionDeadline { get; set; }
    public DateTime EffectiveDeadline { get; set; }
    public int? ActualDurationDays { get; set; }
    public bool IsOverdue { get; set; }
    public double? HoursRemaining { get; set; }
}

public class ExtendTaskDeadlineDto
{
    public DateTime NewDeadline { get; set; }
    public string? Reason { get; set; }
    public string ExtendedByUserId { get; set; } = string.Empty;
}
