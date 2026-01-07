SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'DatwiseSafetyDemo') IS NULL
BEGIN
    CREATE DATABASE DatwiseSafetyDemo;
END
GO

USE DatwiseSafetyDemo;
GO

BEGIN TRY
    BEGIN TRAN;

    -------------------------------------------------------------------------
    -- 1) Upsert demo users (do NOT delete; keeps any user-created hazards intact)
    -------------------------------------------------------------------------
    DECLARE @Iterations INT = 100000;
    DECLARE @Algo NVARCHAR(50) = N'PBKDF2-SHA1';

    -- safety
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'safety')
        UPDATE dbo.Users
        SET FullName = N'Safety Officer',
            Role = N'SafetyOfficer',
            IsActive = 1,
            PasswordHash = 0xe0ec3b721e83721285ca813bcd79123a71047957dc8b21892731d5c7573f07b2,
            PasswordSalt = 0x856e8bd358ece398c72f1003391aef5d,
            PasswordIterations = @Iterations,
            PasswordAlgorithm = @Algo
        WHERE UserName = N'safety';
    ELSE
        INSERT INTO dbo.Users (UserName, FullName, Role, IsActive, PasswordHash, PasswordSalt, PasswordIterations, PasswordAlgorithm)
        VALUES (N'safety', N'Safety Officer', N'SafetyOfficer', 1, 0xe0ec3b721e83721285ca813bcd79123a71047957dc8b21892731d5c7573f07b2, 0x856e8bd358ece398c72f1003391aef5d, @Iterations, @Algo);

    -- manager
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'manager')
        UPDATE dbo.Users
        SET FullName = N'Site Manager',
            Role = N'SiteManager',
            IsActive = 1,
            PasswordHash = 0xdfd43e23d5cdc312b222b5630bff7766e04d9e7433dffb046d3e81f1d17591bb,
            PasswordSalt = 0x6ee4a469cd4e91053847f5d3fcb61dbc,
            PasswordIterations = @Iterations,
            PasswordAlgorithm = @Algo
        WHERE UserName = N'manager';
    ELSE
        INSERT INTO dbo.Users (UserName, FullName, Role, IsActive, PasswordHash, PasswordSalt, PasswordIterations, PasswordAlgorithm)
        VALUES (N'manager', N'Site Manager', N'SiteManager', 1, 0xdfd43e23d5cdc312b222b5630bff7766e04d9e7433dffb046d3e81f1d17591bb, 0x6ee4a469cd4e91053847f5d3fcb61dbc, @Iterations, @Algo);

    -- worker
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'worker')
        UPDATE dbo.Users
        SET FullName = N'Field Worker',
            Role = N'FieldWorker',
            IsActive = 1,
            PasswordHash = 0x0df9c980e7a5e4877bc070e77fed0be40f8921c343756affa3b29528dfbdcdce,
            PasswordSalt = 0x87eba76e7f3164534045ba922e7770fb,
            PasswordIterations = @Iterations,
            PasswordAlgorithm = @Algo
        WHERE UserName = N'worker';
    ELSE
        INSERT INTO dbo.Users (UserName, FullName, Role, IsActive, PasswordHash, PasswordSalt, PasswordIterations, PasswordAlgorithm)
        VALUES (N'worker', N'Field Worker', N'FieldWorker', 1, 0x0df9c980e7a5e4877bc070e77fed0be40f8921c343756affa3b29528dfbdcdce, 0x87eba76e7f3164534045ba922e7770fb, @Iterations, @Algo);

    DECLARE @SafetyId INT  = (SELECT TOP 1 UserId FROM dbo.Users WHERE UserName = N'safety');
    DECLARE @ManagerId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE UserName = N'manager');
    DECLARE @WorkerId INT  = (SELECT TOP 1 UserId FROM dbo.Users WHERE UserName = N'worker');

    -------------------------------------------------------------------------
    -- 2) Clear previous DEMO hazards only (Title LIKE 'Demo:%')
    -------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#DemoHazards') IS NOT NULL DROP TABLE #DemoHazards;
    SELECT HazardId INTO #DemoHazards
    FROM dbo.Hazards
    WHERE Title LIKE N'Demo:%';

    IF OBJECT_ID(N'dbo.HazardAttachments', N'U') IS NOT NULL
BEGIN
    DELETE a
    FROM dbo.HazardAttachments a
    INNER JOIN #DemoHazards d ON d.HazardId = a.HazardId;
END

    DELETE l
    FROM dbo.HazardLogs l
    INNER JOIN #DemoHazards d ON d.HazardId = l.HazardId;

    DELETE h
    FROM dbo.Hazards h
    INNER JOIN #DemoHazards d ON d.HazardId = h.HazardId;

    -------------------------------------------------------------------------
    -- 3) Insert demo hazards (covers Dashboard: Open/InProgress/Resolved + overdue)
    -------------------------------------------------------------------------
    DECLARE @Inserted TABLE (HazardId INT NOT NULL, Title NVARCHAR(200) NOT NULL);

    INSERT INTO dbo.Hazards (Title, Description, Severity, Type, Status, DueDate, ReportedByUserId, AssignedToUserId)
    OUTPUT inserted.HazardId, inserted.Title INTO @Inserted(HazardId, Title)
    VALUES
      (N'Demo: Overdue Open – Unlabelled chemical containers',
       N'Several containers are missing labels in the storage area.',
       N'High', N'Chemical', N'Open',        CAST('2025-12-01T00:00:00' AS DATETIME), @WorkerId,  @ManagerId),

      (N'Demo: In Progress – Missing guard rails on scaffold',
       N'Guard rails are partially missing on level 2 scaffold.',
       N'Medium', N'Physical', N'InProgress', CAST('2026-01-20T00:00:00' AS DATETIME), @WorkerId,  @WorkerId),

      (N'Demo: Resolved – Exposed electrical wiring in hallway',
       N'Exposed wiring found near the main hallway panel.',
       N'Critical', N'Electrical', N'Resolved', CAST('2025-12-15T00:00:00' AS DATETIME), @ManagerId, @ManagerId),

      (N'Demo: Open (Unassigned) – Slippery floor near entrance',
       N'Wet floor reported near the entrance, no signage.',
       N'Low', N'Physical', N'Open',         CAST('2026-02-01T00:00:00' AS DATETIME), @WorkerId,  NULL);

    -------------------------------------------------------------------------
    -- 4) Seed some logs so the "Details" view has history
    -------------------------------------------------------------------------
    INSERT INTO dbo.HazardLogs (HazardId, ActionType, Details, PerformedByUserId)
    SELECT HazardId, N'Created', N'Demo seed: hazard created', @SafetyId
    FROM @Inserted;

    -- a couple of richer log entries
    INSERT INTO dbo.HazardLogs (HazardId, ActionType, Details, PerformedByUserId)
    SELECT i.HazardId, N'Assigned', N'Demo seed: assigned by system', @SafetyId
    FROM @Inserted i
    WHERE i.Title LIKE N'Demo: Overdue Open%';

    INSERT INTO dbo.HazardLogs (HazardId, ActionType, Details, PerformedByUserId)
    SELECT i.HazardId, N'StatusChanged', N'Demo seed: moved to InProgress', @ManagerId
    FROM @Inserted i
    WHERE i.Title LIKE N'Demo: In Progress%';

    COMMIT TRAN;
    PRINT 'Seed completed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@msg, 16, 1);
END CATCH;
GO

/* End of seed.sql */
