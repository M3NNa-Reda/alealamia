using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Domain.Enums;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.DTOs.TaskOffers;

public class TaskOfferDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int FreelancerProfileId { get; set; }
    public TaskOfferStatus Status { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class TaskOfferDetailsDto : TaskOfferDto
{
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public decimal TaskRewardAmount { get; set; }
    public int TaskTrackId { get; set; }
    public TaskStatus TaskStatus { get; set; }
    public DateTime TaskPublishedDeadline { get; set; }
}

public class TaskOfferFilterDto
{
    public int? FreelancerProfileId { get; set; }
    public int? TaskId { get; set; }
    public TaskOfferStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateTaskOfferDto
{
    public int TaskId { get; set; }
    public int FreelancerProfileId { get; set; }
}

public class RespondToTaskOfferDto
{
    public int FreelancerProfileId { get; set; }
    public bool Accept { get; set; }
}

public class TaskOfferResponseDto
{
    public TaskOfferDto Offer { get; set; } = new();
    public TaskAssignmentResultDto? Assignment { get; set; }
}
