-- Migrate dbo.JobOrder.OrderType to the new order type mapping:
--   0 = Printing (unchanged)
--   1 = Digital Printing (empty after migration)
--   2 = Others (new home for old 1, 2, 3)
--
-- Affected rows: OrderType IN (1, 2, 3) -> 2
-- Run against a backup/snapshot before applying to production.
-- dbo.Z_OrderTypeWorkflow is intentionally left untouched.

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

-- Reversible safety net: snapshot the rows we are about to change.
IF OBJECT_ID('tempdb..#JobOrder_OrderType_Backup', 'U') IS NOT NULL
    DROP TABLE #JobOrder_OrderType_Backup;

SELECT OrderId, OrderType, GETUTCDATE() AS MigratedUtc
INTO #JobOrder_OrderType_Backup
FROM dbo.JobOrder
WHERE OrderType IN (1, 2, 3);

-- Inspect before proceeding.
SELECT OrderType, COUNT(*) AS RowsAffected
FROM #JobOrder_OrderType_Backup
GROUP BY OrderType
ORDER BY OrderType;

DECLARE @Migrated INT = (SELECT COUNT(*) FROM #JobOrder_OrderType_Backup);

UPDATE dbo.JobOrder
SET OrderType = 2
WHERE OrderType IN (1, 2, 3);

-- Validate: no rows may remain with the retired values.
IF EXISTS (SELECT 1 FROM dbo.JobOrder WHERE OrderType IN (1, 3))
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Migration aborted: retired OrderType values still present after update.', 16, 1);
    RETURN;
END;

SELECT
    @Migrated                              AS MigratedRows,
    (SELECT COUNT(*) FROM dbo.JobOrder)    AS TotalRows,
    (SELECT COUNT(*) FROM dbo.JobOrder
     WHERE OrderType = 2)                  AS NowOrderType2,
    (SELECT COUNT(*) FROM dbo.JobOrder
     WHERE OrderType = 0)                  AS NowOrderType0;

COMMIT TRANSACTION;

PRINT 'Migrated ' + CAST(@Migrated AS VARCHAR(20)) + ' JobOrder row(s) to OrderType = 2.';
