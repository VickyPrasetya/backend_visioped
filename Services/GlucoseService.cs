using System.Data;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class GlucoseService(AppDbContext db)
{
    // ── Get all readings ──────────────────────────────────────────────────────
    public async Task<List<GlucoseDto>> GetAllAsync(Guid userId)
    {
        return await db.GlucoseReadings
            .FromSqlRaw("EXEC sp_Glucose_GetAll @UserId",
                new SqlParameter("@UserId", userId))
            .AsNoTracking()
            .Select(g => new GlucoseDto(g.Id, g.GlucoseLevel, g.MeasurementType, g.Notes, g.MeasuredAt))
            .ToListAsync();
    }

    // ── Get trends untuk chart ────────────────────────────────────────────────
    public async Task<List<GlucoseTrendPoint>> GetTrendsAsync(Guid userId, int days = 7)
    {
        // Pakai SP, kembalikan raw data per hari
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_Glucose_GetTrends";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@UserId", userId));
        cmd.Parameters.Add(new SqlParameter("@Days", days));

        var result = new List<GlucoseTrendPoint>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new GlucoseTrendPoint(
                reader["Day"]?.ToString() ?? "-",
                reader["Value"] == DBNull.Value ? null : (decimal?)(decimal)(double)reader["Value"],
                (DateTime)reader["Date"]
            ));
        }
        return result;
    }

    // ── Get by id ─────────────────────────────────────────────────────────────
    public async Task<GlucoseDto?> GetByIdAsync(Guid userId, Guid id)
    {
        var g = await db.GlucoseReadings
            .FromSqlRaw("EXEC sp_Glucose_GetById @UserId, @Id",
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (g is null) return null;
        return new GlucoseDto(g.Id, g.GlucoseLevel, g.MeasurementType, g.Notes, g.MeasuredAt);
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<GlucoseDto> CreateAsync(Guid userId, CreateGlucoseRequest req)
    {
        var newId = Guid.NewGuid();

        var result = await db.GlucoseReadings
            .FromSqlRaw(
                "EXEC sp_Glucose_Insert @Id, @UserId, @GlucoseLevel, @MeasurementType, @MeasuredAt, @Notes",
                new SqlParameter("@Id",              newId),
                new SqlParameter("@UserId",          userId),
                new SqlParameter("@GlucoseLevel",    req.GlucoseLevel),
                new SqlParameter("@MeasurementType", req.MeasurementType),
                new SqlParameter("@MeasuredAt",      req.MeasuredAt),
                new SqlParameter("@Notes",           (object?)req.Notes ?? DBNull.Value))
            .AsNoTracking()
            .FirstAsync();

        return new GlucoseDto(result.Id, result.GlucoseLevel, result.MeasurementType, result.Notes, result.MeasuredAt);
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_Glucose_Delete";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@UserId", userId));
        cmd.Parameters.Add(new SqlParameter("@Id", id));

        var rows = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(rows) > 0;
    }
}
