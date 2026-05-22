using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using CyberCafe.Core.DTOs.Account;
using CyberCafe.Core.DTOs.Auth;
using CyberCafe.Core.Entities;
using CyberCafe.Core.Enums;
using CyberCafe.Core.Interfaces;
using CyberCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CyberCafe.Services;

public class AuthService : IAuthService
{
    private readonly CyberCafeDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(CyberCafeDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── Register ─────────────────────────────────────────────────────────────
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Reject duplicate usernames
        bool exists = await _db.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
            return new AuthResponse(false, null, "Username already taken.");

        // Parse role safely — default to Customer if unrecognised
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            role = UserRole.Customer;

        var user = new User
        {
            Username     = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Automatically create an empty wallet for the new user
        _db.Wallets.Add(new Wallet { UserId = user.Id, Balance = 0m });
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user);
        return new AuthResponse(true, token, "Registration successful.");
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new AuthResponse(false, null, "Invalid username or password.");

        var token = GenerateJwt(user);
        return new AuthResponse(true, token, "Login successful.");
    }

    // ── Change Password ───────────────────────────────────────────────────────
    public async Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return new AuthResponse(false, null, "User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return new AuthResponse(false, null, "Current password is incorrect.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return new AuthResponse(false, null, "New password must be at least 6 characters.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();

        return new AuthResponse(true, null, "Password changed successfully.");
    }

    // ── JWT Generation ────────────────────────────────────────────────────────
    private string GenerateJwt(User user)
    {
        var jwtKey    = _config["Jwt:Key"]    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var jwtIssuer = _config["Jwt:Issuer"] ?? "CyberCafeAPI";
        var expires   = int.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:   jwtIssuer,
            audience: jwtIssuer,
            claims:   claims,
            expires:  DateTime.UtcNow.AddMinutes(expires),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
