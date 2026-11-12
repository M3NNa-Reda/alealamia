using alealamia.Domain.Enums;

namespace alealamia.Application.DTOs.Support
{
    public class SupportTicketDto
    {
        public int Id { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public SupportTicketStatus Status { get; set; }
        public SupportPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class SupportTicketDetailsDto : SupportTicketDto
    {
        public List<SupportMessageDto> Messages { get; set; } = new();
    }

    public class CreateSupportTicketDto
    {
        public string CreatedByUserId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public SupportPriority Priority { get; set; }
        public string InitialMessage { get; set; } = string.Empty;
    }

    public class UpdateSupportTicketStatusDto
    {
        public SupportTicketStatus Status { get; set; }
        public SupportPriority? Priority { get; set; }
    }

    public class SupportMessageDto
    {
        public int Id { get; set; }
        public int SupportTicketId { get; set; }
        public string SenderUserId { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public List<SupportMessageFileDto> Files { get; set; } = new();
    }

    public class CreateSupportMessageDto
    {
        public int SupportTicketId { get; set; }
        public string SenderUserId { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public List<int> FileIds { get; set; } = new();
    }

    public class SupportMessageFileDto
    {
        public int SupportMessageId { get; set; }
        public int FileId { get; set; }
    }
}