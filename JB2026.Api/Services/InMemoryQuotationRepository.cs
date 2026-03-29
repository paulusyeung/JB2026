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
            "31 x 44 in",
            "350gsm Art Card",
            420m,
            "31 x 44",
            4,
            8,
            "2-up",
            12m,
            18m,
            "Retail promo launch",
            1880m,
            1960m,
            2040m,
            2120m,
            1.57m,
            1.63m,
            1.70m,
            1.76m,
            [
                new QuotationLine("Printing", "4C process setup", "1000", 0.85m, 850m, 930m, 1010m, 1090m),
                new QuotationLine("Finishing", "Gloss lamination", "1000", 0.22m, 220m, 250m, 280m, 310m),
                new QuotationLine("Packing", "Carton packing", "500", 0.10m, 50m, 60m, 70m, 80m)
            ],
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
            "24 x 36 in",
            "200gsm Satin",
            180m,
            "24 x 36",
            2,
            2,
            "single-up",
            24m,
            36m,
            "Short-run campaign proof",
            980m,
            1040m,
            1120m,
            1200m,
            1.53m,
            1.62m,
            1.75m,
            1.88m,
            [
                new QuotationLine("Prepress", "Color proof", "1", 120m, 120m, 130m, 140m, 150m),
                new QuotationLine("Printing", "Digital output", "640", 1.10m, 704m, 760m, 820m, 880m)
            ],
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
            "640 x 900 mm",
            "128gsm Gloss",
            610m,
            "640 x 900",
            8,
            16,
            "8-up",
            8.27m,
            11.69m,
            "Quarterly catalog main run",
            3240m,
            3380m,
            3520m,
            3660m,
            1.29m,
            1.35m,
            1.41m,
            1.46m,
            [
                new QuotationLine("Printing", "64pp text signatures", "2500", 0.92m, 2300m, 2400m, 2500m, 2600m),
                new QuotationLine("Binding", "Perfect bind", "2500", 0.18m, 450m, 490m, 530m, 570m),
                new QuotationLine("Packing", "Shrink wrap", "2500", 0.06m, 150m, 170m, 190m, 210m)
            ],
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

        var lines = new List<string>
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
            $"({Escape($"Print: {quotation.PrintsSize} / {quotation.PrintsColor} / Qty {quotation.PrintsQty:0.##}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Quoted On: {quotation.QuotedOn:yyyy-MM-dd}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Material: {quotation.MaterialName} / {quotation.PaperSheetSize} / Cost {quotation.MaterialCost:0.00}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Layout: {quotation.PaperSheetSizeAlias} / Format {quotation.PrintsPerSheet} / PerPage {quotation.PrintsPerPage} {quotation.PrintPerPageEx}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Page Size: {quotation.PageWidth:0.##} x {quotation.PageHeight:0.##}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Remarks: {quotation.Remarks}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Total Cost A: {quotation.TotalCostA:0.00} / B {quotation.TotalCostB:0.00} / C {quotation.TotalCostC:0.00} / D {quotation.TotalCostD:0.00}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Unit Cost A: {quotation.UnitCostA:0.000} / B {quotation.UnitCostB:0.000} / C {quotation.UnitCostC:0.000} / D {quotation.UnitCostD:0.000}")}) Tj"
        };

        foreach (var line in quotation.Lines.Take(8))
        {
            lines.Add("0 -18 Td");
            lines.Add($"({Escape($"{line.Zone}: {line.Description} / Min {line.Minimum} / Unit {line.UnitCost:0.###} / A {line.CostA:0.##} / B {line.CostB:0.##}")}) Tj");
        }

        lines.Add("ET");
        return string.Join("\n", lines);
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
        string PaperSheetSize,
        string MaterialName,
        decimal MaterialCost,
        string PaperSheetSizeAlias,
        int PrintsPerSheet,
        int PrintsPerPage,
        string PrintPerPageEx,
        decimal PageWidth,
        decimal PageHeight,
        string Remarks,
        decimal TotalCostA,
        decimal TotalCostB,
        decimal TotalCostC,
        decimal TotalCostD,
        decimal UnitCostA,
        decimal UnitCostB,
        decimal UnitCostC,
        decimal UnitCostD,
        IReadOnlyList<QuotationLine> Lines,
        int Status)
    {
        public string QuoteNumberIndexPair => $"{QuoteNumber}-{QuoteNumberIndex}";
    }

    private sealed record QuotationLine(
        string Zone,
        string Description,
        string Minimum,
        decimal UnitCost,
        decimal CostA,
        decimal CostB,
        decimal CostC,
        decimal CostD);
}
