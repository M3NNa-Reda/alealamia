namespace alealamia.Application.DTOs.TaskRevision
{
    public class TaskRevisionDto
    {
        public int Id { get; set; } 
        public int TaskId { get; set; } 
        public int TaskReviewId { get; set; } 
        public string RevisionNotes { get; set; } = string.Empty;
        public DateTime NewDeadline { get; set; }
        public RevisionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
