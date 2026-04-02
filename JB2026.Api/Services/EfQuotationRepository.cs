using System.Globalization;
using System.Text;
using JB2026.Api.Models;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class EfQuotationRepository : IQuotationRepository
{
    private readonly JB5LegacyReadContext _readContext;

    public EfQuotationRepository(JB5LegacyReadContext readContext)
    {
        _readContext = readContext;
    }

    public IReadOnlyList<QuotationListItemResponse> GetRange(DateOnly startOn, int days)
    {
        var start = startOn.ToDateTime(TimeOnly.MinValue);
        var rows = _readContext.vwQtHeaderLists
            .AsNoTracking()
            .Where(x => x.QuoteNumberIndex == 1
                        && x.QuotedOn < start.AddDays(1)
                        && x.QuotedOn > start.AddDays(-days))
            .OrderBy(x => x.QuoteNumber)
            .ThenBy(x => x.QuoteNumberIndex)
            .Select(Map)
            .ToList();

        if (rows.Count > 0)
        {
            return rows;
        }

        // Legacy data can be historical-only; fall back to latest rows when range is empty.
        return _readContext.vwQtHeaderLists
            .AsNoTracking()
            .Where(x => x.QuoteNumberIndex == 1)
            .OrderBy(x => x.QuoteNumber)
            .ThenBy(x => x.QuoteNumberIndex)
            .Take(200)
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
        return _readContext.vwQtHeaderLists
            .AsNoTracking()
            .Where(x => x.QuoteNumberIndex == 1
                        && x.Retired == false &&
                        (x.HeaderId.ToString().Contains(normalized)
                         || x.QuoteNumber.ToString().Contains(normalized)
                         || (x.PrintTitle ?? string.Empty).Contains(normalized)
                         || (x.CustomerName ?? string.Empty).Contains(normalized)))
            .OrderBy(x => x.QuoteNumber)
            .ThenBy(x => x.QuoteNumberIndex)
            .Select(Map)
            .ToList();
    }

    public (byte[] Content, string FileName)? GetPdf(Guid headerId)
    {
        var rows = _readContext.vwRptQuotations
            .AsNoTracking()
            .Where(x => x.HeaderId == headerId)
            .OrderBy(x => x.Index)
            .ToList();

        if (rows.Count == 0)
        {
            return null;
        }

        var header = rows[0];
        var remarks = _readContext.QtHeaders
            .AsNoTracking()
            .Where(x => x.HeaderId == headerId)
            .Select(x => x.Remarks)
            .SingleOrDefault();

        var contentStream = BuildContentStream(header, rows, remarks);
        var lines = new[]
        {
            "%PDF-1.4",
            "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj",
            "2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj",
            "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj",
            $"4 0 obj<</Length {contentStream.Length}>>stream\n{contentStream}\nendstream endobj",
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

        var fileName = $"quotation-{header.QuoteNumber}-{header.QuoteNumberIndex}.pdf";
        return (Encoding.ASCII.GetBytes(string.Join("\n", lines)), fileName);
    }

    public QuotationListItemResponse Create(UpsertQuotationRequest request, string actor)
    {
        // Full EF write path is a future task; not yet wired to a legacy write stored procedure.
        throw new NotSupportedException("Quotation create via EF is not yet implemented.");
    }

    public QuotationListItemResponse? Update(Guid headerId, UpsertQuotationRequest request, string actor)
    {
        // Full EF write path is a future task; not yet wired to a legacy write stored procedure.
        throw new NotSupportedException("Quotation update via EF is not yet implemented.");
    }

    private static QuotationListItemResponse Map(JB2026.EfCore.Models.vwQtHeaderList x)
    {
        return new QuotationListItemResponse
        {
            HeaderId = x.HeaderId,
            MachineType = x.MachineType.ToString(CultureInfo.InvariantCulture),
            QuoteNumber = x.QuoteNumber,
            QuoteNumberIndex = x.QuoteNumberIndex,
            QuoteNumberIndexPair = $"{x.QuoteNumber}-{x.QuoteNumberIndex}",
            QuotedOn = x.QuotedOn,
            QuotedBy = x.QuotedBy ?? string.Empty,
            ApprovedOn = x.ApprovedOn,
            ApprovedBy = x.ApprovedBy,
            PrintTitle = x.PrintTitle ?? string.Empty,
            CustomerName = x.CustomerName ?? string.Empty,
            PrintsSize = x.PrintsSize ?? string.Empty,
            PrintsColor = x.PrintsColor ?? string.Empty,
            PrintsQty = ParseDecimal(x.PrintsQty),
            MaterialName = x.MaterialName ?? string.Empty,
            MaterialCost = ParseDecimal(x.MaterialCost),
            TotalCostA = x.TotalCostA ?? 0m,
            UnitCostA = x.UnitCostA ?? 0m,
            Status = x.Status,
            CreatedOn = x.CreatedOn,
            CreatedBy = x.CreatedBy ?? string.Empty,
            ModifiedOn = x.ModifiedOn,
            ModifiedBy = x.ModifiedBy ?? string.Empty
        };
    }

    private static decimal ParseDecimal(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed)
                ? parsed
                : 0m;
    }

    private static string BuildContentStream(
        JB2026.EfCore.Models.vwRptQuotation header,
        IReadOnlyList<JB2026.EfCore.Models.vwRptQuotation> rows,
        string? remarks)
    {
        static string Escape(string value) => value.Replace("(", "[").Replace(")", "]");

        var lines = new List<string>
        {
            "BT",
            "/F1 18 Tf",
            "50 740 Td",
            $"({Escape($"Quotation {header.QuoteNumber}-{header.QuoteNumberIndex}")}) Tj",
            "0 -24 Td",
            "/F1 12 Tf",
            $"({Escape($"Customer: {header.CustomerName ?? string.Empty}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Title: {header.PrintTitle ?? string.Empty}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Print: {header.PrintsSize ?? string.Empty} / {header.PrintsColor ?? string.Empty} / Qty {header.PrintsQty ?? string.Empty}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Quoted On: {header.QuotedOn:yyyy-MM-dd}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Material: {header.MaterialName ?? string.Empty} / {header.PaperSheetSize ?? string.Empty} / Cost {ParseDecimal(header.MaterialCost):0.00}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Layout: {header.PaperSheetSizeAlias ?? string.Empty} / Format {header.PaperSizeFormat?.ToString() ?? string.Empty} / PerSheet {header.PrintsPerSheet?.ToString() ?? string.Empty}")}) Tj",
            "0 -18 Td",
            $"({Escape($"PerPage: {header.PrintsPerPage?.ToString() ?? string.Empty} {header.PrintPerPageEx ?? string.Empty} / Size {header.PageWidth ?? string.Empty} x {header.PageHeight ?? string.Empty}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Total Cost A: {(header.TotalCostA ?? 0m):0.00} / B {(header.TotalCostB ?? 0m):0.00} / C {(header.TotalCostC ?? 0m):0.00} / D {(header.TotalCostD ?? 0m):0.00}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Unit Cost A: {(header.UnitCostA ?? 0m):0.000} / B {(header.UnitCostB ?? 0m):0.000} / C {(header.UnitCostC ?? 0m):0.000} / D {(header.UnitCostD ?? 0m):0.000}")}) Tj"
        };

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            lines.Add("0 -18 Td");
            lines.Add($"({Escape($"Remarks: {remarks}")}) Tj");
        }

        foreach (var row in rows.Take(8))
        {
            var description = row.Description ?? row.ZoneName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            lines.Add("0 -18 Td");
            lines.Add($"({Escape($"{row.ZoneName ?? row.Zone ?? string.Empty}: {description} / Min {row.Minimum ?? string.Empty} / Unit {(row.UnitCost ?? 0m):0.###} / A {(row.CostA ?? 0m):0.##} / B {(row.CostB ?? 0m):0.##}")}) Tj");
        }

        lines.Add("ET");
        return string.Join("\n", lines);
    }
}