namespace alealamia.Application.DTOs.FinanceLedger
{
    public class FreelancerFinanceSummaryDto
    {
        public int FreelancerProfileId { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetBalance { get; set; }
    }
}
