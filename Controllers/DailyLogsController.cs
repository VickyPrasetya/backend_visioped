using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/dailylogs")]
[Authorize]
public class DailyLogsController(DailyLogService dailyLogService) : ControllerBase
{
    /// <summary>Ambil log harian berdasarkan tanggal</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? date)
    {
        var userId = AuthService.GetUserId(User);
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var data = await dailyLogService.GetByDateAsync(userId, targetDate);
        return Ok(new ApiResponse<DailyLogDto?>(true, "Berhasil.", data));
    }

    /// <summary>Simpan atau update log harian</summary>
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] CreateDailyLogRequest req)
    {
        var userId = AuthService.GetUserId(User);
        var data = await dailyLogService.UpsertAsync(userId, req);
        return Ok(new ApiResponse<DailyLogDto>(true, "Log harian berhasil disimpan.", data));
    }
}
