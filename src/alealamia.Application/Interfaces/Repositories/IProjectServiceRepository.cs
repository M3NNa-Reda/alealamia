using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IProjectServiceRepository : IGenericRepository<ProjectService>
    {
        Task<IEnumerable<ProjectService>> GetByProjectIdAsync(int projectId);
        Task<ProjectService?> GetByProjectAndServiceAsync(int projectId, int serviceId);
    }
}