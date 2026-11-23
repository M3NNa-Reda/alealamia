using alealamia.Application.DTOs.FinanceLedger;

namespace alealamia.Application.Interfaces.Services
{
    public interface IFreelancerFinanceService
    {
        Task<FreelancerLedgerEntryDto> RecordEarningAsync(int taskId, string? createdByUserId);

        Task<FreelancerLedgerEntryDto> ApplyPlatformDeductionAsync(int taskId, string reason, string? createdByUserId);

        Task<IEnumerable<FreelancerLedgerEntryDto>> GetLedgerByFreelancerProfileIdAsync(int freelancerProfileId);

        Task<IEnumerable<FreelancerLedgerEntryDto>> GetLedgerByTaskIdAsync(int taskId);

        Task<FreelancerFinanceSummaryDto> GetFinanceSummaryAsync(int freelancerProfileId);
    }
}