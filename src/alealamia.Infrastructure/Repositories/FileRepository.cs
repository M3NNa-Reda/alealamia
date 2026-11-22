using alealamia.Application.Interfaces.Repositories;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class FileRepository : GenericRepository<Domain.Entities.File>, IFileRepository
    {
        public FileRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Domain.Entities.File>> GetByUploadedByUserIdAsync(string userId)
        {
            return await _context.Files
                    .AsNoTracking()
                    .Where(f => f.UploadedByUserId == userId)
                    .ToListAsync();
        }
        public async Task<IEnumerable<Domain.Entities.File>> GetByContentTypeAsync(string contentType)
        {
            return await _context.Files
                    .AsNoTracking()
                    .Where(f => f.ContentType == contentType)
                    .ToListAsync();
        }

        public async Task<IEnumerable<Domain.Entities.File>> GetByUploadedAtRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Files
                    .AsNoTracking()
                    .Where(t => t.UploadedAt >= fromDate && t.UploadedAt <= toDate)
                    .ToListAsync();
        }

    }
}
