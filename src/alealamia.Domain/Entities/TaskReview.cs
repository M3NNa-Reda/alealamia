using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskReview
    {
        public int Id { get; set; } //pk
        public int TaskSubmissionId { get; set; } //fk
        public string ReviewedByUserId { get; set; } = string.Empty; //fk
        public ReviewDecision Decision { get; set; } 
        public string? Feedback { get; set; } 
        public DateTime ReviewedAt { get; set; }

        public TaskSubmission TaskSubmission { get; set; } = null!;
        //       not founded
        //public ApplicationUser ReviewedBy { get; set; } = null!;

        public ICollection<TaskRevision> Revisions { get; set; } = new List<TaskRevision>();
    }
}
