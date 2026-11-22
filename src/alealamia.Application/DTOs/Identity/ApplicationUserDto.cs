using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Identity
{
    public class ApplicationUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
