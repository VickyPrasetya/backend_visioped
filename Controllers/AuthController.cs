using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Daftar akun baru</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var (response, error) = await authService.RegisterAsync(req);
        if (error is not null) return BadRequest(new ApiError(false, error));
        return Ok(new ApiResponse<AuthResponse>(true, "Registrasi berhasil.", response));
    }

    /// <summary>Login dengan email & password</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var (response, error) = await authService.LoginAsync(req);
        if (error is not null) return Unauthorized(new ApiError(false, error));
        return Ok(new ApiResponse<AuthResponse>(true, "Login berhasil.", response));
    }

    /// <summary>Refresh access token menggunakan refresh token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        var (response, error) = await authService.RefreshAsync(req.RefreshToken);
        if (error is not null) return Unauthorized(new ApiError(false, error));
        return Ok(new ApiResponse<AuthResponse>(true, "Token diperbarui.", response));
    }

    /// <summary>Logout – hapus semua refresh token user</summary>
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = AuthService.GetUserId(User);
        await authService.LogoutAsync(userId);
        return Ok(new ApiError(true, "Logout berhasil."));
    }
}
