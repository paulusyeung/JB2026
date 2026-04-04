namespace JB2026.Api.Models;

public sealed class SmlInvoiceListResponse
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required int RowCount { get; init; }

    public required IReadOnlyList<SmlInvoiceListRowResponse> Rows { get; init; }
}

public sealed class SmlInvoiceListRowResponse
{
    public required Guid HeaderId { get; init; }

    public required string InvoiceNumber { get; init; }

    public required int RowNumber { get; init; }

    public required string CustomerName { get; init; }

    public required DateTime InvoiceDate { get; init; }

    public required decimal InvoiceAmount { get; init; }

    public required string ICNumber { get; init; }

    public required DateTime CreatedOn { get; init; }

    public required string CreatedBy { get; init; }
}