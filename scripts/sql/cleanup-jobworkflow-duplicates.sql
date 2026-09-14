-- =============================================================================
-- CLEANUP: Remove duplicate JobWorkflow rows (same OrderId + WorkIndex).
--
-- Run in SSMS against the JB5 legacy database. Do it in 3 parts:
--   PART 1   Preview only - lists exactly what WILL be deleted. No writes.
--   PART 2   The fix - repoints any JobWorkflowForms children, then deletes the
--            duplicate rows. Wrapped in an explicit transaction so you can
--            ROLLBACK. Inspect the deleted rows listed, then COMMIT.
--   PART 3   Verify - must return ZERO rows and an equal count in query 2.
--
-- Keeper rule per (OrderId, WorkIndex) group:
--   1) a row referenced by JobWorkflowForms (so forms need no repointing), else
--   2) the row with the most recent ModifiedOn.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- PART 1 - PREVIEW (read only)
-- -----------------------------------------------------------------------------

-- 1a) Confirm what is currently duplicated
SELECT OrderId, WorkIndex, COUNT(*) AS DuplicateRowCount
FROM JobWorkflow
GROUP BY OrderId, WorkIndex
HAVING COUNT(*) > 1;

-- 1b) Which of those duplicate rows will be DELETED (rn > 1), and whether
--     any of them is referenced by JobWorkflowForms (needs repointing first)
;WITH ranked AS (
    SELECT wf.JobWorkflowId,
           wf.OrderId,
           wf.WorkIndex,
           wf.WorkStatus,
           wf.ModifiedOn,
           CASE WHEN EXISTS (SELECT 1 FROM JobWorkflowForms f WHERE f.JobWorkflowId = wf.JobWorkflowId) THEN 1 ELSE 0 END AS HasFormChildren,
           ROW_NUMBER() OVER (
               PARTITION BY wf.OrderId, wf.WorkIndex
               ORDER BY CASE WHEN EXISTS (SELECT 1 FROM JobWorkflowForms f WHERE f.JobWorkflowId = wf.JobWorkflowId) THEN 0 ELSE 1 END,
                        wf.ModifiedOn DESC,
                        wf.JobWorkflowId
           ) AS rn
    FROM JobWorkflow wf
    WHERE EXISTS (
        SELECT 1 FROM JobWorkflow x
        GROUP BY x.OrderId, x.WorkIndex
        HAVING x.OrderId = wf.OrderId AND x.WorkIndex = wf.WorkIndex AND COUNT(*) > 1
    )
)
SELECT o.OrderNumber, o.OrderTitle, r.OrderId, r.WorkIndex, r.JobWorkflowId,
       r.WorkStatus, r.ModifiedOn, r.HasFormChildren,
       CASE WHEN r.rn = 1 THEN 'KEEP' ELSE 'DELETE' END AS Action
FROM ranked r
JOIN JobOrder o ON o.OrderId = r.OrderId
WHERE r.rn > 1   -- <-- set to "r.rn >= 1" to preview everything, or remove WHERE
ORDER BY r.OrderId, r.WorkIndex, r.rn;

