-- ============================================================
-- VisioPed – Full Database Script
-- Server : LAPTOP-7B4A2GEF\SQLEXPRESS
-- Database: VisioPedDb
-- Jalankan file ini di SQL Server Management Studio (SSMS)
--   File → Open → pilih file ini → F5 / Execute
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'VisioPedDb')
BEGIN
    CREATE DATABASE VisioPedDb;
    PRINT '>> Database VisioPedDb berhasil dibuat.';
END
ELSE
    PRINT '>> Database VisioPedDb sudah ada, skip create.';
GO

USE VisioPedDb;
GO

-- ==============================================================
-- BAGIAN 1 : BUAT TABEL
-- ==============================================================
PRINT '========== MEMBUAT TABEL ==========';

-- ── Users ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  PRIMARY KEY,
        Name              NVARCHAR(100)    NOT NULL,
        Email             NVARCHAR(150)    NOT NULL,
        PasswordHash      NVARCHAR(255)    NOT NULL,
        PhoneNumber       NVARCHAR(20)     NULL,
        DateOfBirth       DATE             NULL,
        Gender            NVARCHAR(10)     NULL,    -- Male / Female
        ProfilePictureUrl NVARCHAR(MAX)    NULL,
        DiabetesType      NVARCHAR(20)     NULL,    -- Type1 / Type2 / PreDiabetes / None
        CreatedAt         DATETIME2(0)     NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt         DATETIME2(0)     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
    PRINT '   Tabel Users dibuat.';
END
GO

