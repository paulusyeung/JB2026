-- One-time migration: align Status with CompletedOn for existing completed jobs.
-- Sets Status = 2 for any job order that has a valid CompletedOn date
-- but hasn't been updated to the new Completed status yet.

UPDATE JobOrder
SET Status = 2
WHERE Status <> 2
  AND CompletedOn IS NOT NULL
  AND CompletedOn <> '1900-01-01';

-- Verify: count rows that will be affected
SELECT COUNT(*) AS RowsToMigrate
FROM JobOrder
WHERE Status <> 2
  AND CompletedOn IS NOT NULL
  AND CompletedOn <> '1900-01-01';
