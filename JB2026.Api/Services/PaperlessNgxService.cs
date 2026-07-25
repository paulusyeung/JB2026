using System.Text.Json;
using System.Text.Json.Serialization;
using JB2026.Api.Options;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services;

public interface IPaperlessNgxService
{
    Task<IReadOnlyList<PaperlessNgxDocumentResponse>> SearchDocumentsAsync(string query, string? searchText = null, CancellationToken cancellationToken = default);
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

    public async Task<IReadOnlyList<PaperlessNgxDocumentResponse>> SearchDocumentsAsync(string query, string? searchText = null, CancellationToken cancellationToken = default)
    {
        var cfg = _options.Value;
        if (string.IsNullOrWhiteSpace(cfg.BaseUrl) || string.IsNullOrWhiteSpace(cfg.ApiToken))
            return [];

        var searchTerms = string.IsNullOrWhiteSpace(searchText)
            ? SplitSearchTerms(query)
            : [searchText.Trim()];
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

                var nextUrl = $"/api/documents/?query={Uri.EscapeDataString(term.Trim())}&page_size=100&ordering=-created";
                var pageCount = 0;
                const int maxPages = 50;

                while (!string.IsNullOrWhiteSpace(nextUrl) && pageCount < maxPages)
                {
                    pageCount++;
                    _logger.LogInformation("[PNGX] GET {Url}", nextUrl);

                    var response = await client.GetAsync(nextUrl, cancellationToken);
                    _logger.LogInformation("[PNGX] status={StatusCode} term={Term} page={Page}", (int)response.StatusCode, term, pageCount);

                    if (!response.IsSuccessStatusCode)
                        break;

                    var body = await response.Content.ReadFromJsonAsync<PaperlessNgxSearchResult>(JsonOptions, cancellationToken);
                    if (body?.Results is null)
                        break;

                    foreach (var doc in body.Results)
                    {
                        if (seenIds.Add(doc.Id))
                            allResults.Add(doc);
                    }

                    nextUrl = body.Next ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[PNGX] error for term={Term}: {Msg}", term, ex.Message);
            }
        }

        if (allResults.Count == 0)
        {
            _logger.LogInformation("[PNGX] no results for query={Query}", query);
            return [];
        }

        var correspondents = await FetchLookupsAsync<PaperlessNgxCorrespondent>("/api/correspondents/", cancellationToken);
        var documentTypes = await FetchLookupsAsync<PaperlessNgxDocumentType>("/api/document_types/", cancellationToken);
        var tags = await FetchLookupsAsync<PaperlessNgxTag>("/api/tags/", cancellationToken);
        var users = await FetchLookupsAsync<PaperlessNgxUser>("/api/users/", cancellationToken);

        var corrDict = correspondents.ToDictionary(c => c.Id, c => c.Name);
        var docTypeDict = documentTypes.ToDictionary(d => d.Id, d => d.Name);
        var tagDict = tags.ToDictionary(t => t.Id);
        var userDict = users.ToDictionary(u => u.Id, u => u.FirstName ?? u.Username);

        var enriched = allResults.Select(doc => new PaperlessNgxDocumentResponse
        {
            Id = doc.Id,
            Title = doc.Title,
            Created = doc.Created,
            Added = doc.Added,
            ArchiveSerialNumber = doc.ArchiveSerialNumber,
            MimeType = doc.MimeType,
            OriginalFileName = doc.OriginalFileName,
            CorrespondentName = doc.Correspondent.HasValue ? corrDict.GetValueOrDefault(doc.Correspondent.Value) : null,
            DocumentTypeName = doc.DocumentType.HasValue ? docTypeDict.GetValueOrDefault(doc.DocumentType.Value) : null,
            PageCount = doc.PageCount,
            OwnerName = doc.Owner.HasValue ? userDict.GetValueOrDefault(doc.Owner.Value) : null,
            IsSharedByRequester = doc.IsSharedByRequester,
            NoteCount = doc.Notes?.Count ?? 0,
            Tags = doc.Tags
                .Where(t => tagDict.ContainsKey(t))
                .Select(t => new PaperlessNgxTagResponse
                {
                    Id = t,
                    Name = tagDict[t].Name,
                    Color = tagDict[t].Color,
                })
                .ToList(),
        }).ToList();

