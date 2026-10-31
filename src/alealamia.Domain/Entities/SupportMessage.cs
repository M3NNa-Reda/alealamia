using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class SupportMessage
    {
        public int Id { get; set; }
        public int SupportTicketId { get; set; } // FK
        public string SenderUserId { get; set; } = string.Empty; // FK
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public SupportTicket SupportTicket { get; set; } = null!;
        public ICollection<SupportMessageFile> SupportMessageFiles { get; set; } = new List<SupportMessageFile>();
    }
}
