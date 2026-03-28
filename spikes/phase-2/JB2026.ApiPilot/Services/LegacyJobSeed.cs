using JB2026.ApiPilot.Models;

namespace JB2026.ApiPilot.Services;

internal sealed record SeedJob(
    Guid OrderId,
    string OrderNumber,
    int JobNumber,
    string CustomerName,
    string CustomerRef,
    string OrderTitle,
    string OrderedBy,
    DateTime OrderedOn,
    DateTime RequiredOn,
    decimal Qty,
    int Status,
    string PaymentTerms,
    string Remarks,
    string[] StyleTitles,
    IReadOnlyList<JobAttachmentDto> Attachments)
{
    public string CompositeOrderNumber => $"{OrderNumber}-{JobNumber}";
}

internal static class LegacyJobSeed
{
    public static IReadOnlyList<SeedJob> Create() =>
    [
        new SeedJob(
            Guid.Parse("1e84b2e5-3f73-4d60-9d0d-08dc50c00001"),
            "JB2401",
            12,
            "Acme Retail",
            "ACM-4471",
            "Spring launch flyer",
            "mchan",
            new DateTime(2026, 3, 25, 10, 15, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 29, 9, 0, 0, DateTimeKind.Utc),
            1500m,
            2,
            "30 days",
            "Requires colour proof before press release.",
            ["Flyer Matte A4", "Retail Promo"],
            [
                new JobAttachmentDto { AttachmentType = "Product", FileName = "spring-flyer-proof.pdf" },
                new JobAttachmentDto { AttachmentType = "Customer", FileName = "acme-brand-guidelines.pdf" }
            ]),
        new SeedJob(
            Guid.Parse("1e84b2e5-3f73-4d60-9d0d-08dc50c00002"),
            "JB2403",
            3,
            "Northwind Foods",
            "NWF-8820",
            "Shelf wobblers refresh",
            "ajohnson",
            new DateTime(2026, 3, 21, 8, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 27, 15, 0, 0, DateTimeKind.Utc),
            3200m,
            1,
            "COD",
            "Keep dieline unchanged from February release.",
            ["Retail Pack", "Diecut Wobbler"],
            [
                new JobAttachmentDto { AttachmentType = "Product", FileName = "northwind-wobbler.ai" }
            ]),
        new SeedJob(
            Guid.Parse("1e84b2e5-3f73-4d60-9d0d-08dc50c00003"),
            "JB2309",
            7,
            "City Print Works",
            "CPW-1108",
            "Archive stock labels",
            "tlee",
            new DateTime(2026, 2, 1, 13, 45, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 6, 9, 0, 0, DateTimeKind.Utc),
            800m,
            4,
            "30 days",
            "Historical order kept to prove range filtering excludes older jobs.",
            ["Archive", "Stock Label"],
            [
                new JobAttachmentDto { AttachmentType = "Customer", FileName = "cityprint-legacy-order.docx" }
            ])
    ];
}