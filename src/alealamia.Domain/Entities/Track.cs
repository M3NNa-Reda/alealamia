using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class Track
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<FreelancerProfile> FreelancerProfiles { get; set; } = new HashSet<FreelancerProfile>();
        public ICollection<TrackSkill> TrackSkills { get; set; } = new List<TrackSkill>();
    }
}
