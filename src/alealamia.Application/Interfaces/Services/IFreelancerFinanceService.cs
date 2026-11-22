using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Services
{
    public interface IFreelancerFinanceService
    {
        Task<FreelancerLedgerEntry> RecordEarningAsync(int taskId, string? createdByUserId);
        Task<FreelancerLedgerEntry> ApplyPlatformDeductionAsync(int taskId, string reason, string? createdByUserId);
        Task<IEnumerable<FreelancerLedgerEntry>> GetLedgerByFreelancerProfileIdAsync(int freelancerProfileId);
        Task<IEnumerable<FreelancerLedgerEntry>> GetLedgerByTaskIdAsync(int taskId);
    }
}