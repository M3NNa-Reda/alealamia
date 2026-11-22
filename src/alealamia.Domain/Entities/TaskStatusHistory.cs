using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Domain.Entities
{
    public class TaskStatusHistory
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public TaskStatus? OldStatus { get; set; }
        public TaskStatus NewStatus { get; set; }
        public string ChangedByUserId { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public Task Task { get; set; } = null!;

    }
}
