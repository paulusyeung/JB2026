-- Migrate dbo.JobOrder.OrderType to the new order type mapping:
--   0 = Offset Print  (unchanged)
--   1 = Digital Print (migrates to 3)
--   2 = Woven Label   (unchanged)
--   3 = Others        (receives old 1)
--
-- Affected rows: OrderType = 1 -> 3
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
WHERE OrderType = 1;

-- Inspect before proceeding.
SELECT OrderType, COUNT(*) AS RowsAffected
FROM #JobOrder_OrderType_Backup
GROUP BY OrderType;

DECLARE @Migrated INT = (SELECT COUNT(*) FROM #JobOrder_OrderType_Backup);

UPDATE dbo.JobOrder
SET OrderType = 3
WHERE OrderType = 1;

-- Validate: no rows may remain with the retired value.
IF EXISTS (SELECT 1 FROM dbo.JobOrder WHERE OrderType = 1)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Migration aborted: retired OrderType value 1 still present after update.', 16, 1);
    RETURN;
END;

SELECT
    @Migrated                              AS MigratedRows,
    (SELECT COUNT(*) FROM dbo.JobOrder)    AS TotalRows,
    (SELECT COUNT(*) FROM dbo.JobOrder
     WHERE OrderType = 3)                  AS NowOrderType3,
    (SELECT COUNT(*) FROM dbo.JobOrder
     WHERE OrderType = 0)                  AS NowOrderType0,
    (SELECT COUNT(*) FROM dbo.JobOrder
     WHERE OrderType = 2)                  AS NowOrderType2;

COMMIT TRANSACTION;

PRINT 'Migrated ' + CAST(@Migrated AS VARCHAR(20)) + ' JobOrder row(s) from OrderType 1 to 3.';
