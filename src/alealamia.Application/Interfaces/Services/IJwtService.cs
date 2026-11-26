using alealamia.Application.DTOs.Auth;
using alealamia.Application.DTOs.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.Interfaces.Services
{
    public interface IJwtService
    {
        Task<GeneratedTokensDto> GenerateTokensAsync(ApplicationUserDto user, IList<string> roles);
    }
}
