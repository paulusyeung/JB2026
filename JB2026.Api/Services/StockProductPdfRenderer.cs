using System.Globalization;
using System.Text;
using JB2026.Api.Models;
using Microsoft.Extensions.Configuration;

namespace JB2026.Api.Services;

public sealed class StockProductPdfRenderer : IStockProductPdfRenderer
{
    private readonly string _fontName;

    public StockProductPdfRenderer(IConfiguration configuration)
    {
        _fontName = (configuration["StockPrint:FontName"] ?? "Helvetica").Trim();
        if (string.IsNullOrWhiteSpace(_fontName))
        {
            _fontName = "Helvetica";
        }
    }

    public byte[] Render(StockProductPrintDocument document)
    {
        const decimal firstPageTableTop = 970m;
        const decimal subsequentPageTableTop = 1090m;
        const decimal tableBottom = 90m;
        const decimal rowHeight = 22m;
        const decimal headerHeight = 26m;

        var firstPageCapacity = CalculatePageCapacity(firstPageTableTop, headerHeight, tableBottom, rowHeight);
        var subsequentPageCapacity = CalculatePageCapacity(subsequentPageTableTop, headerHeight, tableBottom, rowHeight);
        var pagedRows = PaginateRows(document.Movements, firstPageCapacity, subsequentPageCapacity);

        if (pagedRows.Count == 0)
        {
            pagedRows.Add([]);
        }

        var printedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var totalPages = pagedRows.Count;

        var objectMap = new Dictionary<int, string>
        {
            [1] = "<< /Type /Catalog /Pages 2 0 R >>",
            [3] = $"<< /Type /Font /Subtype /Type1 /BaseFont /{EscapeName(_fontName)} >>",
            [4] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
            [5] = "<< /Type /Font /Subtype /Type0 /BaseFont /STSong-Light /Encoding /UniGB-UCS2-H /DescendantFonts [6 0 R] >>",
            [6] = "<< /Type /Font /Subtype /CIDFontType0 /BaseFont /STSong-Light /CIDSystemInfo << /Registry (Adobe) /Ordering (GB1) /Supplement 4 >> /DW 1000 >>",
            [7] = "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"
        };

        var pageObjectIds = new List<int>(totalPages);
        for (var pageIndex = 0; pageIndex < totalPages; pageIndex++)
        {
            var pageNumber = pageIndex + 1;
            var includeDetails = pageIndex == 0;
            var tableTop = includeDetails ? firstPageTableTop : subsequentPageTableTop;
            var contentStream = BuildContentStream(document, pagedRows[pageIndex], pageNumber, totalPages, printedOn, includeDetails, tableTop);

            var pageObjectId = 8 + pageIndex * 2;
            var contentObjectId = pageObjectId + 1;
            pageObjectIds.Add(pageObjectId);

            objectMap[pageObjectId] = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 1191] /Contents {contentObjectId} 0 R /Resources << /Font << /F1 3 0 R /F1B 4 0 R /F2 5 0 R /F3 7 0 R >> >> >>";
            objectMap[contentObjectId] = $"<< /Length {Encoding.UTF8.GetByteCount(contentStream)} >>\nstream\n{contentStream}\nendstream";
        }

        var kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
        objectMap[2] = $"<< /Type /Pages /Count {totalPages} /Kids [{kids}] >>";

        var maxObjectId = objectMap.Keys.Max();

        var builder = new StringBuilder();
        builder.AppendLine("%PDF-1.4");

        var offsets = new Dictionary<int, int>();
        for (var objectId = 1; objectId <= maxObjectId; objectId++)
        {
            offsets[objectId] = Encoding.UTF8.GetByteCount(builder.ToString());
            builder.AppendLine($"{objectId} 0 obj");
            builder.AppendLine(objectMap[objectId]);
            builder.AppendLine("endobj");
        }

        var xrefOffset = Encoding.UTF8.GetByteCount(builder.ToString());
        builder.AppendLine("xref");
        builder.AppendLine($"0 {maxObjectId + 1}");
        builder.AppendLine("0000000000 65535 f ");

        for (var objectId = 1; objectId <= maxObjectId; objectId++)
        {
            builder.AppendLine($"{offsets[objectId]:D10} 00000 n ");
        }

