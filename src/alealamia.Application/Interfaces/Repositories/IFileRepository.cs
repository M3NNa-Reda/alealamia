
namespace alealamia.Application.Interfaces.Repositories
{
    public interface IFileRepository : IGenericRepository<Domain.Entities.File>
    {
        Task<IEnumerable<Domain.Entities.File>> GetByContentTypeAsync(string contentType);
        Task<IEnumerable<Domain.Entities.File>> GetByUploadedByUserIdAsync(string userId);
        Task<IEnumerable<Domain.Entities.File>> GetByUploadedAtRangeAsync(DateTime fromDate, DateTime toDate);

    }
}
