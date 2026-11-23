using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Track
{
    public class UpdateTrackDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
