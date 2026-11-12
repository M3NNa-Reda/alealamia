using alealamia.Domain.Enums;

namespace alealamia.Application.DTOs.Meetings
{
    public class MeetingDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ClientProfileId { get; set; }
        public DateTime ProposedAt { get; set; }
        public MeetingStatus Status { get; set; }
        public string? AdminNotes { get; set; }
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMeetingDto
    {
        public int ProjectId { get; set; }
        public int ClientProfileId { get; set; }
        public DateTime ProposedAt { get; set; }
    }

    public class ReviewMeetingDto
    {
        public int MeetingId { get; set; }
        public MeetingStatus Status { get; set; }   
        public string? AdminNotes { get; set; }
        public string ReviewedByUserId { get; set; } = string.Empty;
    }

    public class MeetingStatusDto
    {
        public int MeetingId { get; set; }
        public MeetingStatus Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}