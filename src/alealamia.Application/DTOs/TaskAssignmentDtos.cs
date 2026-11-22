using alealamia.Domain.Enums;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.DTOs.TaskAssignments;

public class TaskAssignmentDto
{
    public int TaskId { get; set; }
    public int FreelancerProfileId { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime PublishedDeadline { get; set; }
    public DateTime? ExtensionDeadline { get; set; }
}

public class AssignTaskDto
{
    public int TaskId { get; set; }
    public int FreelancerProfileId { get; set; }
    public string AssignedByUserId { get; set; } = string.Empty;
}

public class TaskAssignmentResultDto
{
    public int TaskId { get; set; }
    public int FreelancerProfileId { get; set; }
    public int OfferId { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public int ClosedOfferCount { get; set; }
}

public class TaskEligibilityDto
{
    public int TaskId { get; set; }
    public int FreelancerProfileId { get; set; }
    public string FreelancerUserId { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    public List<string> Reasons { get; set; } = new();
    public List<int> MissingSkillIds { get; set; } = new();
    public int AcceptedThisMonth { get; set; }
    public int MonthlyLimit { get; set; }
}

public class EligibleFreelancerDto
{
    public int FreelancerProfileId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int TrackId { get; set; }
    public int AcceptedThisMonth { get; set; }
    public int RemainingThisMonth { get; set; }
}

public class FreelancerTaskLimitDto
{
    public int FreelancerProfileId { get; set; }
    public int MonthlyLimit { get; set; }
    public int AcceptedThisMonth { get; set; }
    public int Remaining { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class TaskReassignmentDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int? PreviousFreelancerId { get; set; }
    public int? NewFreelancerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReassignedByUserId { get; set; } = string.Empty;
    public DateTime ReassignedAt { get; set; }
}

public class CreateTaskReassignmentDto
{
    public int TaskId { get; set; }
    public int? NewFreelancerProfileId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReassignedByUserId { get; set; } = string.Empty;
}

public class WithdrawTaskDto
{
    public int TaskId { get; set; }
    public int FreelancerProfileId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class FreelancerEligibilitySnapshot
{
    public int FreelancerProfileId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int TrackId { get; set; }
    public FreelancerApprovalStatus ApprovalStatus { get; set; }
    public bool IsAvailable { get; set; }
    public List<int> SkillIds { get; set; } = new();
    public DateTime? ActiveBlockEndsAt { get; set; }
}
