using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Auth
{
    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
