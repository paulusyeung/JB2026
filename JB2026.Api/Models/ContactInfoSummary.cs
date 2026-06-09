using System.Text.Json.Serialization;

namespace JB2026.Api.Models;

public sealed class ContactInfoSummary
{
    [JsonPropertyName("company_name")]
    public string CompanyName { get; init; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; init; } = string.Empty;

    [JsonPropertyName("fax")]
    public string Fax { get; init; } = string.Empty;

    [JsonPropertyName("attention_to")]
    public string AttentionTo { get; init; } = string.Empty;

    [JsonPropertyName("detected_language")]
    public string DetectedLanguage { get; init; } = "en";
}
