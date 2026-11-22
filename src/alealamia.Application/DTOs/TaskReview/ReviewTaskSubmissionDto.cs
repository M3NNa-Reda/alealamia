namespace alealamia.Application.DTOs.TaskReview
{
    public class ReviewTaskSubmissionDto
    {
        public int TaskSubmissionId { get; set; }
        public ReviewDecision Decision { get; set; }
        public string? Feedback { get; set; }
    }
}
