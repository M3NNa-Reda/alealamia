using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Auth
{
    public class TokenDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}
