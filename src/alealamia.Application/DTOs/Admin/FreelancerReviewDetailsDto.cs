namespace alealamia.Application.DTOs.Admin
{
    public class FreelancerReviewDetailsDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public List<FreelancerReviewSkillDto> Skills { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}