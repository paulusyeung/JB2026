namespace JB2026.Api.Services.Billing;

using JB2026.Api.Models.Billing;
using JB2026.Api.Options;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Interface for low-level Invoice Ninja HTTP client operations.
/// Handles authentication, timeouts, retries, and redacted logging.
/// </summary>
public interface IInvoiceNinjaHttpClient
{
    /// <summary>
    /// Performs a GET request to Invoice Ninja API with retry logic for safe reads.
    /// </summary>
    /// <typeparam name="T">Response data type.</typeparam>
    /// <param name="endpoint">API endpoint path (e.g., "/clients/1").</param>
    /// <returns>Deserialized response or null if not found.</returns>
    Task<T?> GetAsync<T>(string endpoint) where T : class;

    /// <summary>
    /// Performs a POST request to Invoice Ninja API.
    /// </summary>
    /// <typeparam name="T">Response data type.</typeparam>
    /// <param name="endpoint">API endpoint path.</param>
    /// <param name="body">Request body to serialize and send.</param>
    /// <returns>Deserialized response.</returns>
    Task<T> PostAsync<T>(string endpoint, object body) where T : class;

    /// <summary>
    /// Performs a PUT request to Invoice Ninja API.
    /// </summary>
    /// <typeparam name="T">Response data type.</typeparam>
    /// <param name="endpoint">API endpoint path.</param>
    /// <param name="body">Request body to serialize and send.</param>
    /// <returns>Deserialized response.</returns>
    Task<T> PutAsync<T>(string endpoint, object body) where T : class;

    /// <summary>
    /// Checks connectivity to the Invoice Ninja API.
    /// </summary>
    /// <returns>True if API is reachable and responds with 2xx; false otherwise.</returns>
    Task<bool> IsConnectedAsync();

    /// <summary>
    /// Validates the current API configuration.
    /// </summary>
    /// <returns>Tuple of (isValid, errorMessage). If isValid is false, errorMessage explains why.</returns>
    (bool isValid, string errorMessage) ValidateConfiguration();
}

/// <summary>
/// Implementation of Invoice Ninja HTTP client with timeouts, retries, and redacted logging.
/// </summary>
public class InvoiceNinjaHttpClient : IInvoiceNinjaHttpClient
{
    private readonly IOptions<BillingOptions> _billingOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InvoiceNinjaHttpClient> _logger;
    private readonly JB5LegacyWriteContext? _writeContext;

    public InvoiceNinjaHttpClient(
        IOptions<BillingOptions> billingOptions,
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider,
        ILogger<InvoiceNinjaHttpClient> logger)
    {
        _billingOptions = billingOptions;
        _httpClientFactory = httpClientFactory;
        _writeContext = serviceProvider.GetService<JB5LegacyWriteContext>();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
        var (isValid, errorMessage) = ValidateConfiguration();
        if (!isValid)
        {
            _logger.LogError("Invoice Ninja configuration invalid: {ErrorMessage}", errorMessage);
            throw BillingException.ConfigurationError(errorMessage);
        }

        var options = _billingOptions.Value.InvoiceNinja;
        var url = $"{options.BaseUrl}{endpoint}";
        var maxRetries = options.RetryMaxAttempts;
        var backoffMultiplier = options.RetryBackoffMultiplier;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-TOKEN", options.ApiKey);
                    client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);

                    _logger.LogDebug("Invoice Ninja GET request: {Endpoint} (attempt {Attempt}/{MaxAttempts})", 
                        endpoint, attempt + 1, maxRetries);

