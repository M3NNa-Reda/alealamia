using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IMeetingRepository : IGenericRepository<Meeting>
    {
        Task<IEnumerable<Meeting>> GetByProjectIdAsync(int projectId);
        Task<IEnumerable<Meeting>> GetByClientProfileIdAsync(int clientProfileId);
        Task<IEnumerable<Meeting>> GetByStatusAsync(MeetingStatus status);
    }
}