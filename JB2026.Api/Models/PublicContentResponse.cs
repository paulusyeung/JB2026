namespace JB2026.Api.Models;

public sealed class PublicContentResponse
{
    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required string UrlPath { get; init; }
}