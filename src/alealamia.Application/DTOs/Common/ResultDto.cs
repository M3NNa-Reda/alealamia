using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Common
{
    public class ResultDto
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
