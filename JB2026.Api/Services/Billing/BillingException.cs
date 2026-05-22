namespace JB2026.Api.Services.Billing;

/// <summary>
/// Exception thrown when a billing operation fails.
/// Contains specific error information suitable for frontend consumption.
/// </summary>
public class BillingException : Exception
{
    /// <summary>
    /// Error code identifying the type of failure (e.g., "INVALID_CONFIG", "AUTH_FAILED", "RATE_LIMITED", "SERVICE_UNAVAILABLE").
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// HTTP status code from Invoice Ninja API, if applicable.
    /// </summary>
    public int? InvoiceNinjaStatusCode { get; }

    /// <summary>
    /// Additional details for debugging or display.
    /// </summary>
    public object? Details { get; }

    public BillingException(string errorCode, string message, int? statusCode = null, object? details = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        InvoiceNinjaStatusCode = statusCode;
        Details = details;
    }

    /// <summary>
    /// Factory for authentication failures (401).
    /// </summary>
    public static BillingException InvalidApiKey(string? message = null, Exception? innerException = null)
    {
        return new BillingException(
            "INVALID_API_KEY",
            message ?? "Invoice Ninja API key is invalid or expired.",
            401,
            null,
            innerException);
    }

    /// <summary>
    /// Factory for not found errors (404).
    /// </summary>
    public static BillingException NotFound(string resource, Exception? innerException = null)
    {
        return new BillingException(
            "NOT_FOUND",
            $"Invoice Ninja resource not found: {resource}",
            404,
            new { resource },
            innerException);
    }

    /// <summary>
    /// Factory for rate limiting (429).
    /// </summary>
    public static BillingException RateLimited(Exception? innerException = null)
    {
        return new BillingException(
            "RATE_LIMITED",
            "Invoice Ninja API rate limit exceeded. Please try again later.",
            429,
            null,
            innerException);
    }

    /// <summary>
    /// Factory for service unavailable (503, 502, etc.).
    /// </summary>
    public static BillingException ServiceUnavailable(int statusCode, Exception? innerException = null)
    {
        return new BillingException(
            "SERVICE_UNAVAILABLE",
            "Invoice Ninja service is temporarily unavailable. Please try again later.",
            statusCode,
            new { statusCode },
            innerException);
    }

    /// <summary>
    /// Factory for configuration errors.
    /// </summary>
    public static BillingException ConfigurationError(string message, Exception? innerException = null)
    {
        return new BillingException(
            "INVALID_CONFIG",
            message,
            null,
            null,
            innerException);
    }

    /// <summary>
    /// Factory for generic HTTP errors.
    /// </summary>
    public static BillingException HttpError(int statusCode, string? message = null, Exception? innerException = null)
    {
        return new BillingException(
            "HTTP_ERROR",
            message ?? $"Invoice Ninja API returned status code {statusCode}.",
            statusCode,
            new { statusCode },
            innerException);
    }
}
