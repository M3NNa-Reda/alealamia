using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Freelancer
{
    public class UpdateFreelancerProfileDto 
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public List<int> SkillIds { get; set; } = new();
    }
}