        await FetchThumbnailsAsync(cfg, enriched, cancellationToken);

        _logger.LogInformation("[PNGX] returning {Count} enriched docs for query={Query}", enriched.Count, query);
        return enriched.AsReadOnly();
    }

    private async Task<List<T>> FetchLookupsAsync<T>(string url, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var cfg = _options.Value;
            client.BaseAddress = new Uri(cfg.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.Add("Authorization", $"Token {cfg.ApiToken}");
            client.Timeout = TimeSpan.FromSeconds(
                cfg.HttpClientTimeoutSeconds > 0 ? cfg.HttpClientTimeoutSeconds : DefaultTimeoutSeconds);

            var allResults = new List<T>();
            var currentUrl = url;

            while (!string.IsNullOrWhiteSpace(currentUrl))
            {
                var response = await client.GetAsync(currentUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    break;

                var body = await response.Content.ReadFromJsonAsync<PaperlessNgxLookupResult<T>>(JsonOptions, cancellationToken);
                if (body?.Results is null)
                    break;

                allResults.AddRange(body.Results);
                currentUrl = body.Next;
            }

            return allResults;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[PNGX] lookup error for {Url}: {Msg}", url, ex.Message);
            return [];
        }
    }

    private static async Task FetchThumbnailsAsync(PaperlessNgxOptions cfg, List<PaperlessNgxDocumentResponse> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
            return;

        using var client = new HttpClient();
        client.BaseAddress = new Uri(cfg.BaseUrl.TrimEnd('/'));
        client.DefaultRequestHeaders.Add("Authorization", $"Token {cfg.ApiToken}");
        client.Timeout = TimeSpan.FromSeconds(5);

        var tasks = documents.Select(async doc =>
        {
            try
            {
                var response = await client.GetAsync($"/api/documents/{doc.Id}/thumb/", cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return;

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/webp";
                doc.Thumbnail = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
            }
            catch
            {
                // thumbnails are optional
            }
        });

        await Task.WhenAll(tasks);
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
    [JsonPropertyName("next")]
    public string? Next { get; set; }
    public List<PaperlessNgxDocument>? Results { get; set; }
}

public sealed class PaperlessNgxDocument
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateOnly Created { get; set; }
    public DateTime Modified { get; set; }
    public DateTime Added { get; set; }
    [JsonPropertyName("archive_serial_number")]
    public string? ArchiveSerialNumber { get; set; }
    public int? Correspondent { get; set; }
    [JsonPropertyName("document_type")]
    public int? DocumentType { get; set; }
    [JsonPropertyName("storage_path")]
    public int? StoragePath { get; set; }
    public List<int> Tags { get; set; } = [];
    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = string.Empty;
    [JsonPropertyName("original_file_name")]
    public string? OriginalFileName { get; set; }
    public int? Owner { get; set; }
    public List<object> Notes { get; set; } = [];
    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }
    [JsonPropertyName("is_shared_by_requester")]
    public bool IsSharedByRequester { get; set; }
}

public sealed class PaperlessNgxDocumentResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly Created { get; set; }
    public DateTime Added { get; set; }
    public string? ArchiveSerialNumber { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? CorrespondentName { get; set; }
    public string? DocumentTypeName { get; set; }
    public int PageCount { get; set; }
    public string? OwnerName { get; set; }
    public bool IsSharedByRequester { get; set; }
    public int NoteCount { get; set; }
    public string? Thumbnail { get; set; }
    public List<PaperlessNgxTagResponse> Tags { get; set; } = [];
}

public sealed class PaperlessNgxTagResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public sealed class PaperlessNgxLookupResult<T>
{
    [JsonPropertyName("next")]
    public string? Next { get; set; }
    public List<T> Results { get; set; } = [];
}

public sealed class PaperlessNgxCorrespondent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class PaperlessNgxDocumentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class PaperlessNgxTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public sealed class PaperlessNgxUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
}
