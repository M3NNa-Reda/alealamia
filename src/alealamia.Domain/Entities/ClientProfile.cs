using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class ClientProfile
    {
        public int Id { get; set; }
        //Unique
        public string UserId { get; set; } = string.Empty; //FK
        public string? BusinessName { get; set; } 
        public string? BusinessType { get; set; } 
        public string? WebsiteUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
