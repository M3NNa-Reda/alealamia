using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IProjectRepository : IGenericRepository<Project>
    {
        Task<Project?> GetDetailsByIdAsync(int id);
        Task<IEnumerable<Project>> GetByClientProfileIdAsync(int clientProfileId);
        Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status);
    }
}