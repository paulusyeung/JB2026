namespace JB2026.Api.Models;

/// <summary>
/// One push-notification history entry that belongs to an order, projected for the
/// job timeline. Legacy <c>FCMHistory.MessageBody</c> rows are written as
/// <c>{OrderNumber}-{JobNumber}: {CustomerName}</c>, so the timeline is the delivery
/// history for every job number under the order.
/// </summary>
public sealed class JobTimelineItemResponse
{
    public required Guid FCMHistoryId { get; init; }

    public required DateTime DeliveredOn { get; init; }

    public string? MessageTitle { get; init; }

    public string? MessageBody { get; init; }

    public string? Topic { get; init; }
}
