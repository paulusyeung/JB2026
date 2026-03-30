namespace JB2026.Api.Models;

public sealed class HelpArticleResponse
{
    public required string ArticleId { get; init; }

    public required string Title { get; init; }

    public required string Category { get; init; }

    public required string Content { get; init; }
}