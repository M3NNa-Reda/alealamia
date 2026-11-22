using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Identity
{
    public class IdentityResultDto
    {
        public bool Succeeded { get; set; }
        public IEnumerable<string> Errors { get; set; }= new List<string>();
        public string? UserId { get; set; }
    }
}
