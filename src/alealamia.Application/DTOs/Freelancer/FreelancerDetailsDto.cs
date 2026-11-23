namespace alealamia.Application.DTOs.Freelancer
{
    public class FreelancerDetailsDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public int TrackId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public List<FreelancerSkillDto> Skills { get; set; } = new();
    }
}