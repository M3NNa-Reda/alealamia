using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class FreelancerBlock
    {
        public int Id { get; set; }
        public int FreelancerProfileId { get; set; } //FK
        public int? TaskId { get; set; } //FK
        public DateTime StartedAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;//FK Admin
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public FreelancerProfile FreelancerProfile { get; set; } = null!;
        public Task? Task { get; set; }




    }
}
