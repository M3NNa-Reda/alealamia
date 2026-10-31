using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskSubmission
    {
        public int Id { get; set; } //pk
        public int TaskId { get; set; } //fk
        public int FreelancerProfileId { get; set; } //fk
        public string? SubmissionNotes { get; set; } 
        public DateTime SubmittedAt { get; set; }
        public SubmissionStatus Status { get; set; }

        public Task Task { get; set; } = null!;
        public FreelancerProfile FreelancerProfile { get; set; } = null!;

        public ICollection<TaskReview> Reviews { get; set; } = new List<TaskReview>();
        public ICollection<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
    }
}
