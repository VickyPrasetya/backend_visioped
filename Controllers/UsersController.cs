using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(UserService userService) : ControllerBase
{
    /// <summary>Ambil profil user yang sedang login</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = AuthService.GetUserId(User);
        var user = await userService.GetByIdAsync(userId);
        if (user is null) return NotFound(new ApiError(false, "User tidak ditemukan."));
        return Ok(new ApiResponse<UserDto>(true, "Berhasil.", user));
    }

    /// <summary>Update profil user</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest req)
    {
        var userId = AuthService.GetUserId(User);
        var user = await userService.UpdateAsync(userId, req);
        if (user is null) return NotFound(new ApiError(false, "User tidak ditemukan."));
        return Ok(new ApiResponse<UserDto>(true, "Profil berhasil diperbarui.", user));
    }
}