        builder.AppendLine("trailer");
        builder.AppendLine($"<< /Size {maxObjectId + 1} /Root 1 0 R >>");
        builder.AppendLine("startxref");
        builder.AppendLine(xrefOffset.ToString());
        builder.AppendLine("%%EOF");

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string BuildContentStream(
        StockProductPrintDocument document,
        IReadOnlyList<StockProductPrintMovementRow> rows,
        int pageNumber,
        int totalPages,
        string printedOn,
        bool includeDetails,
        decimal tableTop)
    {
        const decimal pageLeft = 40m;
        const decimal pageTop = 1145m;
        const decimal tableWidth = 762m;
        const decimal headerLeftInset = 10m;
        const decimal headerRightInset = 10m;
        const decimal rowHeight = 22m;
        const decimal headerHeight = 26m;

        var columnWidths = new[] { 36m, 86m, 254m, 86m, 96m, 120m, 84m };
        var columnStarts = new decimal[columnWidths.Length];
        var runningX = pageLeft;
        for (var i = 0; i < columnWidths.Length; i++)
        {
            columnStarts[i] = runningX;
            runningX += columnWidths[i];
        }

        var lines = new List<string>
        {
            DrawLatinText("F1B", 13m, pageLeft + headerLeftInset, pageTop - 8m, "Stock Record Movement Report"),
            DrawRightAlignedMonospaceTextByRightEdge("F3", 10.5m, pageLeft + tableWidth - headerRightInset, pageTop - 4m, $"Printed On: {printedOn}"),
            DrawRightAlignedTextByRightEdge("F1", 10.5m, pageLeft + tableWidth - headerRightInset, pageTop - 22m, $"Page Number: {pageNumber} of {totalPages}"),

            "0.18 0.26 0.40 rg",
            $"{FormatNumber(pageLeft)} {FormatNumber(tableTop - headerHeight)} {FormatNumber(tableWidth)} {FormatNumber(headerHeight)} re f",
            "1 1 1 rg",
            DrawCenteredLatinText("F1B", 10.5m, columnStarts[0], columnWidths[0], tableTop - 17m, "#"),
            DrawCenteredLatinText("F1B", 10.5m, columnStarts[1], columnWidths[1], tableTop - 17m, "Date"),
            DrawCenteredLatinText("F1B", 10.5m, columnStarts[2], columnWidths[2], tableTop - 17m, "Reference"),
            DrawCenteredLatinText("F1B", 10.5m, columnStarts[3], columnWidths[3], tableTop - 17m, "Quantity"),
            DrawCenteredLatinText("F1B", 10.5m, columnStarts[4], columnWidths[4], tableTop - 17m, "Balance"),
            DrawCenteredLatinText("F1B", 10.5m, columnStarts[5], columnWidths[5], tableTop - 17m, "Modified On"),
            DrawCenteredLatinText("F1B", 10.5m, columnStarts[6], columnWidths[6], tableTop - 17m, "Modified By"),

            "0.65 0.70 0.78 RG",
            $"{FormatNumber(pageLeft)} {FormatNumber(tableTop - headerHeight)} {FormatNumber(tableWidth)} {FormatNumber(headerHeight + rows.Count * rowHeight)} re S"
        };

        if (includeDetails)
        {
            lines.InsertRange(3,
            [
                DrawLabelAndValue(pageLeft + headerLeftInset, pageTop - 42m, "Stock Number:", document.StockNumber),
                DrawLabelAndValue(pageLeft + headerLeftInset, pageTop - 64m, "Product Code:", document.ProductCode),
                DrawLabelAndValue(pageLeft + headerLeftInset, pageTop - 86m, "Product Name:", document.ProductName),
                DrawLabelAndValue(pageLeft + headerLeftInset, pageTop - 108m, "Production Info:", document.ProductionInfo),
                DrawLabelAndValue(pageLeft + headerLeftInset, pageTop - 130m, "Remarks:", document.Remarks),
                DrawLabelAndValue(pageLeft + headerLeftInset, pageTop - 152m, "MOQ:", document.MOQ.ToString("#,##0", CultureInfo.InvariantCulture)),
                DrawLabelAndValue(pageLeft + headerLeftInset + 140m, pageTop - 152m, "Balance:", document.Balance.ToString("#,##0", CultureInfo.InvariantCulture)),
            ]);
        }

        for (var i = 1; i < columnWidths.Length; i++)
        {
            lines.Add($"{FormatNumber(columnStarts[i])} {FormatNumber(tableTop - headerHeight)} m {FormatNumber(columnStarts[i])} {FormatNumber(tableTop - headerHeight - rows.Count * rowHeight)} l S");
        }

        // Draw explicit outer vertical borders to avoid missing start/end lines.
        lines.Add($"{FormatNumber(pageLeft)} {FormatNumber(tableTop - headerHeight)} m {FormatNumber(pageLeft)} {FormatNumber(tableTop - headerHeight - rows.Count * rowHeight)} l S");
        lines.Add($"{FormatNumber(pageLeft + tableWidth)} {FormatNumber(tableTop - headerHeight)} m {FormatNumber(pageLeft + tableWidth)} {FormatNumber(tableTop - headerHeight - rows.Count * rowHeight)} l S");

        for (var rowIndex = 0; rowIndex <= rows.Count; rowIndex++)
        {
            var y = tableTop - headerHeight - rowIndex * rowHeight;
            lines.Add($"{FormatNumber(pageLeft)} {FormatNumber(y)} m {FormatNumber(pageLeft + tableWidth)} {FormatNumber(y)} l S");
        }

        for (var pageRowIndex = 0; pageRowIndex < rows.Count; pageRowIndex++)
        {
            var row = rows[pageRowIndex];
            var top = tableTop - headerHeight - pageRowIndex * rowHeight;
            var baselineY = top - 14m;
            var qtyText = row.Qty.ToString("#,##0", CultureInfo.InvariantCulture);
            var balanceText = row.RunningBalance.ToString("#,##0", CultureInfo.InvariantCulture);

            lines.Add("0.10 0.10 0.10 rg");
            lines.Add(DrawLatinText("F1", 10m, columnStarts[0] + 6m, baselineY, row.RowNumber.ToString()));
            lines.Add(DrawLatinText("F1", 10m, columnStarts[1] + 6m, baselineY, row.InOutDate.ToString("yyyy-MM-dd")));
            lines.Add(DrawSmartText(10m, columnStarts[2] + 6m, baselineY, Truncate(row.Reference, 30)));

            lines.Add(DrawRightAlignedMonospaceText("F3", 10m, columnStarts[3], columnWidths[3], baselineY, qtyText));
            lines.Add(DrawRightAlignedMonospaceText("F3", 10m, columnStarts[4], columnWidths[4], baselineY, balanceText));
            lines.Add(DrawLatinText("F1", 10m, columnStarts[5] + 6m, baselineY, row.ModifiedOn.ToString("yyyy-MM-dd HH:mm")));
            lines.Add(DrawSmartText(10m, columnStarts[6] + 6m, baselineY, Truncate(row.ModifiedBy, 12)));
        }

        return string.Join("\n", lines);
    }

