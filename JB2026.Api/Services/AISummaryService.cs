using System.Text;
using System.Text.Json;
using JB2026.Api.Models;
using JB2026.Api.Options;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;

namespace JB2026.Api.Services;

public sealed class AISummaryService
{
    private readonly IOllamaApiClient _ollamaClient;
    private readonly IOptions<OllamaOptions> _options;
    private readonly ILogger<AISummaryService> _logger;

    public AISummaryService(
        IOllamaApiClient ollamaClient,
        IOptions<OllamaOptions> options,
        ILogger<AISummaryService> logger)
    {
        _ollamaClient = ollamaClient;
        _options = options;
        _logger = logger;
    }

    public async Task<ContactInfoSummary?> SummarizeAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogWarning("AI summarization is disabled.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var systemPrompt = $"""
            You are a contact information extraction assistant. Extract structured fields from the given text.
            Return ONLY valid JSON with these exact fields:
            - company_name: the company or business name (string, empty if not found)
            - address: the full address block (string, empty if not found)
            - phone: primary phone number (string, empty if not found)
            - fax: fax number (string, empty if not found)
            - attention_to: contact person or department name (string, empty if not found)
            - detected_language: ISO 639-1 language code of the input text (e.g., "en", "de", "fr")
            Use empty string for missing fields. Do not include any text outside the JSON object.
            """;

        var request = new GenerateRequest
        {
            Model = _options.Value.DefaultModel,
            Prompt = rawText,
            System = systemPrompt,
            Format = "json",
            Stream = false,
            KeepAlive = "5m"
        };

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.Value.TimeoutSeconds));

            var rawResponse = await CollectResponseAsync(
                _ollamaClient.GenerateAsync(request, timeoutCts.Token));

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                _logger.LogWarning("Ollama returned empty response for summarization.");
                return null;
            }

            var cleaned = StripMarkdownWrappers(rawResponse);

            try
            {
                var summary = JsonSerializer.Deserialize<ContactInfoSummary>(cleaned);
                if (summary is null)
                {
                    _logger.LogError("Failed to deserialize Ollama response into ContactInfoSummary. Raw: {Raw}", TruncateForLog(rawResponse));
                    return null;
                }

                return summary;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing failed for Ollama response. Raw: {Raw}", TruncateForLog(rawResponse));
                return null;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Ollama summarization timed out after {Timeout}s.", _options.Value.TimeoutSeconds);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error contacting Ollama at {BaseUrl}.", _options.Value.BaseUrl);
            return null;
        }
    }

    private static async Task<string?> CollectResponseAsync(
        IAsyncEnumerable<GenerateResponseStream?> stream,
        CancellationToken cancellationToken = default)
    {
        var responseBuilder = new StringBuilder();

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            if (chunk?.Response is not null)
            {
                responseBuilder.Append(chunk.Response);
            }
        }

        var result = responseBuilder.ToString().Trim();
        return result.Length > 0 ? result : null;
    }

    private static string StripMarkdownWrappers(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('\n');
            if (start > 0)
            {
                trimmed = trimmed[(start + 1)..];
            }

            var end = trimmed.LastIndexOf("```");
            if (end >= 0)
            {
                trimmed = trimmed[..end];
            }

            trimmed = trimmed.Trim();
        }

        return trimmed;
    }

    private static string TruncateForLog(string text, int maxLength = 500)
    {
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}
