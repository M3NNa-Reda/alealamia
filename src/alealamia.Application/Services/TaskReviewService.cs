using alealamia.Application.DTOs.TaskReview;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;


namespace alealamia.Application.Services
{
    public class TaskReviewService : ITaskReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        public TaskReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<TaskReviewDto> CreateReviewAsync(ReviewTaskSubmissionDto review)
        {
            TaskReview taskReview = new TaskReview()
            {
                TaskSubmissionId = review.TaskSubmissionId,
                Decision = review.Decision,
                Feedback = review.Feedback,
                ReviewedAt = DateTime.UtcNow
            };
            await _unitOfWork.TaskReviewRepository.AddAsync(taskReview);
            await _unitOfWork.CompleteAsync();

            TaskReviewDto taskReviewDto = new TaskReviewDto()
            {
                Id = taskReview.Id,
                TaskSubmissionId = taskReview.TaskSubmissionId,
                Decision = taskReview.Decision,
                Feedback = taskReview.Feedback,
                ReviewedAt = taskReview.ReviewedAt
            };
            return taskReviewDto;
        }

        public async Task<TaskReviewDto?> GetByIdAsync(int id)
        {
            TaskReview? taskReview = await _unitOfWork.TaskReviewRepository
                .GetByIdAsync(id);

            if (taskReview == null) return null;

            var result = new TaskReviewDto
            {
                Id = taskReview.Id,
                TaskSubmissionId = taskReview.TaskSubmissionId,
                ReviewedByUserId = taskReview.ReviewedByUserId,
                Decision = taskReview.Decision,
                Feedback = taskReview.Feedback,
                ReviewedAt = taskReview.ReviewedAt
            };

            return result;
        }

        public async Task<IEnumerable<TaskReviewDto>> GetBySubmissionIdAsync(int submissionId)
        {
            IEnumerable<TaskReview> taskReview = await _unitOfWork.TaskReviewRepository
                .GetByTaskSubmissionIdAsync(submissionId);

            var result = taskReview.Select(t => new TaskReviewDto
            {
                Id = t.Id,
                TaskSubmissionId = t.TaskSubmissionId,
                ReviewedByUserId = t.ReviewedByUserId,
                Decision = t.Decision,
                Feedback = t.Feedback,
                ReviewedAt = t.ReviewedAt
            });
            return result;
        }
    }
}
