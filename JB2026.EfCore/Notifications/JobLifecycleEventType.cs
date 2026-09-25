namespace JB2026.EfCore.Notifications;

/// <summary>
/// Job and stock lifecycle events that produce legacy-format FCMHistory
/// records and webhook dispatches.
/// </summary>
public enum JobLifecycleEventType
{
    /// <summary>A new job order is saved ("JB5 新增訂單").</summary>
    OrderCreated,

    /// <summary>A job is saved onto the schedule board ("JB5 已排單").</summary>
    Scheduled,

    /// <summary>The plate workflow step becomes ready ("JB5 有鋅").</summary>
    ReadyPlate,

    /// <summary>The paper workflow step becomes ready ("JB5 有紙").</summary>
    ReadyPaper,

    /// <summary>A scheduled job is marked completed ("JB5 全單完成").</summary>
    Completed,

    /// <summary>A job acquires invoice data ("JB5 已開發票").</summary>
    Invoiced,

    /// <summary>A stock product COGS value is set or changed ("JB5 已填成本").</summary>
    CogsFilled,
}