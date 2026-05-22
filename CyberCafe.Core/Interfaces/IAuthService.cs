using CyberCafe.Core.DTOs.Account;
using CyberCafe.Core.DTOs.Auth;

namespace CyberCafe.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}
