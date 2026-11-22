using alealamia.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResultDto> RegisterClientAsync(RegisterClientRequestDto dto);
        Task<AuthResultDto> RegisterFreelancerAsync(RegisterFreelancerRequestDto dto);
        Task<AuthResultDto> LoginAsync(LoginRequestDto dto);
    }
}
