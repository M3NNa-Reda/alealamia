using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class FreelancerBlockRepository : GenericRepository<FreelancerBlock>, IFreelancerBlockRepository
    {
        public FreelancerBlockRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<FreelancerBlock>> GetActiveBlocksByFreelancerIdAsync(int freelancerProfileId, DateTime currentDateTime)
        {
            return await _context.FreelancerBlocks
                .Where(x => x.FreelancerProfileId == freelancerProfileId && 
                x.StartedAt <= currentDateTime && 
                x.EndsAt > currentDateTime)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<FreelancerBlock>> GetAllActiveBlocksAsync(DateTime currentDateTime)
        {
            return await _context.FreelancerBlocks
               .Where(x => x.StartedAt <= currentDateTime &&
               x.EndsAt > currentDateTime)
               .AsNoTracking()
               .ToListAsync();
        }

        public async Task<IEnumerable<FreelancerBlock>> GetBlockHistoryByFreelancerIdAsync(int freelancerProfileId)
        {
            return await _context.FreelancerBlocks
               .Where(x => x.FreelancerProfileId == freelancerProfileId)
               .OrderByDescending(x => x.StartedAt)
               .AsNoTracking()
               .ToListAsync();
        }
    }
}
