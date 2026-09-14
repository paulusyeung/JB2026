-- -----------------------------------------------------------------------------
-- Verify duplicate JobWorkflow rows: same (OrderId, WorkIndex) appearing more
-- than once. Caused by the upsert logic (commit 721d607) inserting new rows
-- instead of updating, with no unique index on (OrderId, WorkIndex).
--
-- Run all three sections below in SSMS against the JB5 legacy database.
-- -----------------------------------------------------------------------------

-- 1) Summary: which (OrderId, WorkIndex) pairs have duplicates, and how many
SELECT wf.OrderId,
       wf.WorkIndex,
       COUNT(*)                    AS DuplicateRowCount,
       MIN(JB2026_dupe.CreatedOn)  AS FirstSeenOn,
       MAX(JB2026_dupe.CreatedOn)  AS LastSeenOn
FROM JobWorkflow wf
CROSS APPLY (SELECT MIN(o.CreatedOn) AS CreatedOn
             FROM JobWorkflow jf
             JOIN JobOrder     o   ON o.OrderId = jf.OrderId
             WHERE jf.OrderId = wf.OrderId AND jf.WorkIndex = wf.WorkIndex) AS JB2026_dupe
GROUP BY wf.OrderId, wf.WorkIndex
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, wf.OrderId, wf.WorkIndex;

-- 2) Detail: every duplicate row with order reference so you can eyeball the
--    different JobWorkflowId values sharing the same OrderId + WorkIndex
;WITH DuplicateIndexes AS (
    SELECT OrderId, WorkIndex
    FROM JobWorkflow
    GROUP BY OrderId, WorkIndex
    HAVING COUNT(*) > 1
)
SELECT jf.JobWorkflowId,
       jf.OrderId,
       o.OrderNumber,
       o.OrderTitle,
       jf.WorkIndex,
       jf.WorkStatus,
       jf.WorkTitle,
       jf.WorkNotes,
       jf.ModifiedOn,
       jf.ModifiedBy
FROM JobWorkflow jf
JOIN JobOrder o ON o.OrderId = jf.OrderId
JOIN DuplicateIndexes d
     ON d.OrderId = jf.OrderId AND d.WorkIndex = jf.WorkIndex
ORDER BY jf.OrderId, jf.WorkIndex, jf.ModifiedOn;

-- 3) Quick sanity check: total row count vs distinct logical pairs, and
--    whether a unique index on (OrderId, WorkIndex) is already missing
SELECT COUNT(*)                                          AS TotalJobWorkflowRows,
       COUNT(DISTINCT (CONVERT(varchar(36), OrderId) + ':' + CONVERT(varchar(10), WorkIndex))) AS DistinctOrderIndexPairs
FROM JobWorkflow;

SELECT i.name AS IndexName, c.name AS ColumnName
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns        c  ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.JobWorkflow')
ORDER BY i.index_id, ic.key_ordinal;