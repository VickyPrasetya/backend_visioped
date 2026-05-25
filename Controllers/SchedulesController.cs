using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize]
public class SchedulesController(ScheduleService scheduleService) : ControllerBase
{
    /// <summary>Ambil semua jadwal atau filter by date (?date=2025-05-25)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateOnly? date)
    {
        var userId = AuthService.GetUserId(User);

        if (date.HasValue)
        {
            var byDate = await scheduleService.GetByDateAsync(userId, date.Value);
            return Ok(new ApiResponse<List<ScheduleDto>>(true, "Berhasil.", byDate));
        }

        var all = await scheduleService.GetAllAsync(userId);
        return Ok(new ApiResponse<List<ScheduleDto>>(true, "Berhasil.", all));
    }

    /// <summary>Buat jadwal baru</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest req)
    {
        var userId = AuthService.GetUserId(User);
        var data = await scheduleService.CreateAsync(userId, req);
        return Ok(new ApiResponse<ScheduleDto>(true, "Jadwal berhasil dibuat.", data));
    }

    /// <summary>Update jadwal (termasuk mark as complete)</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateScheduleRequest req)
    {
        var userId = AuthService.GetUserId(User);
        var data = await scheduleService.UpdateAsync(userId, id, req);
        if (data is null) return NotFound(new ApiError(false, "Jadwal tidak ditemukan."));
        return Ok(new ApiResponse<ScheduleDto>(true, "Jadwal berhasil diperbarui.", data));
    }

    /// <summary>Hapus jadwal</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = AuthService.GetUserId(User);
        var deleted = await scheduleService.DeleteAsync(userId, id);
        if (!deleted) return NotFound(new ApiError(false, "Jadwal tidak ditemukan."));
        return Ok(new ApiError(true, "Jadwal berhasil dihapus."));
    }
}
