namespace alealamia.Application.DTOs.Freelancer
{
    public class ChangeFreelancerTrackDto
    {
        public int TrackId { get; set; }
        public List<int> SkillIds { get; set; } = new();
    }
}