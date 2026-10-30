using alealamia.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class FreelancerProfile
    {
        public int Id { get; set; }
        //Unique
        public string UserId { get; set; } = string.Empty; //FK
        public int TrackId { get; set; }  //FK
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public FreelancerApprovalStatus ApprovalStatus { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Track Track { get; set; } = null!;
        public ICollection<FreelancerBlock> FreelancerBlocks { get; set; } = new List<FreelancerBlock>();
        public ICollection<FreelancerSkill> FreelancerSkills { get; set; } = new List<FreelancerSkill>();
        public ICollection<PortfolioItem> PortfolioItems { get; set; } = new List<PortfolioItem>();


    }
}
