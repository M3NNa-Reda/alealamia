using System;
using System.Collections.Generic;
using System.Text;
using alealamia.Domain.Enums;

namespace alealamia.Domain.Entities
{
    public class SupportTicket
    {
        public int Id { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty; // FK
        public string Subject { get; set; } = string.Empty;
        public SupportTicketStatus Status { get; set; }
        public SupportPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public ICollection<SupportMessage> SupportMessages { get; set; } = new List<SupportMessage>();
    }
}
