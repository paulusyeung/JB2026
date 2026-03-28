using System.Text;
using JB2026.Api.Models;

namespace JB2026.Api.Services;

public sealed class InMemoryQuotationRepository : IQuotationRepository
{
    private readonly IReadOnlyList<QuotationRecord> _quotations =
    [
        new(
            Guid.Parse("2a84b2e5-3f73-4d60-9d0d-08dc50c00001"),
            "Offset",
            61024,
            1,
            new DateTime(2026, 3, 20, 8, 0, 0, DateTimeKind.Utc),
            "admin",
            new DateTime(2026, 3, 22, 10, 0, 0, DateTimeKind.Utc),
            "admin",
            "Retail Packaging Artwork",
            "Northwind Print Co.",
            "A3",
            "CMYK",
            1200m,
            "350gsm Art Card",
            420m,
            1880m,
            1.57m,
            2),
        new(
            Guid.Parse("2a84b2e5-3f73-4d60-9d0d-08dc50c00002"),
            "Digital",
            61025,
            2,
            new DateTime(2026, 3, 24, 8, 0, 0, DateTimeKind.Utc),
            "admin",
            null,
            null,
            "Campaign Poster Refresh",
            "Litware Agency",
            "A1",
            "CMYK + Spot UV",
            640m,
            "200gsm Satin",
            180m,
            980m,
            1.53m,
            1),
        new(
            Guid.Parse("2a84b2e5-3f73-4d60-9d0d-08dc50c00003"),
            "Offset",
            61026,
            1,
            new DateTime(2026, 3, 27, 8, 0, 0, DateTimeKind.Utc),
            "admin",
            null,
            null,
            "Quarterly Product Catalogue",
            "Adventure Works",
            "210x297mm",
            "CMYK",
            2500m,
            "128gsm Gloss",
            610m,
            3240m,
            1.29m,
            1)
    ];

    public IReadOnlyList<QuotationListItemResponse> GetRange(DateOnly startOn, int days)
    {
        var start = startOn.ToDateTime(TimeOnly.MinValue);
        return _quotations
            .Where(q => q.QuotedOn < start.AddDays(1) && q.QuotedOn > start.AddDays(-days))
            .OrderByDescending(q => q.QuoteNumber)
            .ThenBy(q => q.QuoteNumberIndex)
            .Select(Map)
            .ToList();
    }

    public IReadOnlyList<QuotationListItemResponse> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length < 3)
        {
            return [];
        }

        var normalized = keyword.Trim();
        return _quotations
            .Where(q => q.Status >= 1 &&
                        (q.HeaderId.ToString().Contains(normalized, StringComparison.OrdinalIgnoreCase)
                         || q.QuoteNumber.ToString().Contains(normalized, StringComparison.OrdinalIgnoreCase)
                         || q.PrintTitle.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                         || q.CustomerName.Contains(normalized, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(q => q.QuoteNumber)
            .ThenBy(q => q.QuoteNumberIndex)
            .Select(Map)
            .ToList();
    }

    public (byte[] Content, string FileName)? GetPdf(Guid headerId)
    {
        var quotation = _quotations.SingleOrDefault(q => q.HeaderId == headerId);
        if (quotation is null)
        {
            return null;
        }

        var lines = new[]
        {
            "%PDF-1.4",
            "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj",
            "2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj",
            "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj",
            $"4 0 obj<</Length {GetContentStream(quotation).Length}>>stream\n{GetContentStream(quotation)}\nendstream endobj",
            "5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj",
            "xref",
            "0 6",
            "0000000000 65535 f ",
            "0000000010 00000 n ",
            "0000000060 00000 n ",
            "0000000117 00000 n ",
            "0000000243 00000 n ",
            "0000000000 00000 n ",
            "trailer<</Size 6/Root 1 0 R>>",
            "startxref",
            "0",
            "%%EOF"
        };

        return (Encoding.ASCII.GetBytes(string.Join("\n", lines)), $"quotation-{quotation.QuoteNumberIndexPair}.pdf");
    }

    private static string GetContentStream(QuotationRecord quotation)
    {
        static string Escape(string value) => value.Replace("(", "[").Replace(")", "]");

        return string.Join("\n", new[]
        {
            "BT",
            "/F1 18 Tf",
            "50 740 Td",
            $"({Escape($"Quotation {quotation.QuoteNumberIndexPair}")}) Tj",
            "0 -24 Td",
            "/F1 12 Tf",
            $"({Escape($"Customer: {quotation.CustomerName}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Title: {quotation.PrintTitle}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Quoted On: {quotation.QuotedOn:yyyy-MM-dd}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Total Cost A: {quotation.TotalCostA:0.00}")}) Tj",
            "ET"
        });
    }

    private static QuotationListItemResponse Map(QuotationRecord quotation)
    {
        return new QuotationListItemResponse
        {
            HeaderId = quotation.HeaderId,
            MachineType = quotation.MachineType,
            QuoteNumber = quotation.QuoteNumber,
            QuoteNumberIndex = quotation.QuoteNumberIndex,
            QuoteNumberIndexPair = quotation.QuoteNumberIndexPair,
            QuotedOn = quotation.QuotedOn,
            QuotedBy = quotation.QuotedBy,
            ApprovedOn = quotation.ApprovedOn,
            ApprovedBy = quotation.ApprovedBy,
            PrintTitle = quotation.PrintTitle,
            CustomerName = quotation.CustomerName,
            PrintsSize = quotation.PrintsSize,
            PrintsColor = quotation.PrintsColor,
            PrintsQty = quotation.PrintsQty,
            MaterialName = quotation.MaterialName,
            MaterialCost = quotation.MaterialCost,
            TotalCostA = quotation.TotalCostA,
            UnitCostA = quotation.UnitCostA,
            Status = quotation.Status
        };
    }

    private sealed record QuotationRecord(
        Guid HeaderId,
        string MachineType,
        int QuoteNumber,
        int QuoteNumberIndex,
        DateTime QuotedOn,
        string QuotedBy,
        DateTime? ApprovedOn,
        string? ApprovedBy,
        string PrintTitle,
        string CustomerName,
        string PrintsSize,
        string PrintsColor,
        decimal PrintsQty,
        string MaterialName,
        decimal MaterialCost,
        decimal TotalCostA,
        decimal UnitCostA,
        int Status)
    {
        public string QuoteNumberIndexPair => $"{QuoteNumber}-{QuoteNumberIndex}";
    }
}
