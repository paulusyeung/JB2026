using JB2026.Api.Models;

namespace JB2026.Api.Services;

public sealed class InMemoryPublicContentService : IPublicContentService
{
    private static readonly IReadOnlyList<PublicContentResponse> Content =
    [
        new PublicContentResponse
        {
            Slug = "company-profile",
            Title = "Company Profile",
            Summary = "Overview of JB2026 printing capabilities and service scope.",
            UrlPath = "/public/company-profile",
        },
        new PublicContentResponse
        {
            Slug = "service-catalog",
            Title = "Service Catalog",
            Summary = "Browse available print and finishing services.",
            UrlPath = "/public/service-catalog",
        },
    ];

    public IReadOnlyList<PublicContentResponse> GetContent()
    {
        return Content;
    }
}