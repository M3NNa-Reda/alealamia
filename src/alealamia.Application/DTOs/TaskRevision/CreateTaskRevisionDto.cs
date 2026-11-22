namespace alealamia.Application.DTOs.TaskRevision
{
    public class CreateTaskRevisionDto
    {
        public int TaskId { get; set; }
        public int TaskReviewId { get; set; } 
        public string RevisionNotes { get; set; } = string.Empty;
        public DateTime NewDeadline { get; set; }
    }
}

// CompleteTaskRevisionDto 
/*
 Removed as 
            -revisionId already in service method
            -(status and completedAt )automatic calculated by system
 */