using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface ISupportMessageRepository : IGenericRepository<SupportMessage>
    {
        Task<IEnumerable<SupportMessage>> GetByTicketIdAsync(int supportTicketId);
    }
}