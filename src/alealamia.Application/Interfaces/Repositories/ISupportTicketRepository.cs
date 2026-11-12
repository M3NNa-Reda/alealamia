using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface ISupportTicketRepository : IGenericRepository<SupportTicket>
    {
        Task<IEnumerable<SupportTicket>> GetByUserIdAsync(string userId);
        Task<IEnumerable<SupportTicket>> GetByStatusAsync(SupportTicketStatus status);
        Task<SupportTicket?> GetWithMessagesAsync(int id);
    }
}