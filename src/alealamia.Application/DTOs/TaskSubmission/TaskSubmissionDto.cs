
namespace alealamia.Application.DTOs.TaskSubmission
{
    public class TaskSubmissionDto
    {
        public int Id { get; set; } 
        public int TaskId { get; set; } 
        public DateTime SubmittedAt { get; set; }
        public SubmissionStatus Status { get; set; }

    }
}
