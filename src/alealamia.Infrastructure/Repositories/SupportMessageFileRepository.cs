using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class SupportMessageFileRepository : GenericRepository<SupportMessageFile>, ISupportMessageFileRepository
    {
        public SupportMessageFileRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<SupportMessageFile>> GetByMessageIdAsync(int supportMessageId)
        {
            return await _context.SupportMessageFiles.AsNoTracking()
                .Include(f => f.File)
                .Where(f => f.SupportMessageId == supportMessageId)
                .ToListAsync();
        }
    }
}