-- Widen dbo.JobOrder.Remarks from nvarchar(512) to nvarchar(1024).
-- Required because the remarks editor (CKEditor) stores HTML content
-- that easily exceeds 512 characters.
--
-- Run against a backup/snapshot before applying to production.

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

DECLARE @CurrentMaxLen INT;

SELECT @CurrentMaxLen = CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'JobOrder'
  AND COLUMN_NAME = 'Remarks';

IF @CurrentMaxLen >= 1024
BEGIN
    PRINT 'Remarks is already nvarchar(' + CAST(@CurrentMaxLen AS VARCHAR(10)) + ') or wider. No change needed.';
    ROLLBACK TRANSACTION;
    RETURN;
END

PRINT 'Current Remarks max length: ' + CAST(@CurrentMaxLen AS VARCHAR(10));

-- Preview rows that currently exceed 512 chars (if any were somehow saved).
SELECT OrderId, LEN(Remarks) AS RemarksLen
FROM dbo.JobOrder
WHERE LEN(Remarks) > 512;

ALTER TABLE dbo.JobOrder
ALTER COLUMN Remarks nvarchar(1024) NULL;

-- Verify
SELECT CHARACTER_MAXIMUM_LENGTH AS NewMaxLen
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'JobOrder'
  AND COLUMN_NAME = 'Remarks';

COMMIT TRANSACTION;

PRINT 'dbo.JobOrder.Remarks widened to nvarchar(1024).';
