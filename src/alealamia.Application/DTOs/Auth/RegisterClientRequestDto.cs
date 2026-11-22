using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Auth
{
    public class RegisterClientRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? BusinessName { get; set; }
        public string? BusinessType { get; set; }
        public string? WebsiteUrl { get; set; }
    }
}
