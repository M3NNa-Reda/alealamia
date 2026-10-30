using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public ICollection<TrackSkill> TrackSkills { get; set; } = new List<TrackSkill>();
        public ICollection<FreelancerSkill> FreelancerSkills { get; set; } = new List<FreelancerSkill>();

    }
}