-- -----------------------------------------------------------------------------
-- PART 2 - THE FIX (run this whole block), then COMMIT / ROLLBACK as needed
-- -----------------------------------------------------------------------------
/*
BEGIN TRAN;

;WITH ranked AS (
    SELECT wf.JobWorkflowId,
           wf.OrderId,
           wf.WorkIndex,
           ROW_NUMBER() OVER (
               PARTITION BY wf.OrderId, wf.WorkIndex
               ORDER BY CASE WHEN EXISTS (SELECT 1 FROM JobWorkflowForms f WHERE f.JobWorkflowId = wf.JobWorkflowId) THEN 0 ELSE 1 END,
                        wf.ModifiedOn DESC,
                        wf.JobWorkflowId
           ) AS rn
    FROM JobWorkflow wf
    WHERE EXISTS (
        SELECT 1 FROM JobWorkflow x
        GROUP BY x.OrderId, x.WorkIndex
        HAVING x.OrderId = wf.OrderId AND x.WorkIndex = wf.WorkIndex AND COUNT(*) > 1
    )
)
-- 2a) Re-point JobWorkflowForms children of the losing rows to the keeper
UPDATE f
SET f.JobWorkflowId = keeper.JobWorkflowId
FROM JobWorkflowForms f
JOIN ranked loser   ON loser.JobWorkflowId = f.JobWorkflowId AND loser.rn > 1
JOIN ranked keeper  ON keeper.OrderId = loser.OrderId
                   AND keeper.WorkIndex = loser.WorkIndex
                   AND keeper.rn = 1;
SELECT @@ROWCOUNT AS FormsRepointed;

-- 2b) Delete the duplicate rows (keep rn = 1)
;WITH ranked AS (
    SELECT wf.JobWorkflowId,
           wf.OrderId,
           wf.WorkIndex,
           ROW_NUMBER() OVER (
               PARTITION BY wf.OrderId, wf.WorkIndex
               ORDER BY CASE WHEN EXISTS (SELECT 1 FROM JobWorkflowForms f WHERE f.JobWorkflowId = wf.JobWorkflowId) THEN 0 ELSE 1 END,
                        wf.ModifiedOn DESC,
                        wf.JobWorkflowId
           ) AS rn
    FROM JobWorkflow wf
    WHERE EXISTS (
        SELECT 1 FROM JobWorkflow x
        GROUP BY x.OrderId, x.WorkIndex
        HAVING x.OrderId = wf.OrderId AND x.WorkIndex = wf.WorkIndex AND COUNT(*) > 1
    )
)
DELETE wf
FROM JobWorkflow wf
JOIN ranked loser ON loser.JobWorkflowId = wf.JobWorkflowId AND loser.rn > 1;
SELECT @@ROWCOUNT AS DuplicatesDeleted;

-- 2c) Review what PART 2 deleted before committing:
;WITH ranked AS (
    SELECT wf.JobWorkflowId,
           wf.OrderId,
           wf.WorkIndex,
           ROW_NUMBER() OVER (PARTITION BY wf.OrderId, wf.WorkIndex ORDER BY wf.JobWorkflowId) AS rn
    FROM JobWorkflow wf
    WHERE EXISTS (
        SELECT 1 FROM JobWorkflow x
        GROUP BY x.OrderId, x.WorkIndex
        HAVING x.OrderId = wf.OrderId AND x.WorkIndex = wf.WorkIndex AND COUNT(*) > 1
    )
)
SELECT o.OrderNumber, o.OrderTitle, r.OrderId, r.WorkIndex, r.JobWorkflowId
FROM ranked r JOIN JobOrder o ON o.OrderId = r.OrderId;

-- If everything looks right:
COMMIT;
-- If not, roll back everything with:
-- ROLLBACK;
*/

-- -----------------------------------------------------------------------------
-- PART 3 - VERIFY (read only)
-- -----------------------------------------------------------------------------

-- 3a) Must return NO rows
SELECT OrderId, WorkIndex, COUNT(*) AS DuplicateRowCount
FROM JobWorkflow
GROUP BY OrderId, WorkIndex
HAVING COUNT(*) > 1;

-- 3b) Total rows vs distinct pairs must now be equal
SELECT COUNT(*) AS TotalJobWorkflowRows,
       COUNT(DISTINCT (CONVERT(varchar(36), OrderId) + ':' + CONVERT(varchar(10), WorkIndex))) AS DistinctOrderIndexPairs
FROM JobWorkflow;

-- 3c) Confirm no JobWorkflowForms was orphaned (NULL JobWorkflowId)
SELECT COUNT(*) AS OrphanedFormRows
FROM JobWorkflowForms f
WHERE NOT EXISTS (SELECT 1 FROM JobWorkflow wf WHERE wf.JobWorkflowId = f.JobWorkflowId);