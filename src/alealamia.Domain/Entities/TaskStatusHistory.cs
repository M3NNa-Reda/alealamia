using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskStatusHistory
    {
        public int Id { get; set; }//pk
        public int TaskId { get; set; }//fk
        public TaskStatus? OldStatus { get; set; }
        public TaskStatus NewStatus { get; set; }
        public string ChangedByUserId { get; set; } = string.Empty;//fk
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; }

    }
}
