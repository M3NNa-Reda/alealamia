using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskRevision
    {
        public int Id { get; set; } //pk
        public int TaskId { get; set; } //pk
        public int TaskReviewId { get; set; } //pk
        public string RevisionNotes { get; set; } = string.Empty;
        public DateTime NewDeadline { get; set; }
        //       type not founded
        //public RevisionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
