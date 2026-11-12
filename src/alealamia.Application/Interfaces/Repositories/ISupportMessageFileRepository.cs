using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface ISupportMessageFileRepository : IGenericRepository<SupportMessageFile>
    {
        Task<IEnumerable<SupportMessageFile>> GetByMessageIdAsync(int supportMessageId);
    }
}