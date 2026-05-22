namespace JB2026.Api.Options;

/// <summary>
/// Configuration for Invoice Ninja integration.
/// </summary>
public class BillingOptions
{
    public const string SectionName = "Billing";

    /// <summary>
    /// Invoice Ninja API configuration.
    /// </summary>
    public InvoiceNinjaOptions InvoiceNinja { get; set; } = new();
}

/// <summary>
/// Invoice Ninja API configuration options.
/// </summary>
public class InvoiceNinjaOptions
{
    /// <summary>
    /// API key for Invoice Ninja service account. Must be set via secrets provider in production.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Invoice Ninja API (e.g., https://invoicing.example.com/api/v1).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Custom field configuration mapping logical names to Invoice Ninja custom field keys.
    /// </summary>
    public InvoiceNinjaCustomFieldsOptions CustomFields { get; set; } = new();

    /// <summary>
    /// HTTP client timeout in seconds.
    /// </summary>
    public int HttpClientTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for safe reads (GET).
    /// </summary>
    public int RetryMaxAttempts { get; set; } = 3;

    /// <summary>
    /// Backoff multiplier for exponential retry backoff (e.g., 2.0 means double the delay each retry).
    /// </summary>
    public double RetryBackoffMultiplier { get; set; } = 2.0;
}

/// <summary>
/// Custom field slot mappings from logical names to Invoice Ninja field keys.
/// </summary>
public class InvoiceNinjaCustomFieldsOptions
{
    /// <summary>
    /// Client Bill To custom field key.
    /// </summary>
    public string ClientBillTo { get; set; } = string.Empty;

    /// <summary>
    /// Client Ship To custom field key.
    /// </summary>
    public string ClientShipTo { get; set; } = string.Empty;

    /// <summary>
    /// Client Fax custom field key (optional, may be omitted until metadata exists).
    /// </summary>
    public string ClientFax { get; set; } = string.Empty;

    /// <summary>
    /// Client contact Full Name custom field key (optional, may be omitted until metadata exists).
    /// </summary>
    public string ContactFullName { get; set; } = string.Empty;

    /// <summary>
    /// Product/line item Unit custom field key (optional, unit source TBD).
    /// </summary>
    public string ProductUnit { get; set; } = string.Empty;

    /// <summary>
    /// Product/line item P.O.No. custom field key.
    /// </summary>
    public string ProductPoNo { get; set; } = string.Empty;

    /// <summary>
    /// Invoice Job No. custom field key.
    /// </summary>
    public string InvoiceJobNo { get; set; } = string.Empty;
}
