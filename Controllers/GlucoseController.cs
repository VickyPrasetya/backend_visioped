using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/glucose")]
[Authorize]
public class GlucoseController(GlucoseService glucoseService) : ControllerBase
{
    /// <summary>Ambil semua data glukosa milik user</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = AuthService.GetUserId(User);
        var data = await glucoseService.GetAllAsync(userId);
        return Ok(new ApiResponse<List<GlucoseDto>>(true, "Berhasil.", data));
    }

    /// <summary>Ambil data trend glukosa untuk chart (default 7 hari)</summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int days = 7)
    {
        if (days < 1 || days > 30) days = 7;
        var userId = AuthService.GetUserId(User);
        var data = await glucoseService.GetTrendsAsync(userId, days);
        return Ok(new ApiResponse<List<GlucoseTrendPoint>>(true, "Berhasil.", data));
    }

    /// <summary>Ambil detail satu pembacaan glukosa</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = AuthService.GetUserId(User);
        var data = await glucoseService.GetByIdAsync(userId, id);
        if (data is null) return NotFound(new ApiError(false, "Data tidak ditemukan."));
        return Ok(new ApiResponse<GlucoseDto>(true, "Berhasil.", data));
    }

    /// <summary>Tambah pembacaan glukosa baru</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGlucoseRequest req)
    {
        var userId = AuthService.GetUserId(User);
        var data = await glucoseService.CreateAsync(userId, req);
        return CreatedAtAction(nameof(GetById), new { id = data.Id },
            new ApiResponse<GlucoseDto>(true, "Data glukosa berhasil disimpan.", data));
    }

    /// <summary>Hapus pembacaan glukosa</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = AuthService.GetUserId(User);
        var deleted = await glucoseService.DeleteAsync(userId, id);
        if (!deleted) return NotFound(new ApiError(false, "Data tidak ditemukan."));
        return Ok(new ApiError(true, "Data berhasil dihapus."));
    }
}
