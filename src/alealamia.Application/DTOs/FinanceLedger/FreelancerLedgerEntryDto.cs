namespace alealamia.Application.DTOs.FinanceLedger
{
    public class FreelancerLedgerEntryDto
    {
        public int Id { get; set; } 
        public int FreelancerProfileId { get; set; } 
        public int? TaskId { get; set; } 
        public LedgerEntryType EntryType { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedByUserId { get; set; }
    }
}
