using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<GlucoseReading> GlucoseReadings { get; set; }
    public DbSet<DailyLog> DailyLogs { get; set; }
    public DbSet<ScanResult> ScanResults { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Users ──────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Name).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(u => u.PhoneNumber).HasMaxLength(20);
            e.Property(u => u.Gender).HasMaxLength(10);
            e.Property(u => u.DiabetesType).HasMaxLength(20);
        });

        // ── GlucoseReadings ────────────────────────────────────
        modelBuilder.Entity<GlucoseReading>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.GlucoseLevel).HasColumnType("decimal(5,1)").IsRequired();
            e.Property(g => g.MeasurementType).HasMaxLength(20).IsRequired();
            e.HasOne(g => g.User)
             .WithMany(u => u.GlucoseReadings)
             .HasForeignKey(g => g.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DailyLogs ──────────────────────────────────────────
        modelBuilder.Entity<DailyLog>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Weight).HasColumnType("decimal(5,2)");
            e.HasOne(d => d.User)
             .WithMany(u => u.DailyLogs)
             .HasForeignKey(d => d.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ScanResults ────────────────────────────────────────
        modelBuilder.Entity<ScanResult>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.DiagnosisResult).HasMaxLength(50).IsRequired();
            e.Property(s => s.ConfidenceScore).HasColumnType("decimal(5,4)");
            e.HasOne(s => s.User)
             .WithMany(u => u.ScanResults)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Schedules ──────────────────────────────────────────
        modelBuilder.Entity<Schedule>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).HasMaxLength(150).IsRequired();
            e.Property(s => s.ScheduleType).HasMaxLength(30).IsRequired();
            e.HasOne(s => s.User)
             .WithMany(u => u.Schedules)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RefreshTokens ──────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed Data ──────────────────────────────────────────
        var userId1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var userId2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = userId1,
                Name = "Budi Santoso",
                Email = "budi@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                PhoneNumber = "081234567890",
                DiabetesType = "Type2",
                Gender = "Male",
                DateOfBirth = new DateTime(1985, 3, 15),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = userId2,
                Name = "Siti Rahayu",
                Email = "siti@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                PhoneNumber = "089876543210",
                DiabetesType = "PreDiabetes",
                Gender = "Female",
                DateOfBirth = new DateTime(1992, 7, 20),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed GlucoseReadings
        modelBuilder.Entity<GlucoseReading>().HasData(
            new GlucoseReading { Id = Guid.NewGuid(), UserId = userId1, GlucoseLevel = 118.5m, MeasurementType = "FastingBlood", MeasuredAt = DateTime.UtcNow.AddDays(-6), CreatedAt = DateTime.UtcNow },
            new GlucoseReading { Id = Guid.NewGuid(), UserId = userId1, GlucoseLevel = 145.0m, MeasurementType = "PostMeal", MeasuredAt = DateTime.UtcNow.AddDays(-5), CreatedAt = DateTime.UtcNow },
            new GlucoseReading { Id = Guid.NewGuid(), UserId = userId1, GlucoseLevel = 132.0m, MeasurementType = "PostMeal", MeasuredAt = DateTime.UtcNow.AddDays(-4), CreatedAt = DateTime.UtcNow },
            new GlucoseReading { Id = Guid.NewGuid(), UserId = userId1, GlucoseLevel = 109.0m, MeasurementType = "FastingBlood", MeasuredAt = DateTime.UtcNow.AddDays(-3), CreatedAt = DateTime.UtcNow },
            new GlucoseReading { Id = Guid.NewGuid(), UserId = userId1, GlucoseLevel = 127.5m, MeasurementType = "Random", MeasuredAt = DateTime.UtcNow.AddDays(-2), CreatedAt = DateTime.UtcNow },
            new GlucoseReading { Id = Guid.NewGuid(), UserId = userId1, GlucoseLevel = 138.0m, MeasurementType = "PostMeal", MeasuredAt = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow },
            new GlucoseReading { Id = Guid.NewGuid(), UserId = userId1, GlucoseLevel = 121.0m, MeasurementType = "FastingBlood", MeasuredAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        );

        // Seed Schedules
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        modelBuilder.Entity<Schedule>().HasData(
            new Schedule { Id = Guid.NewGuid(), UserId = userId1, Title = "Cek Gula Darah Pagi", ScheduleType = "GlucoseCheck", ScheduledDate = today, ScheduledTime = new TimeOnly(7, 0), IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new Schedule { Id = Guid.NewGuid(), UserId = userId1, Title = "Minum Metformin", ScheduleType = "Medication", ScheduledDate = today, ScheduledTime = new TimeOnly(8, 0), IsCompleted = true, CreatedAt = DateTime.UtcNow },
            new Schedule { Id = Guid.NewGuid(), UserId = userId1, Title = "Jalan Kaki 30 Menit", ScheduleType = "Exercise", ScheduledDate = today, ScheduledTime = new TimeOnly(16, 0), IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new Schedule { Id = Guid.NewGuid(), UserId = userId1, Title = "Kontrol Dokter", ScheduleType = "DoctorVisit", ScheduledDate = today.AddDays(3), ScheduledTime = new TimeOnly(10, 0), IsCompleted = false, CreatedAt = DateTime.UtcNow }
        );
    }
}
