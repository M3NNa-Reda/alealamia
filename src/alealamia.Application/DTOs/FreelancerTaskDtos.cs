using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.DTOs.Tasks;

/// <summary>
/// What a freelancer is allowed to see about a task. It deliberately leaves out
/// the project id, the creator and the internal history.
/// </summary>
public class FreelancerTaskDto
{
    public int Id { get; set; }
    public int TrackId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal RewardAmount { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime PublishedDeadline { get; set; }
    public DateTime? ExtensionDeadline { get; set; }
    public DateTime EffectiveDeadline => ExtensionDeadline ?? PublishedDeadline;
    public bool IsAssignedToMe { get; set; }

    public static FreelancerTaskDto From(TaskDto task, int freelancerProfileId) => new()
    {
        Id = task.Id,
        TrackId = task.TrackId,
        Title = task.Title,
        Description = task.Description,
        RewardAmount = task.RewardAmount,
        Status = task.Status,
        PublishedDeadline = task.PublishedDeadline,
        ExtensionDeadline = task.ExtensionDeadline,
        IsAssignedToMe = task.AssignedFreelancerId == freelancerProfileId
    };
}

public class FreelancerTaskDetailsDto : FreelancerTaskDto
{
    public List<TaskRequiredSkillDto> RequiredSkills { get; set; } = new();

    public static FreelancerTaskDetailsDto FromDetails(TaskDetailsDto task, int freelancerProfileId) => new()
    {
        Id = task.Id,
        TrackId = task.TrackId,
        Title = task.Title,
        Description = task.Description,
        RewardAmount = task.RewardAmount,
        Status = task.Status,
        PublishedDeadline = task.PublishedDeadline,
        ExtensionDeadline = task.ExtensionDeadline,
        IsAssignedToMe = task.AssignedFreelancerId == freelancerProfileId,
        RequiredSkills = task.RequiredSkills
    };
}
