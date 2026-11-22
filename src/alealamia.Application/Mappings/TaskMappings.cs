using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.DTOs.Tasks;
using alealamia.Domain.Entities;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Mappings;

internal static class TaskMappings
{
    public static TaskDto ToDto(this TaskEntity task) => Fill(new TaskDto(), task);

    public static TaskDetailsDto ToDetailsDto(this TaskEntity task)
    {
        var dto = Fill(new TaskDetailsDto(), task);
        dto.RequiredSkills = task.TaskRequiredSkills.Select(s => s.ToDto()).ToList();
        dto.Files = task.TaskFiles.OrderBy(f => f.AddedAt).Select(f => f.ToDto()).ToList();
        dto.StatusHistory = task.TaskStatusHistories
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .Select(h => h.ToDto())
            .ToList();
        dto.Reassignments = task.TaskReassignments
            .OrderByDescending(r => r.ReassignedAt)
            .ThenByDescending(r => r.Id)
            .Select(r => r.ToDto())
            .ToList();
        return dto;
    }

    public static TaskRequiredSkillDto ToDto(this TaskRequiredSkill skill) => new()
    {
        TaskId = skill.TaskId,
        SkillId = skill.SkillId,
        SkillName = skill.Skill?.Name ?? string.Empty
    };

    public static TaskStatusDto ToDto(this TaskStatusHistory history) => new()
    {
        Id = history.Id,
        TaskId = history.TaskId,
        OldStatus = history.OldStatus,
        NewStatus = history.NewStatus,
        ChangedByUserId = history.ChangedByUserId,
        Notes = history.Notes,
        ChangedAt = history.ChangedAt
    };

    public static TaskFileDto ToDto(this TaskFile taskFile) => new()
    {
        TaskId = taskFile.TaskId,
        FileId = taskFile.FileId,
        OriginalFileName = taskFile.File?.OriginalFileName ?? string.Empty,
        FileUrl = taskFile.File?.FileUrl ?? string.Empty,
        ContentType = taskFile.File?.ContentType ?? string.Empty,
        SizeInBytes = taskFile.File?.SizeInBytes ?? 0,
        AddedAt = taskFile.AddedAt
    };

    public static TaskOfferDto ToDto(this TaskOffer offer) => new()
    {
        Id = offer.Id,
        TaskId = offer.TaskId,
        FreelancerProfileId = offer.FreelancerProfileId,
        Status = offer.Status,
        SentAt = offer.SentAt,
        RespondedAt = offer.RespondedAt
    };

    public static TaskOfferDetailsDto ToDetailsDto(this TaskOffer offer) => new()
    {
        Id = offer.Id,
        TaskId = offer.TaskId,
        FreelancerProfileId = offer.FreelancerProfileId,
        Status = offer.Status,
        SentAt = offer.SentAt,
        RespondedAt = offer.RespondedAt,
        TaskTitle = offer.Task?.Title ?? string.Empty,
        TaskDescription = offer.Task?.Description ?? string.Empty,
        TaskTrackId = offer.Task?.TrackId ?? 0,
        TaskStatus = offer.Task?.Status ?? TaskStatus.Draft,
        TaskPublishedDeadline = offer.Task?.PublishedDeadline ?? default
    };

    public static TaskReassignmentDto ToDto(this TaskReassignment reassignment) => new()
    {
        Id = reassignment.Id,
        TaskId = reassignment.TaskId,
        PreviousFreelancerId = reassignment.PreviousFreelancerId,
        NewFreelancerId = reassignment.NewFreelancerId,
        Reason = reassignment.Reason,
        ReassignedByUserId = reassignment.ReassignedByUserId,
        ReassignedAt = reassignment.ReassignedAt
    };

    public static TaskDeadlineDto ToDeadlineDto(this TaskEntity task, DateTime utcNow)
    {
        var effective = task.ExtensionDeadline ?? task.PublishedDeadline;
        var running = task.Status == TaskStatus.Assigned || task.Status == TaskStatus.RevisionRequested;

        return new TaskDeadlineDto
        {
            TaskId = task.Id,
            Status = task.Status,
            PublishedDeadline = task.PublishedDeadline,
            ExtensionDeadline = task.ExtensionDeadline,
            EffectiveDeadline = effective,
            ActualDurationDays = task.ActualDurationDays,
            IsOverdue = running && effective < utcNow,
            HoursRemaining = running ? Math.Round((effective - utcNow).TotalHours, 2) : null
        };
    }

    private static T Fill<T>(T dto, TaskEntity task) where T : TaskDto
    {
        dto.Id = task.Id;
        dto.ProjectId = task.ProjectId;
        dto.TrackId = task.TrackId;
        dto.Title = task.Title;
        dto.Description = task.Description;
        dto.Status = task.Status;
        dto.ActualDurationDays = task.ActualDurationDays;
        dto.PublishedDeadline = task.PublishedDeadline;
        dto.ExtensionDeadline = task.ExtensionDeadline;
        dto.AssignedFreelancerId = task.AssignedFreelancerId;
        dto.CreatedByUserId = task.CreatedByUserId;
        dto.CreatedAt = task.CreatedAt;
        dto.UpdatedAt = task.UpdatedAt;
        return dto;
    }
}