    private static int CalculatePageCapacity(decimal tableTop, decimal headerHeight, decimal tableBottom, decimal rowHeight)
    {
        return Math.Max((int)Math.Floor((tableTop - headerHeight - tableBottom) / rowHeight), 0);
    }

    private static List<IReadOnlyList<StockProductPrintMovementRow>> PaginateRows(
        IReadOnlyList<StockProductPrintMovementRow> movements,
        int firstPageCapacity,
        int subsequentPageCapacity)
    {
        var pages = new List<IReadOnlyList<StockProductPrintMovementRow>>();
        var totalRows = movements.Count;
        var cursor = 0;

        if (totalRows == 0)
        {
            return pages;
        }

        var firstTake = Math.Min(firstPageCapacity, totalRows);
        pages.Add(movements.Skip(cursor).Take(firstTake).ToList());
        cursor += firstTake;

        while (cursor < totalRows)
        {
            var take = Math.Min(subsequentPageCapacity, totalRows - cursor);
            pages.Add(movements.Skip(cursor).Take(take).ToList());
            cursor += take;
        }

        return pages;
    }

    private static string DrawLatinText(string fontAlias, decimal fontSize, decimal x, decimal y, string text)
    {
        return $"BT /{fontAlias} {FormatNumber(fontSize)} Tf 1 0 0 1 {FormatNumber(x)} {FormatNumber(y)} Tm ({EscapeText(text)}) Tj ET";
    }

