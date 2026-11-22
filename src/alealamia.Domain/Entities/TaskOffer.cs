using alealamia.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskOffer
    {
        public int Id { get; set; } // pk
        public int TaskId { get; set; } // fk
        public int FreelancerProfileId { get; set; } // fk
        public TaskOfferStatus Status { get; set; } 
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public Task Task { get; set; } = null!;
        public FreelancerProfile FreelancerProfile { get; set; } = null!;
    }
}
