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

/* =========================================================
   Core tables (idempotent)
   ========================================================= */

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        UserName NVARCHAR(50) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),

        PasswordHash VARBINARY(64) NOT NULL,
        PasswordSalt VARBINARY(32) NOT NULL,
        PasswordIterations INT NOT NULL,
        PasswordAlgorithm NVARCHAR(50) NOT NULL,

        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT(GETUTCDATE()),
        UpdatedAt DATETIME NULL
    );

    CREATE UNIQUE INDEX UX_Users_UserName ON dbo.Users(UserName);
END
GO

IF OBJECT_ID(N'dbo.Hazards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Hazards
    (
        HazardId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Hazards PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(2000) NULL,

        ReportedByUserId INT NOT NULL,
        AssignedToUserId INT NULL,

        Status NVARCHAR(30) NOT NULL,
        Severity NVARCHAR(30) NOT NULL,
        Type NVARCHAR(30) NOT NULL,

        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Hazards_CreatedAt DEFAULT(GETUTCDATE()),
        DueDate DATETIME NULL,

        CONSTRAINT FK_Hazards_ReportedBy FOREIGN KEY (ReportedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Hazards_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(UserId)
    );

    CREATE INDEX IX_Hazards_Status ON dbo.Hazards(Status);
    CREATE INDEX IX_Hazards_AssignedToUserId ON dbo.Hazards(AssignedToUserId);
    CREATE INDEX IX_Hazards_ReportedByUserId ON dbo.Hazards(ReportedByUserId);
    CREATE INDEX IX_Hazards_DueDate ON dbo.Hazards(DueDate);
END
GO

IF OBJECT_ID(N'dbo.HazardLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HazardLogs
    (
        HazardLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HazardLogs PRIMARY KEY,
        HazardId INT NOT NULL,
        ActionType NVARCHAR(50) NOT NULL,
        Details NVARCHAR(2000) NULL,
        PerformedByUserId INT NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_HazardLogs_CreatedAt DEFAULT(GETUTCDATE()),

        CONSTRAINT FK_HazardLogs_Hazard FOREIGN KEY (HazardId) REFERENCES dbo.Hazards(HazardId),
        CONSTRAINT FK_HazardLogs_User FOREIGN KEY (PerformedByUserId) REFERENCES dbo.Users(UserId)
    );

    CREATE INDEX IX_HazardLogs_HazardId_CreatedAt ON dbo.HazardLogs(HazardId, CreatedAt DESC);
END
GO

IF OBJECT_ID(N'dbo.HazardAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HazardAttachments
    (
        AttachmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HazardAttachments PRIMARY KEY,
        HazardId INT NOT NULL,

        FileName NVARCHAR(255) NOT NULL,
        ContentType NVARCHAR(100) NULL,
        FileSizeBytes INT NULL,
        StoragePath NVARCHAR(500) NULL,

        UploadedByUserId INT NULL,
        UploadedAt DATETIME NOT NULL CONSTRAINT DF_HazardAttachments_UploadedAt DEFAULT(GETUTCDATE()),

        CONSTRAINT FK_HazardAttachments_Hazard FOREIGN KEY (HazardId) REFERENCES dbo.Hazards(HazardId),
        CONSTRAINT FK_HazardAttachments_User FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users(UserId)
    );

    CREATE INDEX IX_HazardAttachments_HazardId ON dbo.HazardAttachments(HazardId);
END
GO


/* =========================================================
   Helper function: Hazard accessibility (used by procs)
   ========================================================= */
CREATE OR ALTER FUNCTION dbo.fn_IsAccessibleHazard
(
    @HazardId INT,
    @UserId INT,
    @UserRole NVARCHAR(50)
)
RETURNS BIT
AS
BEGIN
    DECLARE @role NVARCHAR(50) = LTRIM(RTRIM(ISNULL(@UserRole, N'')));
    DECLARE @result BIT = 0;

    IF @role = N'SafetyOfficer'
    BEGIN
        SET @result = 1;
        RETURN @result;
    END

    DECLARE @reportedBy INT = NULL;
    DECLARE @assignedTo INT = NULL;
    DECLARE @status NVARCHAR(30) = NULL;

    SELECT
        @reportedBy = ReportedByUserId,
        @assignedTo = AssignedToUserId,
        @status = Status
    FROM dbo.Hazards
    WHERE HazardId = @HazardId;

    IF @reportedBy IS NULL
    BEGIN
        RETURN @result;
    END

    IF @role = N'FieldWorker'
    BEGIN
        IF (@reportedBy = @UserId OR @assignedTo = @UserId)
            SET @result = 1;

        RETURN @result;
    END

    IF @role = N'SiteManager'
    BEGIN
        -- Can view: hazards assigned to self, reported by self, or unassigned Open hazards
        IF (@assignedTo = @UserId OR @reportedBy = @UserId OR (@status = N'Open' AND @assignedTo IS NULL))
            SET @result = 1;

        RETURN @result;
    END

    RETURN @result;
END
GO

/* =========================================================
   Users procs (required by C# repositories)
   ========================================================= */
CREATE OR ALTER PROCEDURE dbo.usp_GetUserAuthByUserName
    @UserName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        UserId,
        UserName,
        FullName,
        Role,
        IsActive,
        PasswordHash,
        PasswordSalt,
        PasswordIterations,
        PasswordAlgorithm
    FROM dbo.Users
    WHERE UserName = @UserName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetUsersByRole
    @Role NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UserId,
        UserName,
        FullName,
        Role
    FROM dbo.Users
    WHERE IsActive = 1 AND Role = @Role
    ORDER BY FullName;
END
GO

/* =========================================================
   Hazard logs proc (server-side RBAC)
   ========================================================= */
CREATE OR ALTER PROCEDURE dbo.usp_GetHazardLogs
    @HazardId INT,
    @CurrentUserId INT,
    @CurrentUserRole NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role NVARCHAR(50) = LTRIM(RTRIM(ISNULL(@CurrentUserRole, N'')));

    IF dbo.fn_IsAccessibleHazard(@HazardId, @CurrentUserId, @role) = 0
    BEGIN
        -- Return no rows (schema-compatible)
        SELECT TOP 0
            CAST(NULL AS INT) AS HazardLogId,
            CAST(NULL AS INT) AS HazardId,
            CAST(NULL AS NVARCHAR(30)) AS ActionType,
            CAST(NULL AS NVARCHAR(2000)) AS Details,
            CAST(NULL AS INT) AS PerformedByUserId,
            CAST(NULL AS NVARCHAR(200)) AS PerformedByName,
            CAST(NULL AS DATETIME) AS CreatedAt;
        RETURN;
    END

    SELECT
        l.HazardLogId,
        l.HazardId,
        l.ActionType,
        l.Details,
        l.PerformedByUserId,
        u.FullName AS PerformedByName,
        l.CreatedAt
    FROM dbo.HazardLogs l
    INNER JOIN dbo.Users u ON u.UserId = l.PerformedByUserId
    WHERE l.HazardId = @HazardId
    ORDER BY l.CreatedAt DESC, l.HazardLogId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AddHazardLog
    @HazardId INT,
    @ActionType NVARCHAR(50),
    @Details NVARCHAR(2000) = NULL,
    @PerformedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role NVARCHAR(50) =
        (SELECT TOP 1 LTRIM(RTRIM(Role))
         FROM dbo.Users
         WHERE UserId = @PerformedByUserId AND IsActive = 1);

    IF @role IS NULL
    BEGIN
        RAISERROR('Unauthorized', 16, 1);
        RETURN;
    END

    DECLARE @action NVARCHAR(50) = LTRIM(RTRIM(ISNULL(@ActionType, N'')));

    -- This proc supports comments only (prevents accidental status changes)
    IF @action <> N'Comment'
    BEGIN
        RAISERROR('BadRequest', 16, 1);
        RETURN;
    END

    IF @Details IS NULL OR LTRIM(RTRIM(@Details)) = N''
    BEGIN
        RAISERROR('BadRequest', 16, 1);
        RETURN;
    END

    DECLARE @reportedBy INT, @assignedTo INT, @status NVARCHAR(30);

    SELECT
        @reportedBy = ReportedByUserId,
        @assignedTo = AssignedToUserId,
        @status = Status
    FROM dbo.Hazards
    WHERE HazardId = @HazardId;

    IF @reportedBy IS NULL
    BEGIN
        RAISERROR('NotFound', 16, 1);
        RETURN;
    END

    -- Permission check (aligned with HazardAuthorization.CanView)
    IF @role = N'FieldWorker'
    BEGIN
        IF NOT (@reportedBy = @PerformedByUserId OR @assignedTo = @PerformedByUserId)
        BEGIN
            RAISERROR('Forbidden', 16, 1);
            RETURN;
        END
    END

    IF @role = N'SiteManager'
    BEGIN
        IF NOT (
            @assignedTo = @PerformedByUserId
            OR @reportedBy = @PerformedByUserId
            OR (@status = N'Open' AND @assignedTo IS NULL)
        )
        BEGIN
            RAISERROR('Forbidden', 16, 1);
            RETURN;
        END
    END

    IF @role NOT IN (N'SafetyOfficer', N'FieldWorker', N'SiteManager')
    BEGIN
        RAISERROR('Forbidden', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.HazardLogs (HazardId, ActionType, Details, PerformedByUserId)
    VALUES (@HazardId, N'Comment', @Details, @PerformedByUserId);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CreateHazard
    @Title NVARCHAR(200),
    @Description NVARCHAR(2000),
    @ReportedByUserId INT,
    @Severity NVARCHAR(30),
    @Type NVARCHAR(30),
    @DueDate DATETIME = NULL,
    @PerformedByUserId INT,
    @HazardId INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role NVARCHAR(50) =
        (SELECT TOP 1 LTRIM(RTRIM(Role))
         FROM dbo.Users
         WHERE UserId = @PerformedByUserId AND IsActive = 1);

    IF @role IS NULL
    BEGIN
        RAISERROR('Unauthorized', 16, 1);
        RETURN;
    END

    -- Reporter must exist
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @ReportedByUserId AND IsActive = 1)
    BEGIN
        RAISERROR('BadRequest', 16, 1);
        RETURN;
    END

    -- FieldWorker can only report for self
    IF (@role = 'FieldWorker' AND @ReportedByUserId <> @PerformedByUserId)
    BEGIN
        RAISERROR('Forbidden', 16, 1);
        RETURN;
    END

    -- Only these roles can create
    IF @role NOT IN ('SafetyOfficer', 'SiteManager', 'FieldWorker')
    BEGIN
        RAISERROR('Forbidden', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Hazards
    (
        Title,
        Description,
        ReportedByUserId,
        AssignedToUserId,
        Status,
        Severity,
        Type,
        DueDate
    )
    VALUES
    (
        @Title,
        @Description,
        @ReportedByUserId,
        NULL,
        'Open',
        @Severity,
        @Type,
        @DueDate
    );

    SET @HazardId = SCOPE_IDENTITY();

    INSERT INTO dbo.HazardLogs (HazardId, ActionType, Details, PerformedByUserId)
    VALUES (@HazardId, 'Created', '', @PerformedByUserId);

    -- keep seed compatibility (INSERT ... EXEC)
    SELECT @HazardId AS HazardId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_UpdateHazard
    @HazardId INT,
    @Title NVARCHAR(200),
    @Description NVARCHAR(2000),
    @Severity NVARCHAR(30),
    @Type NVARCHAR(30),
    @DueDate DATETIME = NULL,
    @PerformedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role NVARCHAR(50) =
        (SELECT TOP 1 LTRIM(RTRIM(Role))
         FROM dbo.Users
         WHERE UserId = @PerformedByUserId AND IsActive = 1);

    IF @role IS NULL
    BEGIN
        RAISERROR('Unauthorized', 16, 1);
        RETURN;
    END

    -- Load hazard state
    DECLARE @reportedBy INT, @assignedTo INT, @status NVARCHAR(30);
    SELECT @reportedBy = ReportedByUserId, @assignedTo = AssignedToUserId, @status = Status
    FROM dbo.Hazards WHERE HazardId = @HazardId;

    IF @reportedBy IS NULL
    BEGIN
        RAISERROR('NotFound', 16, 1);
        RETURN;
    END

    -- See comment in README: SafetyOfficer can update any; SiteManager limited; FieldWorker limited
    IF @role NOT IN ('SafetyOfficer', 'SiteManager', 'FieldWorker')
    BEGIN
        RAISERROR('Forbidden', 16, 1);
        RETURN;
    END

    IF (@role = 'SiteManager' AND NOT (@assignedTo = @PerformedByUserId OR (@assignedTo IS NULL AND @status = 'Open')))
    BEGIN
        RAISERROR('Forbidden', 16, 1);
        RETURN;
    END

    IF (@role = 'FieldWorker' AND NOT (@reportedBy = @PerformedByUserId AND @assignedTo IS NULL AND @status = 'Open'))
    BEGIN
        RAISERROR('Forbidden', 16, 1);
        RETURN;
    END

    UPDATE dbo.Hazards
    SET
        Title = @Title,
        Description = @Description,
        Severity = @Severity,
        Type = @Type,
        DueDate = @DueDate
    WHERE HazardId = @HazardId;

    INSERT INTO dbo.HazardLogs (HazardId, ActionType, Details, PerformedByUserId)
    VALUES (@HazardId, 'UpdatedDetails', '', @PerformedByUserId);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AssignHazard
    @HazardId INT,
    @AssignedToUserId INT = NULL,
    @PerformedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role NVARCHAR(50) =
        (SELECT TOP 1 LTRIM(RTRIM(Role))
         FROM dbo.Users
         WHERE UserId = @PerformedByUserId AND IsActive = 1);

    IF @role IS NULL
    BEGIN
        RAISERROR('Unauthorized', 16, 1);
        RETURN;
    END

    DECLARE @status NVARCHAR(30), @assignedTo INT;
    SELECT @status = Status, @assignedTo = AssignedToUserId
    FROM dbo.Hazards WHERE HazardId = @HazardId;

    IF @status IS NULL
    BEGIN
        RAISERROR('NotFound', 16, 1);
        RETURN;
    END

    -- Authorization
    IF @role NOT IN ('SafetyOfficer', 'SiteManager')
    BEGIN
        RAISERROR('Forbidden', 16, 1);
        RETURN;
    END

    IF (@role = 'SafetyOfficer')
    BEGIN
        -- Can assign to any active SiteManager (or NULL to unassign)
        IF @AssignedToUserId IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @AssignedToUserId AND IsActive = 1 AND Role = 'SiteManager')
            BEGIN
                RAISERROR('Forbidden', 16, 1);
                RETURN;
            END
        END
    END
    ELSE
    BEGIN
        -- SiteManager: self-assign only, and only if open + unassigned
        IF @AssignedToUserId <> @PerformedByUserId
        BEGIN
            RAISERROR('Forbidden', 16, 1);
            RETURN;
        END

        IF NOT (@status = 'Open' AND @assignedTo IS NULL)
        BEGIN
            RAISERROR('Forbidden', 16, 1);
            RETURN;
        END
    END

    UPDATE dbo.Hazards
    SET AssignedToUserId = @AssignedToUserId
    WHERE HazardId = @HazardId;

    INSERT INTO dbo.HazardLogs (HazardId, ActionType, Details, PerformedByUserId)
    VALUES (@HazardId, 'Assigned', '', @PerformedByUserId);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_ChangeHazardStatus
    @HazardId INT,
    @NewStatus NVARCHAR(30) = NULL,
    @Details NVARCHAR(2000) = NULL,
    @PerformedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role NVARCHAR(50) =
        (SELECT TOP 1 LTRIM(RTRIM(Role))
         FROM dbo.Users
         WHERE UserId = @PerformedByUserId AND IsActive = 1);

    IF @role IS NULL
    BEGIN
        RAISERROR('Unauthorized', 16, 1);
        RETURN;
    END


    -- Comment-only flow: allow adding a comment without changing status.
    IF @NewStatus IS NULL OR LTRIM(RTRIM(@NewStatus)) = ''
    BEGIN
        IF @Details IS NULL OR LTRIM(RTRIM(@Details)) = ''
        BEGIN
            RAISERROR('BadRequest', 16, 1);
            RETURN;
        END

        EXEC dbo.usp_AddHazardLog
            @HazardId = @HazardId,
            @ActionType = 'Comment',
            @Details = @Details,
            @PerformedByUserId = @PerformedByUserId;

        RETURN;
    END

    IF @NewStatus IS NULL OR LTRIM(RTRIM(@NewStatus)) = ''
    BEGIN
        RAISERROR('BadRequest', 16, 1);
        RETURN;
    END

    DECLARE @ns NVARCHAR(30) = LTRIM(RTRIM(@NewStatus));
    IF @ns IS NULL OR @ns = '' OR @ns NOT IN ('Open','InProgress','Resolved')
    BEGIN
        RAISERROR('BadRequest', 16, 1);
        RETURN;
    END

    DECLARE @old NVARCHAR(30), @assignedTo INT;
    SELECT @old = Status, @assignedTo = AssignedToUserId
    FROM dbo.Hazards WHERE HazardId = @HazardId;

    IF @old IS NULL
    BEGIN
        RAISERROR('NotFound', 16, 1);
        RETURN;
    END

    -- Role-based rules:
    -- SafetyOfficer: any transition.
    -- SiteManager: must be assigned to self and can only progress Open->InProgress->Resolved.
    -- Others: forbidden.

    IF @role <> 'SafetyOfficer'
    BEGIN
        IF @role <> 'SiteManager'
        BEGIN
            RAISERROR('Forbidden', 16, 1);
            RETURN;
        END

        IF @assignedTo <> @PerformedByUserId
        BEGIN
            RAISERROR('Forbidden', 16, 1);
            RETURN;
        END

        IF NOT (
            (@old = 'Open' AND @ns = 'InProgress')
            OR (@old = 'InProgress' AND @ns = 'Resolved')
        )
        BEGIN
            RAISERROR('Forbidden', 16, 1);
            RETURN;
        END
    END

    UPDATE dbo.Hazards
    SET Status = @ns
    WHERE HazardId = @HazardId;

    DECLARE @logDetails NVARCHAR(2000) =
        CONCAT(@old, ' -> ', @ns,
               CASE WHEN @Details IS NULL OR @Details = '' THEN '' ELSE ' | ' + @Details END);

    INSERT INTO dbo.HazardLogs (HazardId, ActionType, Details, PerformedByUserId)
    VALUES (@HazardId, 'StatusChanged', @logDetails, @PerformedByUserId);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetHazards
    @CurrentUserId INT,
    @CurrentUserRole NVARCHAR(50),
    @SearchText NVARCHAR(200) = NULL,
    @Status NVARCHAR(30) = NULL,
    @Severity NVARCHAR(30) = NULL,
    @Type NVARCHAR(30) = NULL,
    @AssignedToUserId INT = NULL,
    @OverdueOnly BIT = 0,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now DATETIME = GETUTCDATE();
    DECLARE @role NVARCHAR(50) = LTRIM(RTRIM(ISNULL(@CurrentUserRole, N'')));

    SELECT
        h.HazardId,
        h.Title,
        h.Description,
        h.ReportedByUserId,
        h.AssignedToUserId,
        h.Status,
        h.Severity,
        h.Type,
        h.CreatedAt,
        h.DueDate,
        rb.FullName AS ReportedByName,
        ab.FullName AS AssignedToName
    FROM dbo.Hazards h
    INNER JOIN dbo.Users rb ON rb.UserId = h.ReportedByUserId
    LEFT JOIN dbo.Users ab ON ab.UserId = h.AssignedToUserId
    WHERE
        (
            @role = 'SafetyOfficer'
            OR (@role = 'FieldWorker' AND (h.ReportedByUserId = @CurrentUserId OR h.AssignedToUserId = @CurrentUserId))
            OR (@role = 'SiteManager' AND (
                    h.AssignedToUserId = @CurrentUserId
                    OR h.ReportedByUserId = @CurrentUserId
                    OR (h.AssignedToUserId IS NULL AND h.Status = 'Open')
                ))
        )
        AND (@SearchText IS NULL OR @SearchText = '' OR h.Title LIKE '%' + @SearchText + '%' OR h.Description LIKE '%' + @SearchText + '%')
        AND (@Status IS NULL OR @Status = '' OR h.Status = @Status)
        AND (@Severity IS NULL OR @Severity = '' OR h.Severity = @Severity)
        AND (@Type IS NULL OR @Type = '' OR h.Type = @Type)
        AND (@AssignedToUserId IS NULL OR h.AssignedToUserId = @AssignedToUserId)
        AND (@OverdueOnly = 0 OR (h.DueDate IS NOT NULL AND h.DueDate < @now AND h.Status <> 'Resolved'))
        AND (@FromDate IS NULL OR h.CreatedAt >= @FromDate)
        AND (@ToDate IS NULL OR h.CreatedAt <= @ToDate)
    ORDER BY h.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetHazardById
    @HazardId INT,
    @CurrentUserId INT,
    @CurrentUserRole NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @role NVARCHAR(50) = LTRIM(RTRIM(ISNULL(@CurrentUserRole, N'')));

    IF dbo.fn_IsAccessibleHazard(@HazardId, @CurrentUserId, @role) = 0
    BEGIN
        -- Return no rows (avoid leaking existence)
        SELECT TOP 0 * FROM dbo.Hazards;
        RETURN;
    END

    SELECT
        h.HazardId,
        h.Title,
        h.Description,
        h.ReportedByUserId,
        h.AssignedToUserId,
        h.Status,
        h.Severity,
        h.Type,
        h.CreatedAt,
        h.DueDate,
        rb.FullName AS ReportedByName,
        ab.FullName AS AssignedToName
    FROM dbo.Hazards h
    INNER JOIN dbo.Users rb ON rb.UserId = h.ReportedByUserId
    LEFT JOIN dbo.Users ab ON ab.UserId = h.AssignedToUserId
    WHERE h.HazardId = @HazardId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetDashboardMetrics
    @CurrentUserId INT,
    @CurrentUserRole NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now DATETIME = GETUTCDATE();
    DECLARE @role NVARCHAR(50) = LTRIM(RTRIM(ISNULL(@CurrentUserRole, N'')));

    IF OBJECT_ID('tempdb..#scoped') IS NOT NULL DROP TABLE #scoped;

    SELECT *
    INTO #scoped
    FROM dbo.Hazards h
    WHERE
        (@role = 'SafetyOfficer'
         OR (@role = 'FieldWorker' AND (h.ReportedByUserId = @CurrentUserId OR h.AssignedToUserId = @CurrentUserId))
         OR (@role = 'SiteManager' AND (
                h.AssignedToUserId = @CurrentUserId
                OR h.ReportedByUserId = @CurrentUserId
                OR (h.AssignedToUserId IS NULL AND h.Status = 'Open')
            )))
    ;

    -- Result set 1: totals
    SELECT
        COUNT(1) AS Total,
        SUM(CASE WHEN Status = 'Open' THEN 1 ELSE 0 END) AS OpenCount,
        SUM(CASE WHEN Status = 'InProgress' THEN 1 ELSE 0 END) AS InProgressCount,
        SUM(CASE WHEN Status = 'Resolved' THEN 1 ELSE 0 END) AS ResolvedCount,
        SUM(CASE WHEN DueDate IS NOT NULL AND DueDate < @now AND Status <> 'Resolved' THEN 1 ELSE 0 END) AS OverdueCount
    FROM #scoped;

    -- Result set 2: by severity
    SELECT Severity AS [Key], COUNT(1) AS [Count]
    FROM #scoped
    GROUP BY Severity
    ORDER BY COUNT(1) DESC;

    -- Result set 3: by type
    SELECT [Type] AS [Key], COUNT(1) AS [Count]
    FROM #scoped
    GROUP BY [Type]
    ORDER BY COUNT(1) DESC;
END

GO
