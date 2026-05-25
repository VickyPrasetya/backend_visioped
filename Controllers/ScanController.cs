using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/scan")]
[Authorize]
public class ScanController(AppDbContext db) : ControllerBase
{
    /// <summary>Riwayat hasil scan</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = AuthService.GetUserId(User);
        var results = await db.ScanResults
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ScannedAt)
            .Select(s => new ScanResultDto(s.Id, s.ImageUrl, s.DiagnosisResult,
                                           s.ConfidenceScore, s.Notes, s.ScannedAt))
            .ToListAsync();
        return Ok(new ApiResponse<List<ScanResultDto>>(true, "Berhasil.", results));
    }

    /// <summary>
    /// Upload gambar scan (placeholder – model AI belum di-integrate).
    /// Saat ini mengembalikan hasil mock.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Scan(IFormFile? image)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new ApiError(false, "Gambar tidak ditemukan."));

        var userId = AuthService.GetUserId(User);

        // Simpan file ke wwwroot/uploads (placeholder)
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
            await image.CopyToAsync(stream);

        // TODO: Ganti dengan panggilan model AI
        var mockResults = new[] { "Normal", "Mild", "Moderate", "Severe" };
        var mockResult = mockResults[new Random().Next(mockResults.Length)];
        var mockScore = (decimal)Math.Round(new Random().NextDouble() * 0.3 + 0.7, 4);

        var scanResult = new ScanResult
        {
            UserId = userId,
            ImageUrl = $"/uploads/{fileName}",
            DiagnosisResult = mockResult,
            ConfidenceScore = mockScore,
            Notes = "Hasil analisis otomatis (placeholder)."
        };

        db.ScanResults.Add(scanResult);
        await db.SaveChangesAsync();

        var dto = new ScanResultDto(scanResult.Id, scanResult.ImageUrl,
            scanResult.DiagnosisResult, scanResult.ConfidenceScore,
            scanResult.Notes, scanResult.ScannedAt);

        return Ok(new ApiResponse<ScanResultDto>(true, "Scan berhasil.", dto));
    }
}
