using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using JB2026.Api.Models;
using JB2026.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JB2026.Api.Services;

public sealed class JobOrderQuestDocument : DocumentBase<JobOrderPrintDocument>
{
    private const float HeaderLabelWidth1 = 74f;
    private const float HeaderLabelWidth2 = 102f;
    private const float HeaderLabelWidth3 = 102f;
    private const float HeaderValueWidth1 = 150f;
    private const float HeaderValueWidth2 = 50f;
    private const float ContentLabelWidth = 58f;
    private const float WorkInstLabelWidth = 74f;
    private const float SectionLabelFontSize = 14f;
    private const float ImageMaxHeight = 280f;

    public JobOrderQuestDocument(JobOrderPrintDocument model)
        : base(model)
    {
    }

    public override DocumentMetadata GetMetadata()
    {
        return new DocumentMetadata
        {
            Title = $"生產單 {Model.OrderNumber}",
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
                    row.RelativeItem()
                       .Text("生產單")
                       .Style(CjkTextStyle.FontSize(20).Bold());
                });
                header.Item().PaddingTop(20).Element(ComposeHeaderTable);
                header.Item().BorderBottom(0.5f);
            });

            page.Content().PaddingTop(10).Column(body =>
            {
                body.Spacing(0);
                body.Item().Element(ComposeContentSection);
                body.Item().PaddingTop(20).Element(ComposeWorkInstructionsSection);
                body.Item().PaddingTop(28).Element(ComposeRemarksSection);
            });

            page.Footer()
                .AlignRight()
                .Text(DateTime.Now.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture))
                .Style(LatinTextStyle.FontSize(14));
        });
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void ComposeHeaderTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(HeaderLabelWidth1);
                cols.ConstantColumn(HeaderValueWidth1);
                cols.ConstantColumn(HeaderLabelWidth2);
                cols.ConstantColumn(HeaderValueWidth2);
                cols.ConstantColumn(HeaderLabelWidth3);
                cols.RelativeColumn();
            });

            // Row 1: 工單編號 | 工單類別 | 制單日期
            AddHeaderCell(table, "工單編號：", Model.OrderNumber, HeaderLabelWidth1);
            AddHeaderCell(table, "工單類別：", Model.PaymentTerms, HeaderLabelWidth2);
            AddHeaderCell(table, "制單日期：", FmtDate(Model.OrderedOn), HeaderLabelWidth3);

            // Row 2: 客戶姓名 | 經手人 | 修改日期
            AddHeaderCell(table, "客戶姓名：", Model.CustomerName, HeaderLabelWidth1, cjk: UseCjkFallback(Model.CustomerName));
            AddHeaderCell(table, "經手人：", Model.OrderedBy, HeaderLabelWidth2, cjk: UseCjkFallback(Model.OrderedBy));
            AddHeaderCell(table, "修改日期：", FmtDate(Model.ModifiedOn), HeaderLabelWidth3);

            // Row 3: 主題名稱 | (empty) | 要求出貨日期
            AddHeaderCell(table, "主題名稱：", Model.OrderTitle, HeaderLabelWidth1, cjk: UseCjkFallback(Model.OrderTitle));
            AddHeaderCell(table, string.Empty, string.Empty, HeaderLabelWidth2);
            AddHeaderCell(table, "要求出貨日期：", FmtDate(Model.RequiredOn), HeaderLabelWidth3, labelCjk: true);

            // Row 4: 採購訂單 | 數量 | (empty)
            AddHeaderCell(table, "採購訂單：", Model.CustomerRef, HeaderLabelWidth1, cjk: UseCjkFallback(Model.CustomerRef));
            AddHeaderCell(table, "數量：", FmtQty(Model.Qty), HeaderLabelWidth2);
            AddHeaderCell(table, string.Empty, string.Empty, HeaderLabelWidth3);

            // Row 5: 成品代號 | 輸出檔案編號 | (empty)
            AddHeaderCell(table, "成品代號：", Model.ProductCode, HeaderLabelWidth1);
            AddHeaderCell(table, "輸出檔案編號：", Model.InvoiceRef, HeaderLabelWidth2, labelCjk: true);
            AddHeaderCell(table, string.Empty, string.Empty, HeaderLabelWidth3);
        });
    }

    private static void AddHeaderCell(TableDescriptor table, string label, string? value,
        float labelWidth, bool cjk = false, bool labelCjk = false)
    {
        var useCjkLabel = labelCjk || UseCjkFallback(label);

        table.Cell().Padding(1).AlignRight().Text(label)
               .Style(useCjkLabel ? CjkTextStyle : LatinTextStyle);
        table.Cell().Text(value ?? string.Empty)
             .Style(cjk ? CjkTextStyle : SelectTextStyle(UseCjkFallback(value)));
    }

    // ── Content (內容) ─────────────────────────────────────────────────────────

    private void ComposeContentSection(IContainer container)
    {
        container.Row(outer =>
        {
            outer.ConstantItem(ContentLabelWidth)
                 .Text("內容：")
                 .Style(CjkTextStyle.SemiBold());

            outer.RelativeItem().Row(inner =>
            {
                inner.RelativeItem().Column(textCol =>
                {
                    textCol.Spacing(1);

                    if (!Model.NoProductDetails && !string.IsNullOrWhiteSpace(Model.ProductDetails))
                    {
                        var plain = NormalizePrintText(HtmlToPlainText(Model.ProductDetails));
                        if (!string.IsNullOrWhiteSpace(plain))
                        {
                            var productTextStyle = SelectTextStyle(UseCjkFallback(plain))
                                .FontSize(ResolveFontSizeFromHtml(Model.ProductDetails, 14f))
                                .LineHeight(1.4f);

                            RenderFormattedMultiline(textCol, plain, productTextStyle, emphasizeSectionHeadings: false);
                        }
                    }

                    foreach (var wf in Model.Workflows)
                    {
                        if (!string.IsNullOrWhiteSpace(wf.WorkInstruction))
                        {
                            var instr = NormalizePrintText(HtmlToPlainText(wf.WorkInstruction));
                            if (!string.IsNullOrWhiteSpace(instr))
                            {
                                var instructionStyle = SelectTextStyle(UseCjkFallback(instr)).FontSize(14).LineHeight(1.4f);
                                RenderFormattedMultiline(textCol, instr, instructionStyle, emphasizeSectionHeadings: false);
                            }
                        }
                    }
                });

                if (!Model.NoPicture && Model.ImageBytes is not null)
                {
                    inner.RelativeItem().PaddingLeft(8).AlignTop()
                         .MaxHeight(ImageMaxHeight)
                         .Image(Model.ImageBytes).FitArea();
                }
            });
        });
    }

    // ── 工作指引 ────────────────────────────────────────────────────────────────

    private void ComposeWorkInstructionsSection(IContainer container)
    {
        container.Column(section =>
        {
            section.Item().BorderTop(0.5f);

            section.Item().PaddingTop(6).Row(row =>
            {
                row.ConstantItem(WorkInstLabelWidth)
                   .Text("工作指引：")
                   .Style(CjkTextStyle.FontSize(SectionLabelFontSize).SemiBold());

                row.RelativeItem().MinHeight(48).Column(col =>
                {
                    col.Spacing(3);
                    foreach (var wf in Model.Workflows)
                    {
                        if (!string.IsNullOrWhiteSpace(wf.WorkNotes))
                        {
                            var notes = NormalizePrintText(HtmlToPlainText(wf.WorkNotes));
                            if (!string.IsNullOrWhiteSpace(notes))
                            {
                                col.Item().Text(notes)
                                   .Style(SelectTextStyle(UseCjkFallback(notes)).FontSize(14).LineHeight(1.4f));
                            }
                        }
                    }
                });
            });
        });
    }

    // ── 備註 ───────────────────────────────────────────────────────────────────

    private void ComposeRemarksSection(IContainer container)
    {
        container.Column(section =>
        {
            section.Item().BorderTop(0.5f);

            section.Item().PaddingTop(8).Row(row =>
            {
                row.ConstantItem(ContentLabelWidth)
                   .Text("備註：")
                   .Style(CjkTextStyle.FontSize(SectionLabelFontSize).SemiBold());

                row.RelativeItem().MinHeight(36).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(Model.Remarks))
                    {
                        var remarks = NormalizePrintText(Model.Remarks);
                        col.Item().Text(remarks)
                           .Style(SelectTextStyle(UseCjkFallback(remarks)).FontSize(14).LineHeight(1.4f));
                    }
                });
            });
        });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string FmtDate(DateTime? dt) =>
        dt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FmtQty(decimal? qty) =>
        qty.HasValue ? qty.Value.ToString("#,##0.##", CultureInfo.InvariantCulture) : string.Empty;

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

    private static string NormalizePrintText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var output = new List<string>(lines.Length);
        var previousBlank = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var isBlank = string.IsNullOrWhiteSpace(line);

            if (isBlank)
            {
                if (!previousBlank)
                {
                    output.Add(string.Empty);
                }
            }
            else
            {
                output.Add(line);
            }

            previousBlank = isBlank;
        }

        while (output.Count > 0 && string.IsNullOrWhiteSpace(output[0]))
        {
            output.RemoveAt(0);
        }

        while (output.Count > 0 && string.IsNullOrWhiteSpace(output[^1]))
        {
            output.RemoveAt(output.Count - 1);
        }

        return string.Join("\n", output);
    }

    private static float ResolveFontSizeFromHtml(string? html, float fallback)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return fallback;
        }

        var maxPt = 0f;

        foreach (Match match in Regex.Matches(html, @"font-size\s*:\s*(\d+(?:\.\d+)?)\s*px", RegexOptions.IgnoreCase))
        {
            if (!match.Success)
            {
                continue;
            }

            if (!float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var px))
            {
                continue;
            }

            var pt = px * 0.75f;
            if (pt > maxPt)
            {
                maxPt = pt;
            }
        }

        foreach (Match match in Regex.Matches(html, @"font-size\s*:\s*(\d+(?:\.\d+)?)\s*pt", RegexOptions.IgnoreCase))
        {
            if (!match.Success)
            {
                continue;
            }

            if (!float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pt))
            {
                continue;
            }

            if (pt > maxPt)
            {
                maxPt = pt;
            }
        }

        if (maxPt <= 0f)
        {
            return fallback;
        }

        return Math.Clamp(maxPt, 8f, 16f);
    }

    private static bool IsSectionHeadingLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        // Legacy print details usually start sections with numbered markers like
        // "1.印刷內容:", "5.數碼印刷: Fuji", or "9.過膠（文膜）".
        if (Regex.IsMatch(trimmed, @"^\d{1,3}\s*[\.)、．]\s*\S.+$"))
        {
            return true;
        }

        // Some source text drops the dot after OCR/import, but still keeps the
        // section-like pattern "{number} {title}: ...".
        return Regex.IsMatch(trimmed, @"^\d{1,3}\s+\S.{0,40}[:：].*$");
    }

    private static void RenderFormattedMultiline(
        ColumnDescriptor column,
        string text,
        TextStyle baseStyle,
        bool emphasizeSectionHeadings)
    {
        var lines = text.Split('\n');
        var hasRenderedContent = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                column.Item().Height(2);
                continue;
            }

            var style = baseStyle;
            if (emphasizeSectionHeadings && IsSectionHeadingLine(line))
            {
                // Match legacy layout: keep one empty line between numbered sections.
                if (hasRenderedContent)
                {
                    column.Item().Height(8);
                }
                style = style.SemiBold();
            }

            column.Item().Text(line).Style(style);
            hasRenderedContent = true;
        }
    }

    private static bool UseCjkFallback(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (ch > 127)
            {
                return true;
            }
        }

        return false;
    }
}
