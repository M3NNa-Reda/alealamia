using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IFreelancerLedgerEntryRepository:IGenericRepository<FreelancerLedgerEntry>
    {
        Task<IEnumerable<FreelancerLedgerEntry>> GetByFreelancerProfileIdAsync(int id);
        Task<IEnumerable<FreelancerLedgerEntry>> GetByTaskIdAsync(int id);
        Task<IEnumerable<FreelancerLedgerEntry>> GetByEntryTypeAsync(LedgerEntryType entryType);
        Task<IEnumerable<FreelancerLedgerEntry>> GetByCreatedAtRangeAsync(DateTime fromDate, DateTime toDate);
        Task<IEnumerable<FreelancerLedgerEntry>> GetByCreatedByUserIdAsync(string id);
    }
}
