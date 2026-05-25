using System.Data;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class DailyLogService(AppDbContext db)
{
    // ── Get by date ───────────────────────────────────────────────────────────
    public async Task<DailyLogDto?> GetByDateAsync(Guid userId, DateOnly date)
    {
        var log = await db.DailyLogs
            .FromSqlRaw("EXEC sp_DailyLog_GetByDate @UserId, @LogDate",
                new SqlParameter("@UserId",  userId),
                new SqlParameter("@LogDate", date.ToDateTime(TimeOnly.MinValue).Date))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return log is null ? null : MapToDto(log);
    }

    // ── Upsert (create or update) ─────────────────────────────────────────────
    public async Task<DailyLogDto> UpsertAsync(Guid userId, CreateDailyLogRequest req)
    {
        var result = await db.DailyLogs
            .FromSqlRaw(
                """
                EXEC sp_DailyLog_Upsert
                    @Id, @UserId, @LogDate,
                    @FoodNotes, @ExerciseMinutes, @ExerciseType,
                    @WaterIntakeMl, @Weight,
                    @BloodPressureSystolic, @BloodPressureDiastolic,
                    @MoodScore
                """,
                new SqlParameter("@Id",                    Guid.NewGuid()),
                new SqlParameter("@UserId",                userId),
                new SqlParameter("@LogDate",               req.LogDate.ToDateTime(TimeOnly.MinValue).Date),
                new SqlParameter("@FoodNotes",             (object?)req.FoodNotes            ?? DBNull.Value),
                new SqlParameter("@ExerciseMinutes",       (object?)req.ExerciseMinutes      ?? DBNull.Value),
                new SqlParameter("@ExerciseType",          (object?)req.ExerciseType         ?? DBNull.Value),
                new SqlParameter("@WaterIntakeMl",         (object?)req.WaterIntakeMl        ?? DBNull.Value),
                new SqlParameter("@Weight",                (object?)req.Weight               ?? DBNull.Value),
                new SqlParameter("@BloodPressureSystolic", (object?)req.BloodPressureSystolic ?? DBNull.Value),
                new SqlParameter("@BloodPressureDiastolic",(object?)req.BloodPressureDiastolic ?? DBNull.Value),
                new SqlParameter("@MoodScore",             (object?)req.MoodScore            ?? DBNull.Value))
            .AsNoTracking()
            .FirstAsync();

        return MapToDto(result);
    }

    private static DailyLogDto MapToDto(DailyLog d) => new(
        d.Id, d.LogDate, d.FoodNotes, d.ExerciseMinutes,
        d.ExerciseType, d.WaterIntakeMl, d.Weight,
        d.BloodPressureSystolic, d.BloodPressureDiastolic, d.MoodScore);
}
