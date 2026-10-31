using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskRevision
    {
        public int Id { get; set; } //pk
        public int TaskId { get; set; } //fk
        public int TaskReviewId { get; set; } //fk
        public string RevisionNotes { get; set; } = string.Empty;
        public DateTime NewDeadline { get; set; }
        public RevisionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Task Task { get; set; } = null!;
        public TaskReview TaskReview { get; set; } = null!;
    }
}
