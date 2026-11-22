using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class FreelancerLedgerEntryRepository : GenericRepository<FreelancerLedgerEntry>, IFreelancerLedgerEntryRepository
    {
        public FreelancerLedgerEntryRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<FreelancerLedgerEntry>> GetByTaskIdAsync(int id)
        {
            return await _context.FreelancerLedgerEntries
                     .AsNoTracking()
                     .Where(f => f.TaskId == id)
                     .ToListAsync();
        }
        public async Task<IEnumerable<FreelancerLedgerEntry>> GetByFreelancerProfileIdAsync(int id)
        {
            return await _context.FreelancerLedgerEntries
                     .AsNoTracking()
                     .Where(f => f.FreelancerProfileId == id)
                     .ToListAsync();
        }
        public async Task<IEnumerable<FreelancerLedgerEntry>> GetByCreatedByUserIdAsync(string id)
        {
            return await _context.FreelancerLedgerEntries
                     .AsNoTracking()
                     .Where(f => f.CreatedByUserId == id)
                     .ToListAsync();
        }
        public async Task<IEnumerable<FreelancerLedgerEntry>> GetByEntryTypeAsync(LedgerEntryType entryType)
        {
            return await _context.FreelancerLedgerEntries
                   .AsNoTracking()
                   .Where(f => f.EntryType == entryType)
                   .ToListAsync();
        }
        public async Task<IEnumerable<FreelancerLedgerEntry>> GetByCreatedAtRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.FreelancerLedgerEntries
                    .AsNoTracking()
                    .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
                    .ToListAsync();
        }
    }
}
