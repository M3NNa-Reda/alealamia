using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Freelancer
{
    public class CompleteFreelancerProfileDto
    {
        public int TrackId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public List<int> SkillIds { get; set; } = new();
    }
}
