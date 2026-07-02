namespace JB2026.Api.Models.Billing;

using System.Text.Json.Serialization;

/// <summary>
/// Invoice Ninja client (customer) representation.
/// </summary>
public class InvoiceNinjaClientResponse
{
    /// <summary>
    /// Unique client ID in Invoice Ninja.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Client display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional client identifier/number.
    /// </summary>
    [JsonPropertyName("id_number")]
    public string IdNumber { get; set; } = string.Empty;

    /// <summary>
    /// Client email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Currency code.
    /// </summary>
    [JsonPropertyName("currency_id")]
    public string CurrencyId { get; set; } = string.Empty;

    /// <summary>
    /// Current outstanding balance for the client.
    /// </summary>
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    /// <summary>
    /// Custom field values keyed by field key.
    /// </summary>
    [JsonPropertyName("custom_values")]
    public Dictionary<string, string?> CustomValues { get; set; } = new();

    /// <summary>
    /// Timestamp of last update.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    /// <summary>
    /// Client display name as rendered by Invoice Ninja.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Invoice Ninja group setting representation (group_settings endpoint).
/// </summary>
public class InvoiceNinjaGroupResponse
{
    /// <summary>
    /// Unique group ID in Invoice Ninja.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Group name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Request to create or update an Invoice Ninja client.
/// </summary>
public class CreateInvoiceNinjaClientRequest
{
    /// <summary>
    /// Client display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional client identifier/number (e.g., customerCode).
    /// </summary>
    [JsonPropertyName("id_number")]
    public string IdNumber { get; set; } = string.Empty;

    /// <summary>
    /// Client email address (optional).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Currency code (optional, uses company default if omitted).
    /// </summary>
    [JsonPropertyName("currency_id")]
    public string CurrencyId { get; set; } = string.Empty;

    /// <summary>
    /// Custom field values keyed by field key.
    /// </summary>
    [JsonPropertyName("custom_values")]
    public Dictionary<string, string?> CustomValues { get; set; } = new();
}

/// <summary>
/// Invoice Ninja invoice representation.
/// </summary>
public class InvoiceNinjaInvoiceResponse
{
    /// <summary>
    /// Unique invoice ID in Invoice Ninja.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Invoice number (display value).
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Client ID this invoice belongs to.
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Invoice amount (total).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Invoice date as returned by Invoice Ninja.
    /// </summary>
    [JsonPropertyName("date")]
    public string InvoiceDate { get; set; } = string.Empty;

    /// <summary>
    /// Invoice status (draft, sent, viewed, partial, paid, etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Due date as returned by Invoice Ninja.
    /// </summary>
    [JsonPropertyName("due_date")]
    public string DueDate { get; set; } = string.Empty;

    /// <summary>
    /// Invoice status identifier used by Invoice Ninja list responses.
    /// </summary>
    [JsonPropertyName("status_id")]
    public string StatusId { get; set; } = string.Empty;

    [JsonPropertyName("custom_value1")]
    public string CustomValue1 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value2")]
    public string CustomValue2 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value3")]
    public string CustomValue3 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value4")]
    public string CustomValue4 { get; set; } = string.Empty;

    public string GetCustomValue(string key) => key switch
    {
        "custom_value1" => CustomValue1,
        "custom_value2" => CustomValue2,
        "custom_value3" => CustomValue3,
        "custom_value4" => CustomValue4,
        _ => string.Empty
    };

    /// <summary>
    /// Invoice line items.
    /// </summary>
    [JsonPropertyName("line_items")]
    public List<InvoiceLineItemResponse> LineItems { get; set; } = new();

    /// <summary>
    /// Included client details when requested from Invoice Ninja.
    /// </summary>
    [JsonPropertyName("client")]
    public InvoiceNinjaClientResponse? Client { get; set; }

    /// <summary>
    /// Invitations for this invoice (included when requested with ?include=invitations).
    /// Used to extract the invitation_key for PDF downloads.
    /// </summary>
    [JsonPropertyName("invitations")]
    public List<InvoiceNinjaInvitation> Invitations { get; set; } = new();