-- ── GlucoseReadings ────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GlucoseReadings')
BEGIN
    CREATE TABLE GlucoseReadings (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL,
        GlucoseLevel    DECIMAL(5,1)     NOT NULL,               -- mg/dL
        MeasurementType NVARCHAR(20)     NOT NULL,               -- FastingBlood | PostMeal | Random | BeforeSleep
        Notes           NVARCHAR(MAX)    NULL,
        MeasuredAt      DATETIME2(0)     NOT NULL,
        CreatedAt       DATETIME2(0)     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Glucose_Users FOREIGN KEY (UserId)
            REFERENCES Users(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Glucose_UserId_MeasuredAt ON GlucoseReadings (UserId, MeasuredAt DESC);
    PRINT '   Tabel GlucoseReadings dibuat.';
END
GO

-- ── DailyLogs ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DailyLogs')
BEGIN
    CREATE TABLE DailyLogs (
        Id                     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  PRIMARY KEY,
        UserId                 UNIQUEIDENTIFIER NOT NULL,
        LogDate                DATE             NOT NULL,
        FoodNotes              NVARCHAR(MAX)    NULL,
        ExerciseMinutes        INT              NULL,
        ExerciseType           NVARCHAR(50)     NULL,
        WaterIntakeMl          INT              NULL,
        Weight                 DECIMAL(5,2)     NULL,            -- kg
        BloodPressureSystolic  INT              NULL,
        BloodPressureDiastolic INT              NULL,
        MoodScore              INT              NULL,            -- 1‑5
        CreatedAt              DATETIME2(0)     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_DailyLogs_Users FOREIGN KEY (UserId)
            REFERENCES Users(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_DailyLogs_UserDate UNIQUE (UserId, LogDate)
    );
    CREATE INDEX IX_DailyLogs_UserId_LogDate ON DailyLogs (UserId, LogDate);
    PRINT '   Tabel DailyLogs dibuat.';
END
GO

-- ── ScanResults ────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScanResults')
BEGIN
    CREATE TABLE ScanResults (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL,
        ImageUrl        NVARCHAR(MAX)    NOT NULL,
        DiagnosisResult NVARCHAR(50)     NOT NULL,               -- Normal | Mild | Moderate | Severe
        ConfidenceScore DECIMAL(5,4)     NULL,                   -- 0.0000‑1.0000
        Notes           NVARCHAR(MAX)    NULL,
        ScannedAt       DATETIME2(0)     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Scan_Users FOREIGN KEY (UserId)
            REFERENCES Users(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_ScanResults_UserId_ScannedAt ON ScanResults (UserId, ScannedAt DESC);
    PRINT '   Tabel ScanResults dibuat.';
END
GO

-- ── Schedules ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Schedules')
BEGIN
    CREATE TABLE Schedules (
        Id            UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  PRIMARY KEY,
        UserId        UNIQUEIDENTIFIER NOT NULL,
        Title         NVARCHAR(150)    NOT NULL,
        Description   NVARCHAR(MAX)    NULL,
        ScheduleType  NVARCHAR(30)     NOT NULL,                 -- Medication | GlucoseCheck | Exercise | DoctorVisit
        ScheduledDate DATE             NOT NULL,
        ScheduledTime TIME             NULL,
        IsCompleted   BIT              NOT NULL DEFAULT 0,
        CreatedAt     DATETIME2(0)     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Schedules_Users FOREIGN KEY (UserId)
            REFERENCES Users(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Schedules_UserId_Date ON Schedules (UserId, ScheduledDate);
    PRINT '   Tabel Schedules dibuat.';
END
GO

-- ── RefreshTokens ──────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE RefreshTokens (
        Id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()  PRIMARY KEY,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        Token     NVARCHAR(MAX)    NOT NULL,
        ExpiresAt DATETIME2(0)     NOT NULL,
        CreatedAt DATETIME2(0)     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId)
            REFERENCES Users(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens (UserId);
    PRINT '   Tabel RefreshTokens dibuat.';
END
GO

-- ==============================================================
-- BAGIAN 2 : STORED PROCEDURES
-- ==============================================================
PRINT '';
PRINT '========== MEMBUAT STORED PROCEDURES ==========';
GO  -- ← GO wajib di sini agar CREATE PROCEDURE di bawah jadi batch baru

-- ──────────────────────────────────────────────────────────────
-- sp_Auth_Register
-- Daftar user baru; return 0 jika sukses, -1 jika email sudah ada
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Auth_Register
    @Id            UNIQUEIDENTIFIER,
    @Name          NVARCHAR(100),
    @Email         NVARCHAR(150),
    @PasswordHash  NVARCHAR(255),
    @PhoneNumber   NVARCHAR(20)   = NULL,
    @DiabetesType  NVARCHAR(20)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
    BEGIN
        SELECT -1 AS ResultCode, N'Email sudah terdaftar.' AS Message;
        RETURN;
    END

    INSERT INTO Users (Id, Name, Email, PasswordHash, PhoneNumber, DiabetesType)
    VALUES (@Id, @Name, @Email, @PasswordHash, @PhoneNumber, @DiabetesType);

    SELECT 0 AS ResultCode, N'Registrasi berhasil.' AS Message;
END
GO
PRINT '   SP sp_Auth_Register dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Auth_GetUserByEmail
-- Cari user berdasarkan email (untuk verifikasi login)
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Auth_GetUserByEmail
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Email, PasswordHash, PhoneNumber, Gender,
           DiabetesType, ProfilePictureUrl, DateOfBirth, CreatedAt, UpdatedAt
    FROM   Users
    WHERE  Email = @Email;
END
GO
PRINT '   SP sp_Auth_GetUserByEmail dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Auth_SaveRefreshToken
-- Simpan refresh token baru
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Auth_SaveRefreshToken
    @Id        UNIQUEIDENTIFIER,
    @UserId    UNIQUEIDENTIFIER,
    @Token     NVARCHAR(MAX),
    @ExpiresAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO RefreshTokens (Id, UserId, Token, ExpiresAt)
    VALUES (@Id, @UserId, @Token, @ExpiresAt);
END
GO
PRINT '   SP sp_Auth_SaveRefreshToken dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Auth_GetRefreshToken
-- Cek apakah refresh token valid dan belum kadaluarsa
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Auth_GetRefreshToken
    @Token NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT rt.Id, rt.UserId, rt.Token, rt.ExpiresAt,
           u.Id    AS UserId,
           u.Name, u.Email, u.PasswordHash, u.PhoneNumber,
           u.Gender, u.DiabetesType, u.ProfilePictureUrl, u.DateOfBirth
    FROM   RefreshTokens rt
    JOIN   Users u ON u.Id = rt.UserId
    WHERE  rt.Token     = @Token
      AND  rt.ExpiresAt > GETUTCDATE();
END
GO
PRINT '   SP sp_Auth_GetRefreshToken dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Auth_DeleteRefreshToken
-- Hapus satu refresh token (saat rotate)
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Auth_DeleteRefreshToken
    @Token NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RefreshTokens WHERE Token = @Token;
END
GO
PRINT '   SP sp_Auth_DeleteRefreshToken dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Auth_DeleteAllRefreshTokensByUser
-- Logout – hapus semua token milik user
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Auth_DeleteAllRefreshTokensByUser
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RefreshTokens WHERE UserId = @UserId;
END
GO
PRINT '   SP sp_Auth_DeleteAllRefreshTokensByUser dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_User_GetById
-- Ambil profil user berdasarkan Id
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_User_GetById
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Email, PhoneNumber, Gender,
           DiabetesType, ProfilePictureUrl, DateOfBirth, CreatedAt, UpdatedAt
    FROM   Users
    WHERE  Id = @UserId;
END
GO
PRINT '   SP sp_User_GetById dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_User_Update
-- Update profil user dengan COALESCE (hanya field yang dikirim)
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_User_Update
    @UserId       UNIQUEIDENTIFIER,
    @Name         NVARCHAR(100)  = NULL,
    @PhoneNumber  NVARCHAR(20)   = NULL,
    @Gender       NVARCHAR(10)   = NULL,
    @DiabetesType NVARCHAR(20)   = NULL,
    @DateOfBirth  DATE           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users
    SET
        Name         = COALESCE(@Name,         Name),
        PhoneNumber  = COALESCE(@PhoneNumber,  PhoneNumber),
        Gender       = COALESCE(@Gender,       Gender),
        DiabetesType = COALESCE(@DiabetesType, DiabetesType),
        DateOfBirth  = COALESCE(@DateOfBirth,  DateOfBirth),
        UpdatedAt    = GETUTCDATE()
    WHERE Id = @UserId;

    EXEC sp_User_GetById @UserId;
END
GO
PRINT '   SP sp_User_Update dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Glucose_GetAll
-- Ambil semua pembacaan glukosa milik user (urut terbaru)
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Glucose_GetAll
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, GlucoseLevel, MeasurementType, Notes, MeasuredAt, CreatedAt
    FROM   GlucoseReadings
    WHERE  UserId = @UserId
    ORDER  BY MeasuredAt DESC;
END
GO
PRINT '   SP sp_Glucose_GetAll dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Glucose_GetById
-- Ambil satu pembacaan berdasarkan Id
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Glucose_GetById
    @UserId UNIQUEIDENTIFIER,
    @Id     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, GlucoseLevel, MeasurementType, Notes, MeasuredAt, CreatedAt
    FROM   GlucoseReadings
    WHERE  UserId = @UserId AND Id = @Id;
END
GO
PRINT '   SP sp_Glucose_GetById dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Glucose_GetTrends
-- Rata-rata glukosa per hari selama N hari terakhir (untuk chart)
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Glucose_GetTrends
    @UserId UNIQUEIDENTIFIER,
    @Days   INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    WITH DateSeries AS (
        SELECT CAST(DATEADD(DAY, -(n.num), CAST(GETUTCDATE() AS DATE)) AS DATE) AS TanggalHari,
               FORMAT(DATEADD(DAY, -(n.num), GETUTCDATE()), 'ddd')              AS NamaHari
        FROM (
            SELECT TOP (@Days) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS num
            FROM   sys.columns
        ) n
    ),
    GlucosePerDay AS (
        SELECT CAST(MeasuredAt AS DATE) AS TanggalHari,
               AVG(GlucoseLevel)        AS AvgGlucose,
               COUNT(*)                 AS JumlahData,
               MAX(GlucoseLevel)        AS MaxGlucose,
               MIN(GlucoseLevel)        AS MinGlucose
        FROM   GlucoseReadings
        WHERE  UserId    = @UserId
          AND  MeasuredAt >= DATEADD(DAY, -@Days, GETUTCDATE())
        GROUP  BY CAST(MeasuredAt AS DATE)
    )
    SELECT
        ds.TanggalHari              AS [Date],
        ds.NamaHari                 AS [Day],
        gpd.AvgGlucose              AS [Value],
        gpd.MaxGlucose              AS [MaxValue],
        gpd.MinGlucose              AS [MinValue],
        COALESCE(gpd.JumlahData, 0) AS [DataCount]
    FROM   DateSeries ds
    LEFT   JOIN GlucosePerDay gpd ON gpd.TanggalHari = ds.TanggalHari
    ORDER  BY ds.TanggalHari;
END
GO
PRINT '   SP sp_Glucose_GetTrends dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Glucose_GetStats
-- Statistik ringkasan: rata-rata, tertinggi, terendah, total
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Glucose_GetStats
    @UserId UNIQUEIDENTIFIER,
    @Days   INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(*)          AS TotalReadings,
        AVG(GlucoseLevel) AS AvgGlucose,
        MAX(GlucoseLevel) AS MaxGlucose,
        MIN(GlucoseLevel) AS MinGlucose,
        SUM(CASE WHEN GlucoseLevel < 70                      THEN 1 ELSE 0 END) AS CountLow,
        SUM(CASE WHEN GlucoseLevel BETWEEN 70  AND 99        THEN 1 ELSE 0 END) AS CountNormal,
        SUM(CASE WHEN GlucoseLevel BETWEEN 100 AND 125       THEN 1 ELSE 0 END) AS CountPreDiabetes,
        SUM(CASE WHEN GlucoseLevel >= 126                    THEN 1 ELSE 0 END) AS CountHigh
    FROM   GlucoseReadings
    WHERE  UserId    = @UserId
      AND  MeasuredAt >= DATEADD(DAY, -@Days, GETUTCDATE());
END
GO
PRINT '   SP sp_Glucose_GetStats dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Glucose_Insert
-- Tambah pembacaan glukosa baru
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Glucose_Insert
    @Id              UNIQUEIDENTIFIER,
    @UserId          UNIQUEIDENTIFIER,
    @GlucoseLevel    DECIMAL(5,1),
    @MeasurementType NVARCHAR(20),
    @MeasuredAt      DATETIME2,
    @Notes           NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO GlucoseReadings (Id, UserId, GlucoseLevel, MeasurementType, MeasuredAt, Notes)
    VALUES (@Id, @UserId, @GlucoseLevel, @MeasurementType, @MeasuredAt, @Notes);

    EXEC sp_Glucose_GetById @UserId, @Id;
END
GO
PRINT '   SP sp_Glucose_Insert dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Glucose_Delete
-- Hapus satu pembacaan glukosa milik user
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Glucose_Delete
    @UserId UNIQUEIDENTIFIER,
    @Id     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RowsAffected INT;

    DELETE FROM GlucoseReadings
    WHERE UserId = @UserId AND Id = @Id;

    SET @RowsAffected = @@ROWCOUNT;
    SELECT @RowsAffected AS RowsDeleted;
END
GO
PRINT '   SP sp_Glucose_Delete dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Schedule_GetByDate
-- Ambil jadwal berdasarkan tanggal tertentu
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Schedule_GetByDate
    @UserId        UNIQUEIDENTIFIER,
    @ScheduledDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Title, Description, ScheduleType,
           ScheduledDate, ScheduledTime, IsCompleted, CreatedAt
    FROM   Schedules
    WHERE  UserId        = @UserId
      AND  ScheduledDate = @ScheduledDate
    ORDER  BY ScheduledTime;
END
GO
PRINT '   SP sp_Schedule_GetByDate dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Schedule_GetAll
-- Ambil semua jadwal milik user
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Schedule_GetAll
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Title, Description, ScheduleType,
           ScheduledDate, ScheduledTime, IsCompleted, CreatedAt
    FROM   Schedules
    WHERE  UserId = @UserId
    ORDER  BY ScheduledDate, ScheduledTime;
END
GO
PRINT '   SP sp_Schedule_GetAll dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Schedule_GetById
-- Ambil satu jadwal berdasarkan Id
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Schedule_GetById
    @UserId UNIQUEIDENTIFIER,
    @Id     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Title, Description, ScheduleType,
           ScheduledDate, ScheduledTime, IsCompleted, CreatedAt
    FROM   Schedules
    WHERE  UserId = @UserId AND Id = @Id;
END
GO
PRINT '   SP sp_Schedule_GetById dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Schedule_Insert
-- Buat jadwal baru
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Schedule_Insert
    @Id            UNIQUEIDENTIFIER,
    @UserId        UNIQUEIDENTIFIER,
    @Title         NVARCHAR(150),
    @Description   NVARCHAR(MAX)  = NULL,
    @ScheduleType  NVARCHAR(30),
    @ScheduledDate DATE,
    @ScheduledTime TIME           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Schedules (Id, UserId, Title, Description, ScheduleType, ScheduledDate, ScheduledTime)
    VALUES (@Id, @UserId, @Title, @Description, @ScheduleType, @ScheduledDate, @ScheduledTime);

    EXEC sp_Schedule_GetById @UserId, @Id;
END
GO
PRINT '   SP sp_Schedule_Insert dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Schedule_Update
-- Update jadwal (termasuk mark sebagai selesai)
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Schedule_Update
    @UserId        UNIQUEIDENTIFIER,
    @Id            UNIQUEIDENTIFIER,
    @Title         NVARCHAR(150)  = NULL,
    @Description   NVARCHAR(MAX)  = NULL,
    @IsCompleted   BIT            = NULL,
    @ScheduledDate DATE           = NULL,
    @ScheduledTime TIME           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Schedules WHERE Id = @Id AND UserId = @UserId)
    BEGIN
        SELECT -1 AS ResultCode;
        RETURN;
    END

    UPDATE Schedules
    SET
        Title         = COALESCE(@Title,         Title),
        Description   = COALESCE(@Description,   Description),
        IsCompleted   = COALESCE(@IsCompleted,   IsCompleted),
        ScheduledDate = COALESCE(@ScheduledDate, ScheduledDate),
        ScheduledTime = COALESCE(@ScheduledTime, ScheduledTime)
    WHERE Id = @Id AND UserId = @UserId;

    EXEC sp_Schedule_GetById @UserId, @Id;
END
GO
PRINT '   SP sp_Schedule_Update dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Schedule_Delete
-- Hapus jadwal
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Schedule_Delete
    @UserId UNIQUEIDENTIFIER,
    @Id     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Schedules WHERE UserId = @UserId AND Id = @Id;
    SELECT @@ROWCOUNT AS RowsDeleted;
END
GO
PRINT '   SP sp_Schedule_Delete dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_Schedule_GetUpcoming
-- Jadwal mendatang N hari ke depan (untuk notifikasi)
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Schedule_GetUpcoming
    @UserId UNIQUEIDENTIFIER,
    @Days   INT = 7
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Title, Description, ScheduleType,
           ScheduledDate, ScheduledTime, IsCompleted
    FROM   Schedules
    WHERE  UserId        = @UserId
      AND  IsCompleted   = 0
      AND  ScheduledDate BETWEEN CAST(GETUTCDATE() AS DATE)
                             AND CAST(DATEADD(DAY, @Days, GETUTCDATE()) AS DATE)
    ORDER  BY ScheduledDate, ScheduledTime;
END
GO
PRINT '   SP sp_Schedule_GetUpcoming dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_DailyLog_GetByDate
-- Ambil log harian berdasarkan tanggal
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_DailyLog_GetByDate
    @UserId  UNIQUEIDENTIFIER,
    @LogDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, LogDate, FoodNotes, ExerciseMinutes, ExerciseType,
           WaterIntakeMl, Weight, BloodPressureSystolic,
           BloodPressureDiastolic, MoodScore, CreatedAt
    FROM   DailyLogs
    WHERE  UserId  = @UserId
      AND  LogDate = @LogDate;
END
GO
PRINT '   SP sp_DailyLog_GetByDate dibuat.';
GO

-- ──────────────────────────────────────────────────────────────
-- sp_DailyLog_Upsert
-- Insert atau Update log harian (berdasarkan UserId + LogDate)
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_DailyLog_Upsert
    @Id                    UNIQUEIDENTIFIER,
    @UserId                UNIQUEIDENTIFIER,
    @LogDate               DATE,
    @FoodNotes             NVARCHAR(MAX) = NULL,
    @ExerciseMinutes       INT           = NULL,
    @ExerciseType          NVARCHAR(50)  = NULL,
    @WaterIntakeMl         INT           = NULL,
    @Weight                DECIMAL(5,2)  = NULL,
    @BloodPressureSystolic INT           = NULL,
    @BloodPressureDiastolic INT          = NULL,
    @MoodScore             INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM DailyLogs WHERE UserId = @UserId AND LogDate = @LogDate)
    BEGIN
        -- Update jika sudah ada log hari ini
        UPDATE DailyLogs
        SET
            FoodNotes              = COALESCE(@FoodNotes,             FoodNotes),
            ExerciseMinutes        = COALESCE(@ExerciseMinutes,       ExerciseMinutes),
            ExerciseType           = COALESCE(@ExerciseType,          ExerciseType),
            WaterIntakeMl          = COALESCE(@WaterIntakeMl,         WaterIntakeMl),
            Weight                 = COALESCE(@Weight,                Weight),
            BloodPressureSystolic  = COALESCE(@BloodPressureSystolic, BloodPressureSystolic),
            BloodPressureDiastolic = COALESCE(@BloodPressureDiastolic,BloodPressureDiastolic),
            MoodScore              = COALESCE(@MoodScore,             MoodScore)
        WHERE UserId = @UserId AND LogDate = @LogDate;
    END
    ELSE
    BEGIN
        -- Insert jika belum ada
        INSERT INTO DailyLogs (
            Id, UserId, LogDate, FoodNotes, ExerciseMinutes, ExerciseType,
            WaterIntakeMl, Weight, BloodPressureSystolic, BloodPressureDiastolic, MoodScore
        )
        VALUES (
            @Id, @UserId, @LogDate, @FoodNotes, @ExerciseMinutes, @ExerciseType,
            @WaterIntakeMl, @Weight, @BloodPressureSystolic, @BloodPressureDiastolic, @MoodScore
        );
    END

    -- Return data terbaru
    EXEC sp_DailyLog_GetByDate @UserId, @LogDate;
END
GO
PRINT '   SP sp_DailyLog_Upsert dibuat.';

-- ──────────────────────────────────────────────────────────────
-- SP_DailyLog_GetHistory
-- Riwayat log harian N hari terakhir
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_DailyLog_GetHistory
    @UserId UNIQUEIDENTIFIER,
    @Days   INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, LogDate, FoodNotes, ExerciseMinutes, ExerciseType,
           WaterIntakeMl, Weight, BloodPressureSystolic,
           BloodPressureDiastolic, MoodScore, CreatedAt
    FROM   DailyLogs
    WHERE  UserId  = @UserId
      AND  LogDate >= DATEADD(DAY, -@Days, CAST(GETUTCDATE() AS DATE))
    ORDER  BY LogDate DESC;
END
GO
PRINT '   SP sp_DailyLog_GetHistory dibuat.';

-- ──────────────────────────────────────────────────────────────
-- SP_Scan_GetHistory
-- Riwayat hasil scan foto luka/ulcer
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Scan_GetHistory
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ImageUrl, DiagnosisResult, ConfidenceScore, Notes, ScannedAt
    FROM   ScanResults
    WHERE  UserId = @UserId
    ORDER  BY ScannedAt DESC;
END
GO
PRINT '   SP sp_Scan_GetHistory dibuat.';

-- ──────────────────────────────────────────────────────────────
-- SP_Scan_Insert
-- Simpan hasil scan baru
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Scan_Insert
    @Id              UNIQUEIDENTIFIER,
    @UserId          UNIQUEIDENTIFIER,
    @ImageUrl        NVARCHAR(MAX),
    @DiagnosisResult NVARCHAR(50),
    @ConfidenceScore DECIMAL(5,4) = NULL,
    @Notes           NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ScanResults (Id, UserId, ImageUrl, DiagnosisResult, ConfidenceScore, Notes)
    VALUES (@Id, @UserId, @ImageUrl, @DiagnosisResult, @ConfidenceScore, @Notes);

    SELECT Id, ImageUrl, DiagnosisResult, ConfidenceScore, Notes, ScannedAt
    FROM   ScanResults
    WHERE  Id = @Id;
END
GO
PRINT '   SP sp_Scan_Insert dibuat.';

-- ──────────────────────────────────────────────────────────────
-- SP_Dashboard_Summary
-- Ringkasan dashboard: glukosa hari ini, jadwal hari ini, dll
-- ──────────────────────────────────────────────────────────────
CREATE PROCEDURE sp_Dashboard_Summary
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);

    -- Glukosa terakhir
    SELECT TOP 1
        GlucoseLevel     AS LastGlucose,
        MeasurementType  AS LastMeasurementType,
        MeasuredAt       AS LastMeasuredAt
    FROM   GlucoseReadings
    WHERE  UserId = @UserId
    ORDER  BY MeasuredAt DESC;

    -- Statistik glukosa 7 hari
    SELECT
        ROUND(AVG(CAST(GlucoseLevel AS FLOAT)), 1) AS AvgGlucose7Days,
        MAX(GlucoseLevel)                           AS MaxGlucose7Days,
        MIN(GlucoseLevel)                           AS MinGlucose7Days,
        COUNT(*)                                    AS TotalReadings7Days
    FROM   GlucoseReadings
    WHERE  UserId    = @UserId
      AND  MeasuredAt >= DATEADD(DAY, -7, GETUTCDATE());

    -- Jadwal hari ini
    SELECT
        COUNT(*)                                         AS TotalScheduleToday,
        SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) AS CompletedToday,
        SUM(CASE WHEN IsCompleted = 0 THEN 1 ELSE 0 END) AS PendingToday
    FROM   Schedules
    WHERE  UserId        = @UserId
      AND  ScheduledDate = @Today;
END
GO
PRINT '   SP sp_Dashboard_Summary dibuat.';

-- ==============================================================
-- BAGIAN 3 : SEED DATA
-- ==============================================================
PRINT '';
PRINT '========== INSERT SEED DATA ==========';

DECLARE @UserId1 UNIQUEIDENTIFIER = 'A1B2C3D4-E5F6-7890-ABCD-EF1234567890';
DECLARE @UserId2 UNIQUEIDENTIFIER = 'B2C3D4E5-F6A7-8901-BCDE-F12345678901';

-- Users (password: password123 – di-hash BCrypt)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'budi@example.com')
BEGIN
    INSERT INTO Users (Id, Name, Email, PasswordHash, PhoneNumber, DiabetesType, Gender, DateOfBirth)
    VALUES (
        @UserId1, 'Budi Santoso', 'budi@example.com',
        '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy',
        '081234567890', 'Type2', 'Male', '1985-03-15'
    );
    PRINT '   User Budi Santoso ditambahkan.';
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'siti@example.com')
BEGIN
    INSERT INTO Users (Id, Name, Email, PasswordHash, PhoneNumber, DiabetesType, Gender, DateOfBirth)
    VALUES (
        @UserId2, 'Siti Rahayu', 'siti@example.com',
        '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy',
        '089876543210', 'PreDiabetes', 'Female', '1992-07-20'
    );
    PRINT '   User Siti Rahayu ditambahkan.';
END

-- GlucoseReadings (7 hari terakhir untuk Budi)
IF NOT EXISTS (SELECT 1 FROM GlucoseReadings WHERE UserId = @UserId1)
BEGIN
    INSERT INTO GlucoseReadings (UserId, GlucoseLevel, MeasurementType, MeasuredAt)
    VALUES
        (@UserId1, 118.5, 'FastingBlood', DATEADD(DAY,-6,GETUTCDATE())),
        (@UserId1, 145.0, 'PostMeal',     DATEADD(DAY,-5,GETUTCDATE())),
        (@UserId1, 132.0, 'PostMeal',     DATEADD(DAY,-4,GETUTCDATE())),
        (@UserId1, 109.0, 'FastingBlood', DATEADD(DAY,-3,GETUTCDATE())),
        (@UserId1, 127.5, 'Random',       DATEADD(DAY,-2,GETUTCDATE())),
        (@UserId1, 138.0, 'PostMeal',     DATEADD(DAY,-1,GETUTCDATE())),
        (@UserId1, 121.0, 'FastingBlood', GETUTCDATE());

    INSERT INTO GlucoseReadings (UserId, GlucoseLevel, MeasurementType, MeasuredAt)
    VALUES
        (@UserId2, 115.0, 'FastingBlood', DATEADD(DAY,-3,GETUTCDATE())),
        (@UserId2, 128.5, 'PostMeal',     DATEADD(DAY,-1,GETUTCDATE()));

    PRINT '   Data GlucoseReadings ditambahkan.';
END

-- Schedules
IF NOT EXISTS (SELECT 1 FROM Schedules WHERE UserId = @UserId1)
BEGIN
    INSERT INTO Schedules (UserId, Title, ScheduleType, ScheduledDate, ScheduledTime, IsCompleted)
    VALUES
        (@UserId1, 'Cek Gula Darah Pagi',  'GlucoseCheck', CAST(GETUTCDATE() AS DATE), '07:00', 0),
        (@UserId1, 'Minum Metformin',       'Medication',   CAST(GETUTCDATE() AS DATE), '08:00', 1),
        (@UserId1, 'Jalan Kaki 30 Menit',  'Exercise',     CAST(GETUTCDATE() AS DATE), '16:00', 0),
        (@UserId1, 'Cek Gula Darah Malam', 'GlucoseCheck', CAST(GETUTCDATE() AS DATE), '20:00', 0),
        (@UserId1, 'Kontrol ke Dokter',     'DoctorVisit',  CAST(DATEADD(DAY,3,GETUTCDATE()) AS DATE), '10:00', 0);
    PRINT '   Data Schedules ditambahkan.';
END

-- DailyLog hari ini
IF NOT EXISTS (SELECT 1 FROM DailyLogs WHERE UserId = @UserId1 AND LogDate = CAST(GETUTCDATE() AS DATE))
BEGIN
    INSERT INTO DailyLogs (
        UserId, LogDate, FoodNotes, ExerciseMinutes, ExerciseType,
        WaterIntakeMl, Weight, BloodPressureSystolic, BloodPressureDiastolic, MoodScore
    )
    VALUES (
        @UserId1, CAST(GETUTCDATE() AS DATE),
        'Nasi merah, sayur bayam, ayam rebus', 30, 'Jalan kaki',
        2000, 72.5, 120, 80, 4
    );
    PRINT '   Data DailyLog ditambahkan.';
END

GO

-- ==============================================================
-- BAGIAN 4 : VERIFIKASI
-- ==============================================================
PRINT '';
PRINT '========== VERIFIKASI HASIL ==========';

SELECT 'Users'           AS Tabel, COUNT(*) AS JumlahData FROM Users          UNION ALL
SELECT 'GlucoseReadings',          COUNT(*)               FROM GlucoseReadings UNION ALL
SELECT 'Schedules',                COUNT(*)               FROM Schedules        UNION ALL
SELECT 'DailyLogs',                COUNT(*)               FROM DailyLogs        UNION ALL
SELECT 'ScanResults',              COUNT(*)               FROM ScanResults;

SELECT 'Total SP' AS Info,
       COUNT(*)   AS Jumlah
FROM   sys.procedures
WHERE  name LIKE 'sp_%'
  AND  SCHEMA_NAME(schema_id) = 'dbo';

PRINT '';
PRINT '==============================================';
PRINT '  SETUP SELESAI!';
PRINT '  Login test:';
PRINT '  Email   : budi@example.com';
PRINT '  Password: password123';
PRINT '==============================================';
GO