                    var response = await client.GetAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogDebug("Invoice Ninja resource not found: {Endpoint}", endpoint);
                        return null;
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogError("Invoice Ninja authentication failed (401): {Endpoint}", endpoint);
                        throw BillingException.InvalidApiKey();
                    }

                    if ((int)response.StatusCode == 429)
                    {
                        _logger.LogWarning("Invoice Ninja rate limit hit (429): {Endpoint}", endpoint);
                        if (attempt < maxRetries - 1)
                        {
                            var delayMs = (int)(1000 * Math.Pow(backoffMultiplier, attempt));
                            _logger.LogWarning("Rate limited, retrying in {DelayMs}ms", delayMs);
                            await Task.Delay(delayMs);
                            continue;
                        }
                        throw BillingException.RateLimited();
                    }

                    if ((int)response.StatusCode is 502 or 503 or 504)
                    {
                        _logger.LogWarning("Invoice Ninja service unavailable ({StatusCode}): {Endpoint}", (int)response.StatusCode, endpoint);
                        if (attempt < maxRetries - 1)
                        {
                            var delayMs = (int)(1000 * Math.Pow(backoffMultiplier, attempt));
                            _logger.LogWarning("Service unavailable, retrying in {DelayMs}ms", delayMs);
                            await Task.Delay(delayMs);
                            continue;
                        }
                        throw BillingException.ServiceUnavailable((int)response.StatusCode);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Invoice Ninja GET failed with status {StatusCode}: {Endpoint}", (int)response.StatusCode, endpoint);
                        throw BillingException.HttpError((int)response.StatusCode, $"Invoice Ninja API returned {(int)response.StatusCode}");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<InvoiceNinjaApiResponse<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogDebug("Invoice Ninja GET succeeded: {Endpoint}", endpoint);
                    return data?.Data;
                }
            }
            catch (BillingException)
            {
                throw;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries - 1 && IsRetryableStatusCode(ex.StatusCode))
            {
                var delayMs = (int)(1000 * Math.Pow(backoffMultiplier, attempt));
                _logger.LogWarning("Invoice Ninja GET retryable error (attempt {Attempt}/{MaxAttempts}), retrying in {DelayMs}ms: {ErrorMessage}",
                    attempt + 1, maxRetries, delayMs, ex.Message);
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice Ninja GET failed at endpoint {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
                throw BillingException.HttpError(0, "Invoice Ninja GET request failed.", ex);
            }
        }

        throw BillingException.HttpError(0, "Invoice Ninja GET request failed after retries.");
    }

    public async Task<T> PostAsync<T>(string endpoint, object body) where T : class
    {
        var (isValid, errorMessage) = ValidateConfiguration();
        if (!isValid)
        {
            _logger.LogError("Invoice Ninja configuration invalid: {ErrorMessage}", errorMessage);
            throw BillingException.ConfigurationError(errorMessage);
        }

        var options = _billingOptions.Value.InvoiceNinja;
        var url = $"{options.BaseUrl}{endpoint}";

        try
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Add("X-API-TOKEN", options.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);

                var payload = SerializeForLog(body);
                _logger.LogInformation("Invoice Ninja POST request: {Endpoint} Payload: {Payload}", endpoint, payload);
                await PersistLegacyLog4NetEntryAsync("INFO", "InvoiceNinjaHttpClient", $"POST {endpoint} Payload: {payload}");

                var response = await client.PostAsJsonAsync(url, body);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Invoice Ninja authentication failed (401): {Endpoint}", endpoint);
                    throw BillingException.InvalidApiKey();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError("Invoice Ninja resource not found (404): {Endpoint}", endpoint);
                    throw BillingException.NotFound(endpoint);
                }

                if ((int)response.StatusCode == 429)
                {
                    _logger.LogWarning("Invoice Ninja rate limit hit (429): {Endpoint}", endpoint);
                    throw BillingException.RateLimited();
                }

                if ((int)response.StatusCode is 502 or 503 or 504)
                {
                    _logger.LogWarning("Invoice Ninja service unavailable ({StatusCode}): {Endpoint}", (int)response.StatusCode, endpoint);
                    throw BillingException.ServiceUnavailable((int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = TruncateForLog(await response.Content.ReadAsStringAsync());
                    _logger.LogError("Invoice Ninja POST failed with status {StatusCode}: {Endpoint}", (int)response.StatusCode, endpoint);
                    _logger.LogError("Invoice Ninja POST error response: {ResponseBody}", errorContent);
                    throw BillingException.HttpError((int)response.StatusCode, $"Invoice Ninja API returned {(int)response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<InvoiceNinjaApiResponse<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogDebug("Invoice Ninja POST succeeded: {Endpoint}", endpoint);
                return data?.Data ?? throw new InvalidOperationException("Empty response from Invoice Ninja API");
            }
        }
        catch (BillingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice Ninja POST failed at endpoint {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
            throw BillingException.HttpError(0, "Invoice Ninja POST request failed.", ex);
        }
    }

    public async Task<T> PutAsync<T>(string endpoint, object body) where T : class
    {
        var (isValid, errorMessage) = ValidateConfiguration();
        if (!isValid)
        {
            _logger.LogError("Invoice Ninja configuration invalid: {ErrorMessage}", errorMessage);
            throw BillingException.ConfigurationError(errorMessage);
        }

        var options = _billingOptions.Value.InvoiceNinja;
        var url = $"{options.BaseUrl}{endpoint}";

        try
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Add("X-API-TOKEN", options.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);

                var payload = SerializeForLog(body);
                _logger.LogInformation("Invoice Ninja PUT request: {Endpoint} Payload: {Payload}", endpoint, payload);
                await PersistLegacyLog4NetEntryAsync("INFO", "InvoiceNinjaHttpClient", $"PUT {endpoint} Payload: {payload}");

                var response = await client.PutAsJsonAsync(url, body);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Invoice Ninja authentication failed (401): {Endpoint}", endpoint);
                    throw BillingException.InvalidApiKey();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError("Invoice Ninja resource not found (404): {Endpoint}", endpoint);
                    throw BillingException.NotFound(endpoint);
                }

                if ((int)response.StatusCode == 429)
                {
                    _logger.LogWarning("Invoice Ninja rate limit hit (429): {Endpoint}", endpoint);
                    throw BillingException.RateLimited();
                }

                if ((int)response.StatusCode is 502 or 503 or 504)
                {
                    _logger.LogWarning("Invoice Ninja service unavailable ({StatusCode}): {Endpoint}", (int)response.StatusCode, endpoint);
                    throw BillingException.ServiceUnavailable((int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = TruncateForLog(await response.Content.ReadAsStringAsync());
                    _logger.LogError("Invoice Ninja PUT failed with status {StatusCode}: {Endpoint}", (int)response.StatusCode, endpoint);
                    _logger.LogError("Invoice Ninja PUT error response: {ResponseBody}", errorContent);
                    throw BillingException.HttpError((int)response.StatusCode, $"Invoice Ninja API returned {(int)response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<InvoiceNinjaApiResponse<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogDebug("Invoice Ninja PUT succeeded: {Endpoint}", endpoint);
                return data?.Data ?? throw new InvalidOperationException("Empty response from Invoice Ninja API");
            }
        }
        catch (BillingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice Ninja PUT failed at endpoint {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
            throw BillingException.HttpError(0, "Invoice Ninja PUT request failed.", ex);
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            var options = _billingOptions.Value.InvoiceNinja;
            
            if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                _logger.LogWarning("Invoice Ninja API key or base URL not configured");
                return false;
            }

            var url = $"{options.BaseUrl}/info";
            using (var client = _httpClientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Add("X-API-TOKEN", options.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);

                var response = await client.GetAsync(url);
                var isConnected = response.IsSuccessStatusCode;

                _logger.LogInformation("Invoice Ninja connectivity check: {IsConnected} (status: {StatusCode})",
                    isConnected, (int)response.StatusCode);

                return isConnected;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice Ninja connectivity check failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public (bool isValid, string errorMessage) ValidateConfiguration()
    {
        var options = _billingOptions.Value.InvoiceNinja;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return (false, "INVOICE_NINJA_API_KEY is not configured");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return (false, "INVOICE_NINJA_BASE_URL is not configured");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            return (false, $"INVOICE_NINJA_BASE_URL is not a valid URI: {options.BaseUrl}");
        }

        // Validate custom field configuration
        if (string.IsNullOrWhiteSpace(options.CustomFields.ClientBillTo) ||
            string.IsNullOrWhiteSpace(options.CustomFields.ClientShipTo) ||
            string.IsNullOrWhiteSpace(options.CustomFields.ProductPoNo) ||
            string.IsNullOrWhiteSpace(options.CustomFields.InvoiceJobNo))
        {
            return (false, "Required custom field mappings are not configured (ClientBillTo, ClientShipTo, ProductPoNo, InvoiceJobNo)");
        }

        return (true, string.Empty);
    }

    private bool IsRetryableStatusCode(System.Net.HttpStatusCode? statusCode)
    {
        return statusCode == System.Net.HttpStatusCode.RequestTimeout ||
               statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               statusCode == System.Net.HttpStatusCode.GatewayTimeout ||
               (int?)statusCode == 429; // Too Many Requests
    }

    private static string SerializeForLog(object body)
    {
        try
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return TruncateForLog(json);
        }
        catch
        {
            return "<serialization_failed>";
        }
    }

    private static string TruncateForLog(string value)
    {
        const int maxLength = 4000;
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...<truncated>";
    }

    private async Task PersistLegacyLog4NetEntryAsync(string level, string logger, string message)
    {
        if (_writeContext is null)
        {
            return;
        }

        try
        {
            const string sql = """
INSERT INTO Log4Net ([Date], [Thread], [Level], [Logger], [Message], [Exception])
VALUES ({0}, {1}, {2}, {3}, {4}, {5})
""";

            await _writeContext.Database.ExecuteSqlRawAsync(
                sql,
                DateTime.UtcNow,
                Environment.CurrentManagedThreadId.ToString(),
                TruncateForLog(level),
                TruncateForLog(logger),
                TruncateForLog(message),
                string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to persist Invoice Ninja payload log to Log4Net table.");
        }
    }
}
