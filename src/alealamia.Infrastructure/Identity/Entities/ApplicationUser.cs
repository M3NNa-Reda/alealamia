using alealamia.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Infrastructure.Identity.Entities
{
    public class ApplicationUser: IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ClientProfile? ClientProfile { get; set; }
        public FreelancerProfile? FreelancerProfile { get; set; }
        public ICollection<FreelancerBlock> FreelancerBlocks { get; set; } = new List<FreelancerBlock>();
        public ICollection<TaskReview> TaskReviews { get; set; } = new List<TaskReview>();
        public ICollection<FreelancerLedgerEntry> FreelancerLedgerEntries { get; set; } = new List<FreelancerLedgerEntry>();
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
        public ICollection<TaskReassignment> TaskReassignments { get; set; } = new List<TaskReassignment>();
        public ICollection<SupportMessage> SupportMessages { get; set; } = new List<SupportMessage>();
        public ICollection<Domain.Entities.Task> Tasks { get; set; } = new List<Domain.Entities.Task>();
    }
}
