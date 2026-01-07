USE DatwiseSafetyDemo;
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

