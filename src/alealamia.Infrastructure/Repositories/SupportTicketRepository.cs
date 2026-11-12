using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class SupportTicketRepository : GenericRepository<SupportTicket>, ISupportTicketRepository
    {
        public SupportTicketRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<SupportTicket>> GetByUserIdAsync(string userId)
        {
            return await _context.SupportTickets.AsNoTracking()
                .Where(t => t.CreatedByUserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SupportTicket>> GetByStatusAsync(SupportTicketStatus status)
        {
            return await _context.SupportTickets.AsNoTracking()
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<SupportTicket?> GetWithMessagesAsync(int id)
        {
            return await _context.SupportTickets
                .Include(t => t.SupportMessages)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}