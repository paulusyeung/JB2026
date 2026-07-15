using System.Text.Json;
using JB2026.Api.Options;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services.TwentyCrm;

public class TwentyCrmService : ITwentyCrmService
{
    private readonly IOptions<TwentyCrmOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwentyCrmService> _logger;

    public TwentyCrmService(
        IOptions<TwentyCrmOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TwentyCrmService> logger)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — skipping email lookup for {Email}", email);
            return false;
        }

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);

        var baseUrl = options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/rest/workspaceMembers?limit=100";

        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Twenty CRM REST returned {StatusCode} for {Email}. Body: {Body}",
                    (int)response.StatusCode, email, Truncate(body));
                return false;
            }

            using var doc = JsonDocument.Parse(body);
            var members = doc.RootElement
                .GetProperty("data")
                .GetProperty("workspaceMembers");

            foreach (var member in members.EnumerateArray())
            {
                if (member.TryGetProperty("userEmail", out var userEmailEl)
                    && userEmailEl.ValueKind == JsonValueKind.String
                    && string.Equals(userEmailEl.GetString(), email, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check email {Email} in Twenty CRM", email);
            return false;
        }
    }

    private static string Truncate(string value, int max = 2000)
    {
        return value.Length <= max ? value : value[..max] + "...<truncated>";
    }
}
