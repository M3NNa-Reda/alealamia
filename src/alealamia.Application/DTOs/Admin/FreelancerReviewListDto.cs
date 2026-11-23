namespace alealamia.Application.DTOs.Admin
{
    public class FreelancerReviewListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}