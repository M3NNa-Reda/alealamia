
namespace alealamia.Application.DTOs.TaskReview
{
    public class TaskReviewDto
    {
        public int Id { get; set; } 
        public int TaskSubmissionId { get; set; } 
        public string ReviewedByUserId { get; set; } = string.Empty;
        public ReviewDecision Decision { get; set; }
        public string? Feedback { get; set; }
        public DateTime ReviewedAt { get; set; }
    }
}
