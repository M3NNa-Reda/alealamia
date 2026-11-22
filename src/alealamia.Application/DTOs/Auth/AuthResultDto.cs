using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Auth
{
    public class AuthResultDto
    {
        public bool Succeeded { get; set; }
        public AuthResponseDto? Data { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
    }
}
