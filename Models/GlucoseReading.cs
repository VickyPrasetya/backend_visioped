namespace Backend.Models;

public class GlucoseReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal GlucoseLevel { get; set; } // mg/dL
    public string MeasurementType { get; set; } = string.Empty; // FastingBlood, PostMeal, Random, BeforeSleep
    public string? Notes { get; set; }
    public DateTime MeasuredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
