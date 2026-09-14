-- =============================================================================
-- HARDENING: enforce one JobWorkflow row per (OrderId, WorkIndex).
--
-- Run ONLY after cleanup-jobworkflow-duplicates.sql has completed and the
-- verification query returns zero duplicate pairs.
--
-- Failure modes:
--   * Msg 1505 / "duplicate key"  -> duplicates still exist; re-run the
--     verification query in cleanup-jobworkflow-duplicates.sql (PART 3).
--   * Exception from the API      -> update the upsert logic in
--     SaveBatch / UpdatePendingWorkflow (code fix, step 3) so a concurrent
--     check-then-insert race retries as an update instead of throwing.
-- =============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_JobWorkflow_OrderId_WorkIndex'
      AND object_id = OBJECT_ID('dbo.JobWorkflow')
)
BEGIN
    CREATE UNIQUE INDEX IX_JobWorkflow_OrderId_WorkIndex
        ON dbo.JobWorkflow (OrderId, WorkIndex);
END;

-- Verify the index exists:
SELECT i.name, i.is_unique, c.name AS ColumnName, ic.key_ordinal
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns        c  ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.JobWorkflow')
ORDER BY i.index_id, ic.key_ordinal;