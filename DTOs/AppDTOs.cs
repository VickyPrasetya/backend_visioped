namespace Backend.DTOs;

// ── Auth ─────────────────────────────────────────────────────────────────────

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string? PhoneNumber,
    string? DiabetesType
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User
);

public record RefreshTokenRequest(string RefreshToken);

// ── User ──────────────────────────────────────────────────────────────────────

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    string? Gender,
    string? DiabetesType,
    string? ProfilePictureUrl,
    DateTime? DateOfBirth
);

public record UpdateUserRequest(
    string? Name,
    string? PhoneNumber,
    string? Gender,
    string? DiabetesType,
    DateTime? DateOfBirth
);

// ── Glucose ───────────────────────────────────────────────────────────────────

public record CreateGlucoseRequest(
    decimal GlucoseLevel,
    string MeasurementType,
    DateTime MeasuredAt,
    string? Notes
);

public record GlucoseDto(
    Guid Id,
    decimal GlucoseLevel,
    string MeasurementType,
    string? Notes,
    DateTime MeasuredAt
);

public record GlucoseTrendPoint(
    string Day,
    decimal? Value,
    DateTime Date
);

// ── Daily Log ─────────────────────────────────────────────────────────────────

public record CreateDailyLogRequest(
    DateOnly LogDate,
    string? FoodNotes,
    int? ExerciseMinutes,
    string? ExerciseType,
    int? WaterIntakeMl,
    decimal? Weight,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? MoodScore
);

public record DailyLogDto(
    Guid Id,
    DateOnly LogDate,
    string? FoodNotes,
    int? ExerciseMinutes,
    string? ExerciseType,
    int? WaterIntakeMl,
    decimal? Weight,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? MoodScore
);

// ── Schedule ──────────────────────────────────────────────────────────────────

public record CreateScheduleRequest(
    string Title,
    string? Description,
    string ScheduleType,
    DateOnly ScheduledDate,
    TimeOnly? ScheduledTime
);

public record UpdateScheduleRequest(
    string? Title,
    string? Description,
    bool? IsCompleted,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime
);

public record ScheduleDto(
    Guid Id,
    string Title,
    string? Description,
    string ScheduleType,
    DateOnly ScheduledDate,
    TimeOnly? ScheduledTime,
    bool IsCompleted
);

// ── Scan ──────────────────────────────────────────────────────────────────────

public record ScanResultDto(
    Guid Id,
    string ImageUrl,
    string DiagnosisResult,
    decimal? ConfidenceScore,
    string? Notes,
    DateTime ScannedAt
);

// ── Generic ───────────────────────────────────────────────────────────────────

public record ApiResponse<T>(bool Success, string Message, T? Data);
public record ApiError(bool Success, string Message);
