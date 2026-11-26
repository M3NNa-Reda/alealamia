using alealamia.Application.DTOs.Auth;
using alealamia.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResultDto> RegisterAsync(RegisterRequestDto dto);
        Task<AuthResultDto> LoginAsync(LoginRequestDto dto);
        Task<AuthResultDto> RefreshAsync(RefreshTokenRequestDto dto);
        Task<ResultDto> LogoutAsync(RefreshTokenRequestDto dto);
    }
}
