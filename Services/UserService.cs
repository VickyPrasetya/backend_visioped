using System.Data;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class UserService(AppDbContext db)
{
    // ── Get by id ─────────────────────────────────────────────────────────────
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_User_GetById";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@UserId", id));

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return ReadUserDto(reader);
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest req)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_User_Update";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@UserId",       id));
        cmd.Parameters.Add(new SqlParameter("@Name",         (object?)req.Name         ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@PhoneNumber",  (object?)req.PhoneNumber  ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@Gender",       (object?)req.Gender       ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@DiabetesType", (object?)req.DiabetesType ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@DateOfBirth",  req.DateOfBirth.HasValue
            ? (object)req.DateOfBirth.Value.Date
            : DBNull.Value));

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return ReadUserDto(reader);
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private static UserDto ReadUserDto(System.Data.Common.DbDataReader r) => new(
        (Guid)r["Id"],
        r["Name"].ToString()!,
        r["Email"].ToString()!,
        r["PhoneNumber"] == DBNull.Value ? null : r["PhoneNumber"].ToString(),
        r["Gender"]      == DBNull.Value ? null : r["Gender"].ToString(),
        r["DiabetesType"] == DBNull.Value ? null : r["DiabetesType"].ToString(),
        r["ProfilePictureUrl"] == DBNull.Value ? null : r["ProfilePictureUrl"].ToString(),
        r["DateOfBirth"] == DBNull.Value ? null : (DateTime?)r["DateOfBirth"]
    );
}
