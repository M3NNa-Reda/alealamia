using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Skill
{
    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
