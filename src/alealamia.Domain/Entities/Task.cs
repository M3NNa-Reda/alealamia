using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class Task
    {
        public int Id { get; set; } // pk
        public int ProjectId { get; set; } //fk
        public int TrackId { get; set; } //fk
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskStatus Status { get; set; }
        public int? ActualDurationDays { get; set; }
        public DateTime PublishedDeadline { get; set; }
        public DateTime? ExtensionDeadline { get; set; }
        public int? AssignedFreelancerId { get; set; } //fk
        public string CreatedByUserId { get; set; } = string.Empty; //fk admin
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<FreelancerBlock> FreelancerBlocks { get; set; } = new HashSet<FreelancerBlock>();

    }
}
