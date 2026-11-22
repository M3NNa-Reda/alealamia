using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface ITaskRevisionRepository :IGenericRepository<TaskRevision>
    {
        Task<IEnumerable<TaskRevision>> GetByTaskIdAsync(int id);
        Task<IEnumerable<TaskRevision>> GetByTaskReviewIdAsync(int id);
        Task<IEnumerable<TaskRevision>> GetByStatusAsync(RevisionStatus status);
        Task<IEnumerable<TaskRevision>> GetByCreatedAtRangeAsync(DateTime fromDate, DateTime toDate);

    }
}