    /// <summary>
    /// Indicates whether the invoice has been deleted in Invoice Ninja.
    /// </summary>
    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp of last update.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }
}

/// <summary>
/// Invoice Ninja invitation model (used for PDF download link extraction).
/// </summary>
public class InvoiceNinjaInvitation
{
    /// <summary>
    /// Unique invitation key used for PDF download endpoint.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Email address associated with this invitation.
    /// </summary>
    [JsonPropertyName("contact_id")]
    public string ContactId { get; set; } = string.Empty;
}

/// <summary>
/// Invoice line item representation.
/// </summary>
public class InvoiceLineItemResponse
{
    /// <summary>
    /// Line item description (Invoice Ninja stores this as 'notes').
    /// </summary>
    [JsonPropertyName("notes")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit cost.
    /// </summary>
    public decimal Cost { get; set; }

    [JsonPropertyName("custom_value1")]
    public string CustomValue1 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value2")]
    public string CustomValue2 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value3")]
    public string CustomValue3 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value4")]
    public string CustomValue4 { get; set; } = string.Empty;

    public string GetCustomValue(string key) => key switch
    {
        "custom_value1" => CustomValue1,
        "custom_value2" => CustomValue2,
        "custom_value3" => CustomValue3,
        "custom_value4" => CustomValue4,
        _ => string.Empty
    };
}

/// <summary>
/// Request to create an Invoice Ninja invoice.
/// </summary>
public class CreateInvoiceNinjaInvoiceRequest
{
    /// <summary>
    /// Client ID (Invoice Ninja ID, not JB2026 customer code).
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Invoice date (ISO date string, e.g. "2026-05-23").
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// Invoice due date (ISO date string, e.g. "2026-05-23").
    /// </summary>
    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("custom_value1")]
    public string CustomValue1 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value2")]
    public string CustomValue2 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value3")]
    public string CustomValue3 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value4")]
    public string CustomValue4 { get; set; } = string.Empty;

    public void SetCustomValue(string key, string? value)
    {
        switch (key)
        {
            case "custom_value1": CustomValue1 = value ?? string.Empty; break;
            case "custom_value2": CustomValue2 = value ?? string.Empty; break;
            case "custom_value3": CustomValue3 = value ?? string.Empty; break;
            case "custom_value4": CustomValue4 = value ?? string.Empty; break;
        }
    }

    /// <summary>
    /// Line items for the invoice.
    /// </summary>
    [JsonPropertyName("line_items")]
    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}

/// <summary>
/// Request to create an invoice line item.
/// </summary>
public class CreateInvoiceLineItemRequest
{
    /// <summary>
    /// Line item description (Invoice Ninja stores this as 'notes').
    /// </summary>
    [JsonPropertyName("notes")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity.
    /// </summary>
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit cost.
    /// </summary>
    [JsonPropertyName("cost")]
    public decimal Cost { get; set; }

    [JsonPropertyName("custom_value1")]
    public string CustomValue1 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value2")]
    public string CustomValue2 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value3")]
    public string CustomValue3 { get; set; } = string.Empty;

    [JsonPropertyName("custom_value4")]
    public string CustomValue4 { get; set; } = string.Empty;

    public void SetCustomValue(string key, string? value)
    {
        switch (key)
        {
            case "custom_value1": CustomValue1 = value ?? string.Empty; break;
            case "custom_value2": CustomValue2 = value ?? string.Empty; break;
            case "custom_value3": CustomValue3 = value ?? string.Empty; break;
            case "custom_value4": CustomValue4 = value ?? string.Empty; break;
        }
    }
}

/// <summary>
/// Summary of an invoice suitable for display in billing screens and job/order views.
/// </summary>
public class InvoiceBillingSummary
{
    /// <summary>
    /// External Invoice Ninja invoice ID.
    /// </summary>
    public string ExternalInvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Invoice number as displayed in Invoice Ninja.
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Client name associated with the invoice.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Invoice date.
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// Invoice total amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Invoice status (draft, sent, viewed, partial, paid, overdue, etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Due date (if available).
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Last sync timestamp with Invoice Ninja.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
}

/// <summary>
/// Response envelope for Invoice Ninja API responses.
/// </summary>
/// <typeparam name="T">The data type wrapped by the response.</typeparam>
public class InvoiceNinjaApiResponse<T>
{
    /// <summary>
    /// The wrapped data object.
    /// </summary>
    public T Data { get; set; } = default!;
}
