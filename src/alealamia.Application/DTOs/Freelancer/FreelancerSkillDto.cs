using alealamia.Domain.Enums;

namespace alealamia.Application.DTOs.Freelancer
{
    public class FreelancerSkillDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public ProficiencyLevel ProficiencyLevel { get; set; }
    }
}