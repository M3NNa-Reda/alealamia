using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Track
{
    public class TrackDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
