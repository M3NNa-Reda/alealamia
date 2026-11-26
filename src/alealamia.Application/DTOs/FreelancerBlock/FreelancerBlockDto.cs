namespace alealamia.Application.DTOs.FreelancerBlock
{
    public class FreelancerBlockDto
    {
        public int Id { get; set; }
        public int FreelancerProfileId { get; set; }
        public int? TaskId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime EndsAt { get; set; }
    }
}