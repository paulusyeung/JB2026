using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface IHelpContentService
{
    IReadOnlyList<HelpArticleResponse> GetArticles();
}