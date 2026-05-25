using System.Data;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ScheduleService(AppDbContext db)
{
    // ── Get by date ───────────────────────────────────────────────────────────
    public async Task<List<ScheduleDto>> GetByDateAsync(Guid userId, DateOnly date)
    {
        var schedules = await db.Schedules
            .FromSqlRaw("EXEC sp_Schedule_GetByDate @UserId, @ScheduledDate",
                new SqlParameter("@UserId",        userId),
                new SqlParameter("@ScheduledDate", date.ToDateTime(TimeOnly.MinValue).Date))
            .AsNoTracking()
            .ToListAsync();

        return schedules.Select(MapToDto).ToList();
    }

    // ── Get all ───────────────────────────────────────────────────────────────
    public async Task<List<ScheduleDto>> GetAllAsync(Guid userId)
    {
        var schedules = await db.Schedules
            .FromSqlRaw("EXEC sp_Schedule_GetAll @UserId",
                new SqlParameter("@UserId", userId))
            .AsNoTracking()
            .ToListAsync();

        return schedules.Select(MapToDto).ToList();
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<ScheduleDto> CreateAsync(Guid userId, CreateScheduleRequest req)
    {
        var newId = Guid.NewGuid();

        var result = await db.Schedules
            .FromSqlRaw(
                "EXEC sp_Schedule_Insert @Id, @UserId, @Title, @Description, @ScheduleType, @ScheduledDate, @ScheduledTime",
                new SqlParameter("@Id",            newId),
                new SqlParameter("@UserId",        userId),
                new SqlParameter("@Title",         req.Title),
                new SqlParameter("@Description",   (object?)req.Description ?? DBNull.Value),
                new SqlParameter("@ScheduleType",  req.ScheduleType),
                new SqlParameter("@ScheduledDate", req.ScheduledDate.ToDateTime(TimeOnly.MinValue).Date),
                new SqlParameter("@ScheduledTime", req.ScheduledTime.HasValue
                    ? (object)req.ScheduledTime.Value.ToTimeSpan()
                    : DBNull.Value))
            .AsNoTracking()
            .FirstAsync();

        return MapToDto(result);
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public async Task<ScheduleDto?> UpdateAsync(Guid userId, Guid id, UpdateScheduleRequest req)
    {
        var result = await db.Schedules
            .FromSqlRaw(
                "EXEC sp_Schedule_Update @UserId, @Id, @Title, @Description, @IsCompleted, @ScheduledDate, @ScheduledTime",
                new SqlParameter("@UserId",        userId),
                new SqlParameter("@Id",            id),
                new SqlParameter("@Title",         (object?)req.Title ?? DBNull.Value),
                new SqlParameter("@Description",   (object?)req.Description ?? DBNull.Value),
                new SqlParameter("@IsCompleted",   req.IsCompleted.HasValue ? (object)req.IsCompleted.Value : DBNull.Value),
                new SqlParameter("@ScheduledDate", req.ScheduledDate.HasValue
                    ? (object)req.ScheduledDate.Value.ToDateTime(TimeOnly.MinValue).Date
                    : DBNull.Value),
                new SqlParameter("@ScheduledTime", req.ScheduledTime.HasValue
                    ? (object)req.ScheduledTime.Value.ToTimeSpan()
                    : DBNull.Value))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return result is null ? null : MapToDto(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_Schedule_Delete";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@UserId", userId));
        cmd.Parameters.Add(new SqlParameter("@Id", id));

        var rows = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(rows) > 0;
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private static ScheduleDto MapToDto(Schedule s) => new(
        s.Id, s.Title, s.Description, s.ScheduleType,
        s.ScheduledDate, s.ScheduledTime, s.IsCompleted);
}
