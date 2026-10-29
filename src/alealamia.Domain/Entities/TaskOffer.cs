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
        //       type not founded
        //public TaskOfferStatus Status { get; set; } 
        public DateTime SentAt { get; set; } 
        public DateTime? RespondedAt { get; set; } 
    }
}
