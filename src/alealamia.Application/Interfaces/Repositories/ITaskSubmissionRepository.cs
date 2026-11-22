using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface ITaskSubmissionRepository : IGenericRepository<TaskSubmission>
    {
        Task<IEnumerable<TaskSubmission>> GetByTaskIdAsync(int id);
        Task<IEnumerable<TaskSubmission>> GetByFreelancerProfileIdAsync(int id);
        Task<IEnumerable<TaskSubmission>> GetByDateRangeAsync(DateTime fromDate , DateTime toDate);
        Task<IEnumerable<TaskSubmission>> GetByStatusAsync(SubmissionStatus status);
    }
}
