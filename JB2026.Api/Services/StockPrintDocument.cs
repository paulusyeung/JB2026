using System.Globalization;
using JB2026.Api.Models;
using JB2026.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JB2026.Api.Services;

public sealed class StockPrintDocument : DocumentBase<StockProductPrintDocument>
{
    private static readonly IReadOnlyList<ReportColumn> MovementColumns =
    [
        new ReportColumn("#", 0.7f, ReportTextAlign.Center),
        new ReportColumn("Date", 1.7f, ReportTextAlign.Left),
        new ReportColumn("Reference", 5.0f, ReportTextAlign.Left),
        new ReportColumn("Quantity", 1.7f, ReportTextAlign.Right),
        new ReportColumn("Balance", 1.8f, ReportTextAlign.Right),
        new ReportColumn("Modified On", 2.2f, ReportTextAlign.Left),
        new ReportColumn("Modified By", 1.8f, ReportTextAlign.Left)
    ];

    public StockPrintDocument(StockProductPrintDocument model)
        : base(model)
    {
    }

    public override DocumentMetadata GetMetadata()
    {
        return new DocumentMetadata
        {
            Title = "Stock Record Movement Report",
            Author = "JB2026.Api"
        };
    }

    public override void Compose(IDocumentContainer container)
    {
        var orderedRows = GetOrderedRows();

        container.Page(page =>
        {
            page.Size(new PageSize(PageLayout.Width, PageLayout.Height));
            page.MarginLeft(PageLayout.MarginLeft);
            page.MarginRight(PageLayout.MarginRight);
            page.MarginTop(PageLayout.MarginTop);
            page.MarginBottom(PageLayout.MarginBottom);
            page.DefaultTextStyle(LatinTextStyle);

            page.Header().Column(column =>
            {
                column.Spacing(6);
                column.Item()
                    .Row(row =>
                    {
                        row.RelativeItem().Text("Stock Record Movement Report").FontSize(14).SemiBold();
                        row.ConstantItem(220).AlignRight().Text($"Printed On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(10);
                    });

                column.Item().Element(ComposeIdentitySection);
                column.Item().Element(ComposeSummarySection);
            });

            page.Content()
                .PaddingTop(10)
                .Element(content =>
                {
                    var movementCells = orderedRows.Select(row => (IReadOnlyList<ReportCell>)
                    [
                        new ReportCell(row.RowNumber.ToString(CultureInfo.InvariantCulture), ReportTextAlign.Center),
                        new ReportCell(row.InOutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                        new ReportCell(row.Reference, ReportTextAlign.Left, UseCjkFallback(row.Reference)),
                        new ReportCell(row.Qty.ToString("#,##0", CultureInfo.InvariantCulture), ReportTextAlign.Right),
                        new ReportCell(row.RunningBalance.ToString("#,##0", CultureInfo.InvariantCulture), ReportTextAlign.Right),
                        new ReportCell(row.ModifiedOn.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
                        new ReportCell(row.ModifiedBy, ReportTextAlign.Left, UseCjkFallback(row.ModifiedBy))
                    ]).ToList();

                    ReportTable.Render(
                        content,
                        MovementColumns,
                        movementCells,
                        (_, useCjkFallback) => SelectTextStyle(useCjkFallback));
                });

            page.Footer()
                .AlignRight()
                .Text(text =>
                {
                    text.Span("Page ").FontSize(10);
                    text.CurrentPageNumber().FontSize(10);
                    text.Span(" of ").FontSize(10);
                    text.TotalPages().FontSize(10);
                });
        });
    }

    private void ComposeIdentitySection(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(3);
            column.Item().Element(e => ComposeLabelValue(e, "Stock Number:", FormatStockNumber(Model.StockNumber)));
            column.Item().Element(e => ComposeLabelValue(e, "Product Code:", Model.ProductCode, UseCjkFallback(Model.ProductCode)));
            column.Item().Element(e => ComposeLabelValue(e, "Product Name:", Model.ProductName, UseCjkFallback(Model.ProductName)));
        });
    }

    private void ComposeSummarySection(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(3);
            column.Item().Element(e => ComposeLabelValue(e, "Production Info:", Model.ProductionInfo, UseCjkFallback(Model.ProductionInfo)));
            column.Item().Element(e => ComposeLabelValue(e, "Remarks:", Model.Remarks, UseCjkFallback(Model.Remarks)));
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(e => ComposeLabelValue(e, "MOQ:", Model.MOQ.ToString("#,##0", CultureInfo.InvariantCulture)));
                row.RelativeItem().Element(e => ComposeLabelValue(e, "Balance:", Model.Balance.ToString("#,##0", CultureInfo.InvariantCulture)));
            });
        });
    }

    private static void ComposeLabelValue(IContainer container, string label, string value, bool useCjkFallback = false)
    {
        container.Row(row =>
        {
            row.ConstantItem(110).Text(label).SemiBold();
            row.RelativeItem().Text(value).Style(SelectTextStyle(useCjkFallback));
        });
    }

    private IReadOnlyList<StockProductPrintMovementRow> GetOrderedRows()
    {
        return Model.Movements
            .OrderBy(row => row.InOutDate)
            .ThenBy(row => row.ModifiedOn)
            .Select((row, index) => new StockProductPrintMovementRow
            {
                RowNumber = index + 1,
                InOutDate = row.InOutDate,
                Reference = row.Reference,
                Qty = row.Qty,
                RunningBalance = row.RunningBalance,
                ModifiedOn = row.ModifiedOn,
                ModifiedBy = row.ModifiedBy
            })
            .ToList();
    }

    private static string FormatStockNumber(string stockNumber)
    {
        if (string.IsNullOrEmpty(stockNumber))
        {
            return string.Empty;
        }

        return stockNumber.Length >= 11
            ? string.Format("{0}-{1}-{2}", stockNumber.Substring(0, 3), stockNumber.Substring(3, 3), stockNumber.Substring(6))
            : stockNumber;
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
