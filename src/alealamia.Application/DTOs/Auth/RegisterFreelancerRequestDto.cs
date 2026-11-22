using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.DTOs.Auth
{
    public class RegisterFreelancerRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public int TrackId { get; set; }
    }
}
