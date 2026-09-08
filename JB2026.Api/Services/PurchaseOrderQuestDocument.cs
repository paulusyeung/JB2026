using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using JB2026.Api.Models;
using JB2026.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JB2026.Api.Services;

public sealed class PurchaseOrderQuestDocument : DocumentBase<JobOrderPrintDocument>
{
    private const float InfoLabelWidth = 90f;
    private const float SectionLabelFontSize = 12f;
    private const float ImageMaxHeight = 280f;
    private const float LogoMaxHeight = 70f;

    private static readonly Lazy<byte[]?> LogoBytes = new(LoadLogoFromEmbeddedResource);

    public PurchaseOrderQuestDocument(JobOrderPrintDocument model)
        : base(model)
    {
    }

    public override DocumentMetadata GetMetadata()
    {
        return new DocumentMetadata
        {
            Title = $"工程單 {Model.OrderNumber}",
            Author = "JB2026.Api"
        };
    }

    public override void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginLeft(23);
            page.MarginRight(17);
            page.MarginTop(22);
            page.MarginBottom(34);
            page.DefaultTextStyle(LatinTextStyle);

            page.Header().Column(header =>
            {
                header.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("工程單")
                           .Style(CjkTextStyle.FontSize(20).Bold());
                        col.Item().PaddingTop(8).Row(sub =>
                        {
                            sub.ConstantItem(InfoLabelWidth)
                               .Text("工單編號：")
                               .Style(CjkTextStyle.FontSize(12));
                            sub.RelativeItem()
                               .Text(Model.OrderNumber)
                               .Style(CjkTextStyle.FontSize(12));
                        });
                    });

