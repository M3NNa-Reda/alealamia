using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskReassignment
    {
        public int Id { get; set; } //pk
        public int TaskId { get; set; } //fk
        public int? PreviousFreelancerId { get; set; } //fk
        public int? NewFreelancerId { get; set; } //fk
        public string Reason { get; set; } = string.Empty;
        public string ReassignedByUserId { get; set; } = string.Empty; //fk
        public DateTime ReassignedAt { get; set; } 
    }
}
