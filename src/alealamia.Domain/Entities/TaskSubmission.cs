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
        //       type not founded
        //public SubmissionStatus Status { get; set; }
    }
}
