using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IProjectStatusHistoryRepository : IGenericRepository<ProjectStatusHistory>
    {
        Task<IEnumerable<ProjectStatusHistory>> GetByProjectIdAsync(int projectId);
    }
}