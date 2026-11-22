using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface ITaskReviewRepository :IGenericRepository<TaskReview>
    {
        Task<IEnumerable<TaskReview>> GetByTaskSubmissionIdAsync(int id);
        Task<IEnumerable<TaskReview>> GetByReviewedByUserIdAsync(string id);
        Task<IEnumerable<TaskReview>> GetByDecisionAsync(ReviewDecision decision);
        Task<IEnumerable<TaskReview>> GetByDateRangeAsync(DateTime fromDate , DateTime toDate);

    }
}