                    if (LogoBytes.Value is not null)
                    {
                        row.ConstantItem(64).AlignRight().AlignMiddle()
                           .Image(LogoBytes.Value).FitArea();
                    }
                });
                header.Item().PaddingBottom(4);
                header.Item().BorderBottom(0.5f);
            });

            page.Content().PaddingTop(10).Column(body =>
            {
                body.Spacing(0);
                body.Item().Element(ComposeInfoSection);
                body.Item().PaddingTop(12).Element(ComposePicturesSection);
                body.Item().PaddingTop(12).Element(ComposeContentSection);
            });

            page.Footer()
                .AlignRight()
                .Text(DateTime.Now.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture))
                .Style(LatinTextStyle.FontSize(6));
        });
    }

    // ── Info section (supplier, date, etc.) ───────────────────────────────────

    private void ComposeInfoSection(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(InfoLabelWidth);
                cols.RelativeColumn();
                cols.ConstantColumn(InfoLabelWidth);
                cols.RelativeColumn();
            });

            // Row 1: 供應商 | {supplier} | 經手人 | {orderedBy}
            AddInfoCell(table, "供應商：", Model.SupplierName);
            AddInfoCell(table, "經手人：", Model.OrderedBy);

            // Row 2: 列印日期 | {today} | 輸出檔案編號 | {outputRef}
            AddInfoCell(table, "列印日期：", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AddInfoCell(table, "輸出檔案編號：", Model.OutputRef);

            // Row 3: 主題 | {orderTitle} | (empty) | (empty)
            AddInfoCell(table, "主題：", Model.OrderTitle);
            table.Cell().Text(string.Empty);
        });
    }

    private static void AddInfoCell(TableDescriptor table, string label, string? value)
    {
        table.Cell().Padding(1).Text(label)
             .Style(CjkTextStyle.FontSize(12));
        table.Cell().Padding(1).Text(value ?? string.Empty)
             .Style(CjkTextStyle.FontSize(12));
    }

    // ── Content (selected product detail sections) ────────────────────────────

    private void ComposeContentSection(IContainer container)
    {
        container.Column(section =>
        {
            var hasPictures = !Model.NoPicture && (Model.ImageBytes is not null || Model.ImageBytes2 is not null);
            if (hasPictures)
            {
                section.Item().BorderTop(0.5f);
            }

            section.Item().PaddingTop(6).Column(col =>
            {
                col.Spacing(4);

                if (Model.SelectedProductDetailSections.Count > 0
                    && !string.IsNullOrWhiteSpace(Model.ProductDetails))
                {
                    var plainText = HtmlToPlainText(Model.ProductDetails);
                    var lines = plainText.Split('\n');

                    foreach (var title in Model.SelectedProductDetailSections)
                    {
                        var body = ExtractSectionBody(lines, title);
                        if (body is null)
                        {
                            continue;
                        }

                        col.Item().Text($"{title}：")
                           .Style(CjkTextStyle.FontSize(SectionLabelFontSize).Bold());

                        var bodyStyle = SelectTextStyle(UseCjkFallback(body))
                            .FontSize(12)
                            .LineHeight(1.0f);

                        RenderFormattedMultiline(col, body, bodyStyle);
                    }
                }
            });
        });
    }

    // ── Pictures ──────────────────────────────────────────────────────────────

    private void ComposePicturesSection(IContainer container)
    {
        if (Model.NoPicture)
        {
            return;
        }

        var hasAny = Model.ImageBytes is not null || Model.ImageBytes2 is not null;
        if (!hasAny)
        {
            return;
        }

        container.Column(section =>
        {
            section.Item().BorderTop(0.5f);

            section.Item().PaddingTop(6).Row(row =>
            {
                if (Model.ImageBytes is not null)
                {
                    row.RelativeItem().AlignCenter().MaxHeight(ImageMaxHeight)
                       .Image(Model.ImageBytes).FitArea();
                }

                if (Model.ImageBytes2 is not null)
                {
                    row.RelativeItem().AlignCenter().PaddingLeft(8).MaxHeight(ImageMaxHeight)
                       .Image(Model.ImageBytes2).FitArea();
                }
            });
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? ExtractSectionBody(string[] lines, string title)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var dotIndex = line.IndexOf('.');
            if (dotIndex <= 0)
            {
                continue;
            }

            var prefix = line[..dotIndex].Trim();
            if (!int.TryParse(prefix, out _))
            {
                continue;
            }

            var headerTitle = line[(dotIndex + 1)..].Trim().TrimEnd(':', '：').Trim();
            if (!string.Equals(headerTitle, title, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var body = new List<string>();
            for (var y = i + 1; y < lines.Length; y++)
            {
                var nextLine = lines[y].Trim();
                if (nextLine.Length == 0)
                {
                    continue;
                }

                var nextDot = nextLine.IndexOf('.');
                if (nextDot > 0)
                {
                    var nextPrefix = nextLine[..nextDot].Trim();
                    if (int.TryParse(nextPrefix, out _))
                    {
                        break;
                    }
                }

                body.Add(lines[y].TrimEnd('\r', '\n'));
            }

            return body.Count > 0 ? string.Join("\n", body) : null;
        }

        return null;
    }

    private static string HtmlToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = Regex.Replace(html, @"<\s*/?(p|div|br|li|tr|h[1-6])[^>]*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    private static bool UseCjkFallback(string? value)
    {
        return true;
    }

    private static bool IsSectionHeadingLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        return Regex.IsMatch(trimmed, @"^\d{1,3}\s*[\.)、．]\s*\S.+$")
            || Regex.IsMatch(trimmed, @"^\d{1,3}\s+\S.{0,40}[:：].*$");
    }

    private static void RenderFormattedMultiline(
        ColumnDescriptor column,
        string text,
        TextStyle baseStyle)
    {
        var lines = text.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                column.Item().Height(2);
                continue;
            }

            var style = baseStyle;
            if (IsSectionHeadingLine(line))
            {
                style = style.SemiBold();
            }

            column.Item().Text(line).Style(style);
        }
    }

    private static byte[]? LoadLogoFromEmbeddedResource()
    {
        try
        {
            var assembly = typeof(FontRegistry).Assembly;
            using var stream = assembly.GetManifestResourceStream("JB2026.Reporting.Fonts.logo.png");
            if (stream is null)
            {
                return null;
            }

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
