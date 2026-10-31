using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class FreelancerLedgerEntry
    {
        public int Id { get; set; } //pk
        public int FreelancerProfileId { get; set; } //fk
        public int? TaskId { get; set; } //fk
        public LedgerEntryType EntryType { get; set; } 
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedByUserId { get; set; }
        public FreelancerProfile FreelancerProfile { get; set; } = null!;
        public Task? Task { get; set; }
       // public ApplicationUser? CreatedBy { get; set; }
    }
}
