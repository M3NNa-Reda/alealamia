using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskReview
    {
        public int Id { get; set; } //pk
        public int TaskSubmissionId { get; set; } //pk
        public string ReviewedByUserId { get; set; } = string.Empty; //pk
        //       type not founded
        //public ReviewDecision Decision { get; set; } 
        public string? Feedback { get; set; } 
        public DateTime ReviewedAt { get; set; } 
        public int MyProperty { get; set; }
    }
}
