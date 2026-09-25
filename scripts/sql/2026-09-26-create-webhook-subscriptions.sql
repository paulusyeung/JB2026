-- =============================================================================
-- Create dbo.WebhookSubscriptions.
--
-- Why: JB2026.EfCore/Models/WebhookSubscription.cs is mapped and queried by
-- the webhook dispatchers, but the table was never part of the legacy JB5
-- schema (it is absent from JB2015/JB5.EF6/JB5Model.edmx and from both
-- JB2026 and Job.Book.Marche). The open spec for
-- restore-legacy-job-lifecycle-push-history listed "schema changes" as a
-- non-goal, so the table was assumed to exist and never created.
--
-- Impact while missing: SynchronousWebhookDispatcher.cs queries the table
-- outside its try/catch, so every job lifecycle event (created, scheduled,
-- completed, invoiced, cogs) threw "Invalid object name
-- 'dbo.WebhookSubscriptions'" AFTER the business write had already been
-- committed. The API returned 500 and ScheduleView showed "Unable to save
-- schedule. Please try again." even though the schedule reorder had in fact
-- been saved.
--
-- This script is idempotent: safe to run more than once.
-- Run against a backup/snapshot before applying to production.
-- =============================================================================

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF OBJECT_ID('dbo.WebhookSubscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebhookSubscriptions
    (
        Id         INT            IDENTITY(1,1) NOT NULL,
        Url        NVARCHAR(2048) NOT NULL,
        EventTypes NVARCHAR(1000) NOT NULL,
        IsActive   BIT            NOT NULL CONSTRAINT DF_WebhookSubscriptions_IsActive DEFAULT (1),
        CreatedAt  DATETIME       NOT NULL CONSTRAINT DF_WebhookSubscriptions_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt  DATETIME       NULL,
        CONSTRAINT PK_WebhookSubscriptions PRIMARY KEY CLUSTERED (Id)
    );

    PRINT 'Created dbo.WebhookSubscriptions.';
END
ELSE
BEGIN
    PRINT 'dbo.WebhookSubscriptions already exists. No change needed.';
END

GO

-- -----------------------------------------------------------------------------
-- Index: matches IX_WebhookSubscriptions_IsActive in JB5LegacyContext, which
-- backs the "WHERE IsActive = 1" filter used by both dispatchers.
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_WebhookSubscriptions_IsActive'
      AND object_id = OBJECT_ID('dbo.WebhookSubscriptions')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_WebhookSubscriptions_IsActive
        ON dbo.WebhookSubscriptions (IsActive);
END
GO

-- -----------------------------------------------------------------------------
-- Verify
-- -----------------------------------------------------------------------------
SELECT t.name AS TableName,
       c.name AS ColumnName,
       ty.name AS DataType,
       c.max_length AS MaxLengthBytes,
       c.is_nullable AS IsNullable,
       c.is_identity AS IsIdentity
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types   ty ON ty.user_type_id = c.user_type_id
WHERE t.name = 'WebhookSubscriptions'
ORDER BY c.column_id;

SELECT i.name AS IndexName, i.is_unique AS IsUnique, c.name AS ColumnName
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns        c  ON c.object_id  = ic.object_id AND c.column_id  = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.WebhookSubscriptions')
ORDER BY i.index_id, ic.key_ordinal;
GO

-- -----------------------------------------------------------------------------
-- Optional: enable the lifecycle events for a subscriber. No rows are seeded
-- by this script -- with an empty table the dispatchers no-op by design
-- (no webhook traffic), and the lifecycle push/history rows keep working.
--
-- INSERT INTO dbo.WebhookSubscriptions (Url, EventTypes)
-- VALUES (N'https://example.com/hook',
--         N'OnJobCreated,OnJobScheduled,OnJobCompleted,OnJobInvoiced,OnJobCogsFilled,OnReadyPlate,OnReadyPaper');
-- -----------------------------------------------------------------------------
