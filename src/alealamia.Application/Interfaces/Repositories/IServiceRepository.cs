using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IServiceRepository : IGenericRepository<Service>
    {
        Task<IEnumerable<Service>> GetActiveServicesAsync();
        Task<bool> ExistsByNameAsync(string name);
    }
}