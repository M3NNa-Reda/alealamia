using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class PortfolioRepository
        : GenericRepository<PortfolioItem>, IPortfolioRepository
    {
        public PortfolioRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<PortfolioItem?> GetByIdWithFilesAsync(
            int portfolioItemId)
        {
            return await _context.PortfolioItems
                .Include(x => x.PortfolioItemFiles)
                    .ThenInclude(x => x.File)
                .FirstOrDefaultAsync(x => x.Id == portfolioItemId);
        }

        public async Task<IEnumerable<PortfolioItem>>
            GetByFreelancerIdWithFilesAsync(int freelancerProfileId)
        {
            return await _context.PortfolioItems
                .Where(x => x.FreelancerProfileId == freelancerProfileId)
                .Include(x => x.PortfolioItemFiles)
                    .ThenInclude(x => x.File)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AreFilesOwnedByUserAsync(
            IEnumerable<int> fileIds,
            string userId)
        {
            var ids = fileIds.Distinct().ToList();

            if (!ids.Any())
                return true;

            var count = await _context.Files
                .CountAsync(x =>
                    ids.Contains(x.Id) &&
                    x.UploadedByUserId == userId);

            return count == ids.Count;
        }
    }
}