
using BusBookingApi.DTOs;

namespace BusBookingApi.Interfaces;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<AuthResponseDto?> RefreshAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
}
