namespace alealamia.Application.DTOs.FinanceLedger
{
    public class FreelancerDeductionDto
    {
        public int FreelancerProfileId { get; set; }
        public int? TaskId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;

    }
}
