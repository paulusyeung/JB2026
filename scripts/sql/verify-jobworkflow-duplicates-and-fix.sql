-- 1. Find all duplicate (OrderId, WorkIndex) combinations
SELECT   OrderId,
         WorkIndex,
         COUNT(*) AS DuplicateCount
FROM     [dbo].[JobWorkflow]
WHERE    WorkIndex >= 0 AND WorkIndex <= 2
GROUP BY OrderId, WorkIndex
HAVING   COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- 2. Show the full duplicate rows (for manual inspection / cleanup)
SELECT  w.*
FROM    [dbo].[JobWorkflow] w
INNER JOIN (
    SELECT   OrderId, WorkIndex
    FROM     [dbo].[JobWorkflow]
    WHERE    WorkIndex >= 0 AND WorkIndex <= 2
    GROUP BY OrderId, WorkIndex
    HAVING   COUNT(*) > 1
) dup ON w.OrderId = dup.OrderId AND w.WorkIndex = dup.WorkIndex
ORDER BY w.OrderId, w.WorkIndex;

-- 3. Summary: how many orders are affected
SELECT   COUNT(DISTINCT OrderId) AS AffectedOrderCount,
         SUM(cnt)               AS TotalExcessRows
FROM (
    SELECT   OrderId, WorkIndex, COUNT(*) - 1 AS cnt
    FROM     [dbo].[JobWorkflow]
    WHERE    WorkIndex >= 0 AND WorkIndex <= 2
    GROUP BY OrderId, WorkIndex
    HAVING   COUNT(*) > 1
) sub;

-- 4. (Optional) Deduplicate — keep only the row with the latest ModifiedOn per (OrderId, WorkIndex)
--    REVIEW this carefully before running. It deletes the stale duplicates.
;WITH ranked AS (
    SELECT *,
           ROW_NUMBER() OVER (
               PARTITION BY OrderId, WorkIndex
               ORDER BY ModifiedOn DESC
           ) AS rn
    FROM [dbo].[JobWorkflow]
    WHERE WorkIndex >= 0 AND WorkIndex <= 2
)
DELETE FROM ranked WHERE rn > 1;
