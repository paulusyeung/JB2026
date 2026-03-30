using JB2026.Api.Models;

namespace JB2026.Api.Services;

public sealed class InMemoryHelpContentService : IHelpContentService
{
    private static readonly IReadOnlyList<HelpArticleResponse> Articles =
    [
        new HelpArticleResponse
        {
            ArticleId = "getting-started",
            Title = "Getting Started",
            Category = "Onboarding",
            Content = "Learn how to navigate the JB2026 workspace and key modules.",
        },
        new HelpArticleResponse
        {
            ArticleId = "job-order-lifecycle",
            Title = "Job Order Lifecycle",
            Category = "Operations",
            Content = "Understand how job orders move from creation to completion.",
        },
    ];

    public IReadOnlyList<HelpArticleResponse> GetArticles()
    {
        return Articles;
    }
}