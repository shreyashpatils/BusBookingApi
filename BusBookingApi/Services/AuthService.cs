
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BusBookingApi.Data;
using BusBookingApi.DTOs;
using BusBookingApi.Entities;
using BusBookingApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BusBookingApi.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<bool> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return false;

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user == null ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var result = CreateTokenPair(user, now);

        _db.RefreshTokens.Add(result.RefreshTokenEntity);
        await _db.SaveChangesAsync();

        return result.Response;
    }

    public async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var tokenHash = HashToken(refreshToken);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var storedToken = await _db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

        var now = DateTime.UtcNow;

        if (storedToken == null ||
            storedToken.RevokedAtUtc != null ||
            storedToken.ExpiresAtUtc <= now)
        {
            return null;
        }

        // Invalidate the old refresh token.
        storedToken.RevokedAtUtc = now;

        // Generate a new access token and refresh token.
        var result = CreateTokenPair(storedToken.User, now);

        _db.RefreshTokens.Add(result.RefreshTokenEntity);

        // Save the new token first to obtain its database ID.
        await _db.SaveChangesAsync();

        storedToken.ReplacedByTokenId = result.RefreshTokenEntity.Id;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return result.Response;
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var tokenHash = HashToken(refreshToken);

        var storedToken = await _db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken == null ||
            storedToken.RevokedAtUtc != null)
        {
            return false;
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    private (AuthResponseDto Response, RefreshToken RefreshTokenEntity)
        CreateTokenPair(User user, DateTime now)
    {
        var expiryMinutes = int.Parse(
            _config["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        var expiresAt = now.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(ClaimTypes.Email, user.Email),

            new Claim(ClaimTypes.Name, user.FullName)
        };

        var keyText = _config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException(
                "JWT secret is missing.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyText));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(jwt);

        var rawRefreshToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        var refreshDays = int.Parse(
            _config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(refreshDays)
        };

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            AccessTokenExpiresAtUtc = expiresAt
        };

        return (response, refreshTokenEntity);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
