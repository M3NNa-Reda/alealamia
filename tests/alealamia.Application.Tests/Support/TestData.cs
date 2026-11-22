using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Domain.Enums;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Tests.Support;

internal static class TestData
{
    public static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    public static TaskEntity NewTask(
        int id = 1,
        TaskStatus status = TaskStatus.Published,
        int trackId = 1,
        int? assignedFreelancerId = null,
        DateTime? deadline = null,
        DateTime? extension = null)
    {
        return new TaskEntity
        {
            Id = id,
            ProjectId = 10,
            TrackId = trackId,
            Title = "Design the landing page",
            Description = "Create the landing page layout.",
            Status = status,
            PublishedDeadline = deadline ?? Now.AddDays(5),
            ExtensionDeadline = extension,
            AssignedFreelancerId = assignedFreelancerId,
            CreatedByUserId = "admin-1",
            CreatedAt = Now.AddDays(-1)
        };
    }

    public static FreelancerEligibilitySnapshot NewFreelancer(
        int id = 5,
        int trackId = 1,
        FreelancerApprovalStatus approval = FreelancerApprovalStatus.Approved,
        bool available = true,
        DateTime? blockedUntil = null,
        params int[] skillIds)
    {
        return new FreelancerEligibilitySnapshot
        {
            FreelancerProfileId = id,
            UserId = $"user-{id}",
            TrackId = trackId,
            ApprovalStatus = approval,
            IsAvailable = available,
            ActiveBlockEndsAt = blockedUntil,
            SkillIds = skillIds.Length == 0 ? new List<int> { 1, 2 } : skillIds.ToList()
        };
    }

    public static TaskEligibilityDto Eligible(int taskId, int freelancerId) => new()
    {
        TaskId = taskId,
        FreelancerProfileId = freelancerId,
        FreelancerUserId = $"user-{freelancerId}",
        IsEligible = true,
        MonthlyLimit = 10
    };

    public static TaskEligibilityDto NotEligible(int taskId, int freelancerId, string reason) => new()
    {
        TaskId = taskId,
        FreelancerProfileId = freelancerId,
        FreelancerUserId = $"user-{freelancerId}",
        IsEligible = false,
        Reasons = new List<string> { reason },
        MonthlyLimit = 10
    };
}