    private static string DrawUtf16Text(string fontAlias, decimal fontSize, decimal x, decimal y, string text)
    {
        return $"BT /{fontAlias} {FormatNumber(fontSize)} Tf 1 0 0 1 {FormatNumber(x)} {FormatNumber(y)} Tm <{ToUtf16Hex(text)}> Tj ET";
    }

    private static string DrawLabelAndValue(decimal x, decimal y, string label, string value)
    {
        var safeLabel = NormalizeForPdf(label);
        var safeValue = NormalizeForPdf(value);
        var labelWidth = MeasureLatinText(safeLabel, 11m);
        return string.Join(
            "\n",
            DrawLatinText("F1", 11m, x, y, safeLabel),
            DrawSmartText(11m, x + labelWidth + 6m, y, safeValue));
    }

    private static string DrawSmartText(decimal fontSize, decimal x, decimal y, string text)
    {
        return ContainsNonAscii(text)
            ? DrawUtf16Text("F2", fontSize, x, y, text)
            : DrawLatinText("F1", fontSize, x, y, text);
    }

    private static string DrawRightAlignedLatinText(string fontAlias, decimal fontSize, decimal columnStart, decimal columnWidth, decimal y, string text)
    {
        var width = MeasureLatinText(text, fontSize);
        var x = columnStart + columnWidth - 6m - width;
        return DrawLatinText(fontAlias, fontSize, x, y, text);
    }

    private static string DrawRightAlignedMonospaceText(string fontAlias, decimal fontSize, decimal columnStart, decimal columnWidth, decimal y, string text)
    {
        var normalized = NormalizeForPdf(text);
        var width = MeasureMonospaceText(normalized, fontSize);
        var x = columnStart + columnWidth - 6m - width;
        return DrawLatinText(fontAlias, fontSize, x, y, normalized);
    }

    private static string DrawCenteredLatinText(string fontAlias, decimal fontSize, decimal columnStart, decimal columnWidth, decimal y, string text)
    {
        var width = MeasureLatinText(text, fontSize);
        var x = columnStart + (columnWidth - width) / 2m;
        return DrawLatinText(fontAlias, fontSize, x, y, text);
    }

    private static string DrawRightAlignedTextByRightEdge(string fontAlias, decimal fontSize, decimal rightEdgeX, decimal y, string text)
    {
        var width = MeasureLatinText(text, fontSize);
        var x = rightEdgeX - width;
        return DrawLatinText(fontAlias, fontSize, x, y, text);
    }

    private static string DrawRightAlignedMonospaceTextByRightEdge(string fontAlias, decimal fontSize, decimal rightEdgeX, decimal y, string text)
    {
        var normalized = NormalizeForPdf(text);
        var width = MeasureMonospaceText(normalized, fontSize);
        var x = rightEdgeX - width;
        return DrawLatinText(fontAlias, fontSize, x, y, normalized);
    }

    private static decimal MeasureLatinText(string text, decimal fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0m;
        }

        // Helvetica average width approximation is sufficient for tabular right alignment.
        return text.Length * fontSize * 0.53m;
    }

    private static decimal MeasureMonospaceText(string text, decimal fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0m;
        }

        return text.Length * fontSize * 0.6m;
    }

    private static string EscapeText(string value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private static string ToUtf16Hex(string value)
    {
        var normalized = NormalizeForPdf(value);

        var bytes = Encoding.BigEndianUnicode.GetBytes(normalized);
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("X2"));
        }

        return builder.ToString();
    }

    private static bool ContainsNonAscii(string value)
    {
        foreach (var ch in NormalizeForPdf(value))
        {
            if (ch > 0x7F)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeForPdf(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace('\r', ' ').Replace('\n', ' ');
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (char.IsControl(ch) || ch == '\uFFFD')
            {
                builder.Append(' ');
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString().Trim();
    }

    private static string EscapeName(string value)
    {
        return (value ?? string.Empty)
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("#", string.Empty);
    }

    private static string FormatNumber(decimal value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string Truncate(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..Math.Max(0, maxLength - 1)] + "~";
    }
}
