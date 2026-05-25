namespace Backend.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? DiabetesType { get; set; } // Type1, Type2, PreDiabetes, None
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<GlucoseReading> GlucoseReadings { get; set; } = [];
    public ICollection<DailyLog> DailyLogs { get; set; } = [];
    public ICollection<ScanResult> ScanResults { get; set; } = [];
    public ICollection<Schedule> Schedules { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
