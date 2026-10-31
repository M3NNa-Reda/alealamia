using System;
using System.Collections.Generic;
using System.Text;
using alealamia.Domain.Enums;

namespace alealamia.Domain.Entities
{
    public class EmailNotificationLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // FK
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public EmailDeliveryStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
    }
}
