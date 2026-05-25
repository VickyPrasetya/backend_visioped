namespace Backend.Models;

public class ScanResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string DiagnosisResult { get; set; } = string.Empty; // Normal, Mild, Moderate, Severe
    public decimal? ConfidenceScore { get; set; } // 0.0 - 1.0
    public string? Notes { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
