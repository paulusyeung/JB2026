using System.Text.Json;
using System.Text.Json.Serialization;
using JB2026.Api.Options;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services;

public interface IPaperlessNgxService
{
    Task<IReadOnlyList<PaperlessNgxDocument>> SearchDocumentsAsync(string query, CancellationToken cancellationToken = default);
}

public sealed class PaperlessNgxService : IPaperlessNgxService
{
    private readonly IOptions<PaperlessNgxOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaperlessNgxService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const int DefaultTimeoutSeconds = 10;

    public PaperlessNgxService(IOptions<PaperlessNgxOptions> options, IHttpClientFactory httpClientFactory, ILogger<PaperlessNgxService> logger)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PaperlessNgxDocument>> SearchDocumentsAsync(string query, CancellationToken cancellationToken = default)
    {
        var cfg = _options.Value;
        if (string.IsNullOrWhiteSpace(cfg.BaseUrl) || string.IsNullOrWhiteSpace(cfg.ApiToken))
            return [];

        var searchTerms = SplitSearchTerms(query);
        var allResults = new List<PaperlessNgxDocument>();
        var seenIds = new HashSet<int>();

        foreach (var term in searchTerms)
        {
            if (string.IsNullOrWhiteSpace(term))
                continue;

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(cfg.BaseUrl.TrimEnd('/'));
                client.DefaultRequestHeaders.Add("Authorization", $"Token {cfg.ApiToken}");
                client.Timeout = TimeSpan.FromSeconds(
                    cfg.HttpClientTimeoutSeconds > 0 ? cfg.HttpClientTimeoutSeconds : DefaultTimeoutSeconds);

                var url = $"/api/documents/?query={Uri.EscapeDataString(term.Trim())}&page_size=50";
                _logger.LogInformation("[PNGX] GET {Url}", url);

                var response = await client.GetAsync(url, cancellationToken);
                _logger.LogInformation("[PNGX] status={StatusCode} term={Term}", (int)response.StatusCode, term);

                if (!response.IsSuccessStatusCode)
                    continue;

                var body = await response.Content.ReadFromJsonAsync<PaperlessNgxSearchResult>(JsonOptions, cancellationToken);
                if (body?.Results is null)
                    continue;

                foreach (var doc in body.Results)
                {
                    if (seenIds.Add(doc.Id))
                        allResults.Add(doc);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[PNGX] error for term={Term}: {Msg}", term, ex.Message);
            }
        }

        _logger.LogInformation("[PNGX] returning {Count} docs for query={Query}", allResults.Count, query);
        return allResults.AsReadOnly();
    }

    private static string[] SplitSearchTerms(string name)
    {
        var englishPart = ExtractEnglishPart(name);
        var chinesePart = ExtractChinesePart(name);

        if (!string.IsNullOrWhiteSpace(englishPart) && !string.IsNullOrWhiteSpace(chinesePart))
            return [englishPart.Trim(), chinesePart.Trim()];

        return [name.Trim()];
    }

    private static string ExtractEnglishPart(string input) =>
        new string(input.Where(c => c <= 127).ToArray()).Trim();

    private static string ExtractChinesePart(string input) =>
        new string(input.Where(c => c > 127).ToArray()).Trim();
}

public sealed class PaperlessNgxSearchResult
{
    public int Count { get; set; }
    public List<PaperlessNgxDocument>? Results { get; set; }
}

public sealed class PaperlessNgxDocument
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public DateTime Added { get; set; }
    public int? DocumentType { get; set; }
    public int? Correspondent { get; set; }
    public List<int> Tags { get; set; } = [];
    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = string.Empty;
    [JsonPropertyName("original_filename")]
    public string? OriginalFilename { get; set; }
}
