using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class SupportMessageRepository : GenericRepository<SupportMessage>, ISupportMessageRepository
    {
        public SupportMessageRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<SupportMessage>> GetByTicketIdAsync(int supportTicketId)
        {
            return await _context.SupportMessages.AsNoTracking()
                .Include(m => m.SupportMessageFiles)
                .Where(m => m.SupportTicketId == supportTicketId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }
}