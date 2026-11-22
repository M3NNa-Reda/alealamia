
namespace alealamia.Domain.Entities
{
    public class TaskSubmission
    {
        public int Id { get; set; } //pk
        public int TaskId { get; set; } //fk
        public int FreelancerProfileId { get; set; } //fk
        public string? SubmissionNotes { get; set; } 
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public SubmissionStatus Status { get; set; }

        public Task Task { get; set; } = null!;
        public FreelancerProfile FreelancerProfile { get; set; } = null!;

        public ICollection<TaskReview> TaskReviews { get; set; } = new List<TaskReview>();
        public ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();
    }
}
