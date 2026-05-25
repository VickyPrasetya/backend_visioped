namespace Backend.Models;

public class DailyLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public string? FoodNotes { get; set; }
    public int? ExerciseMinutes { get; set; }
    public string? ExerciseType { get; set; }
    public int? WaterIntakeMl { get; set; }
    public decimal? Weight { get; set; } // kg
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? MoodScore { get; set; } // 1-5
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
