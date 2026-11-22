
namespace alealamia.Domain.Entities
{
    public class TaskReview
    {
        public int Id { get; set; } //pk
        public int TaskSubmissionId { get; set; } //fk
        public string ReviewedByUserId { get; set; } = string.Empty; //fk
        public ReviewDecision Decision { get; set; } 
        public string? Feedback { get; set; } 
        public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
        public TaskSubmission TaskSubmission { get; set; } = null!;
        public ICollection<TaskRevision> TaskRevisions { get; set; } = new List<TaskRevision>();
    }
}
