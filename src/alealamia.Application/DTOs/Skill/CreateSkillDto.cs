using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Skill
{
    public class CreateSkillDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
