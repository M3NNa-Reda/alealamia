using alealamia.Application.DTOs.TaskReview;

namespace alealamia.Application.Interfaces.Services
{
    public interface ITaskReviewService
    {
        Task<TaskReviewDto> CreateReviewAsync(ReviewTaskSubmissionDto review);
        Task<TaskReviewDto?> GetByIdAsync(int id);
        Task<IEnumerable<TaskReviewDto>>  GetBySubmissionIdAsync(int submissionId);
    }
}
