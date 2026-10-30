using alealamia.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Infrastructure.Identity.Entities
{
    public class ApplicationUser: IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ClientProfile ClientProfile { get; set; } = null!;
        public FreelancerProfile FreelancerProfile { get; set; } = null!;
        public ICollection<FreelancerBlock> FreelancerBlocks { get; set; } = new List<FreelancerBlock>();
    }
}
