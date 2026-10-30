using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TrackSkill
    {
        public int TrackId { get; set; } //pk,fk
        public int SkillId { get; set; } //pk,fk
        public Track Track { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}
