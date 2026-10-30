using alealamia.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class FreelancerSkill
    {
        public int FreelancerProfileId { get; set; } //pk,fk
        public int SkillId { get; set; } //pk,fk
        public ProficiencyLevel ProficiencyLevel { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public FreelancerProfile FreelancerProfile { get; set; } = null!;
        public Skill Skill { get; set; } = null!;

    }
}
