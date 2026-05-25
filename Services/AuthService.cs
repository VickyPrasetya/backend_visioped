using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    private readonly string _secret   = config["JwtSettings:SecretKey"]!;
    private readonly string _issuer   = config["JwtSettings:Issuer"]!;
    private readonly string _audience = config["JwtSettings:Audience"]!;
    private readonly int _accessExpiry  = int.Parse(config["JwtSettings:AccessTokenExpiryMinutes"]!);
    private readonly int _refreshExpiry = int.Parse(config["JwtSettings:RefreshTokenExpiryDays"]!);

    // ── Register ──────────────────────────────────────────────────────────────
    public async Task<(AuthResponse? response, string? error)> RegisterAsync(RegisterRequest req)
    {
        var newId        = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_Auth_Register";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@Id",           newId));
        cmd.Parameters.Add(new SqlParameter("@Name",         req.Name));
        cmd.Parameters.Add(new SqlParameter("@Email",        req.Email));
        cmd.Parameters.Add(new SqlParameter("@PasswordHash", passwordHash));
        cmd.Parameters.Add(new SqlParameter("@PhoneNumber",  (object?)req.PhoneNumber  ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@DiabetesType", (object?)req.DiabetesType ?? DBNull.Value));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var code = Convert.ToInt32(reader["ResultCode"]);
            if (code != 0) return (null, reader["Message"].ToString());
        }
        await reader.CloseAsync();

        // Ambil user yang baru dibuat
        var user = await GetUserByEmailAsync(req.Email);
        if (user is null) return (null, "Gagal membuat akun.");

        return (await GenerateAuthResponseAsync(user), null);
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    public async Task<(AuthResponse? response, string? error)> LoginAsync(LoginRequest req)
    {
        var user = await GetUserByEmailAsync(req.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return (null, "Email atau password salah.");

        return (await GenerateAuthResponseAsync(user), null);
    }

    // ── Refresh Token ─────────────────────────────────────────────────────────
    public async Task<(AuthResponse? response, string? error)> RefreshAsync(string refreshToken)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_Auth_GetRefreshToken";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@Token", refreshToken));

        User? user = null;
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            user = new User
            {
                Id           = (Guid)reader["UserId"],
                Name         = reader["Name"].ToString()!,
                Email        = reader["Email"].ToString()!,
                PasswordHash = reader["PasswordHash"].ToString()!,
            };
        }
        await reader.CloseAsync();

        if (user is null) return (null, "Refresh token tidak valid atau sudah kadaluarsa.");

        // Hapus token lama
        using var delCmd = conn.CreateCommand();
        delCmd.CommandText = "sp_Auth_DeleteRefreshToken";
        delCmd.CommandType = CommandType.StoredProcedure;
        delCmd.Parameters.Add(new SqlParameter("@Token", refreshToken));
        await delCmd.ExecuteNonQueryAsync();

        return (await GenerateAuthResponseAsync(user), null);
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    public async Task LogoutAsync(Guid userId)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_Auth_DeleteAllRefreshTokensByUser";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@UserId", userId));
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task<User?> GetUserByEmailAsync(string email)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_Auth_GetUserByEmail";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@Email", email));

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new User
        {
            Id           = (Guid)reader["Id"],
            Name         = reader["Name"].ToString()!,
            Email        = reader["Email"].ToString()!,
            PasswordHash = reader["PasswordHash"].ToString()!,
            PhoneNumber  = reader["PhoneNumber"] == DBNull.Value ? null : reader["PhoneNumber"].ToString(),
            Gender       = reader["Gender"]       == DBNull.Value ? null : reader["Gender"].ToString(),
            DiabetesType = reader["DiabetesType"] == DBNull.Value ? null : reader["DiabetesType"].ToString(),
        };
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var accessToken  = GenerateAccessToken(user);
        var refreshToken = await SaveRefreshTokenAsync(user.Id);

        var userDto = new UserDto(
            user.Id, user.Name, user.Email, user.PhoneNumber,
            user.Gender, user.DiabetesType, user.ProfilePictureUrl, user.DateOfBirth);

        return new AuthResponse(accessToken, refreshToken, userDto);
    }

    private string GenerateAccessToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.Name),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: _issuer, audience: _audience, claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessExpiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> SaveRefreshTokenAsync(Guid userId)
    {
        var tokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_Auth_SaveRefreshToken";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@Id",        Guid.NewGuid()));
        cmd.Parameters.Add(new SqlParameter("@UserId",    userId));
        cmd.Parameters.Add(new SqlParameter("@Token",     tokenString));
        cmd.Parameters.Add(new SqlParameter("@ExpiresAt", DateTime.UtcNow.AddDays(_refreshExpiry)));
        await cmd.ExecuteNonQueryAsync();

        return tokenString;
    }

    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }
}
