using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class PortfolioItem
    {
        public int Id { get; set; }
        public int FreelancerProfileId { get; set; } //fk
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ProjectUrl { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public FreelancerProfile FreelancerProfile { get; set; } = null!;
        public ICollection<PortfolioItemFile> PortfolioItemFiles { get; set; } = new List<PortfolioItemFile>();


    }
}
