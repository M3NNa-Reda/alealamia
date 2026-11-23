using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Track
{
    public class CreateTrackDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
