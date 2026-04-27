using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JB2026.Reporting;

public enum ReportTextAlign
{
    Left,
    Center,
    Right
}

public sealed record ReportColumn(string Header, float RelativeWidth, ReportTextAlign Alignment = ReportTextAlign.Left);

public sealed record ReportCell(string Value, ReportTextAlign Alignment = ReportTextAlign.Left, bool UseCjkFallback = false);

public static class ReportTable
{
    public static void Render(
        IContainer container,
        IReadOnlyList<ReportColumn> columns,
        IReadOnlyList<IReadOnlyList<ReportCell>> rows,
        Func<bool, bool, TextStyle> styleSelector)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columnDefinition =>
            {
                foreach (var column in columns)
                {
                    columnDefinition.RelativeColumn(column.RelativeWidth);
                }
            });

            table.Header(header =>
            {
                foreach (var column in columns)
                {
                    header.Cell()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten1)
                        .Background(Colors.Blue.Medium)
                        .PaddingVertical(6)
                        .PaddingHorizontal(5)
                        .AlignMiddle()
                        .AlignCenter()
                        .Text(column.Header)
                        .Style(styleSelector(false, false))
                        .FontColor(Colors.White)
                        .SemiBold();
                }
            });

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var useAltBackground = rowIndex % 2 == 1;

                for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                {
                    var cell = columnIndex < row.Count ? row[columnIndex] : new ReportCell(string.Empty);
                    table.Cell()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Background(useAltBackground ? Colors.Grey.Lighten5 : Colors.White)
                        .PaddingVertical(4)
                        .PaddingHorizontal(5)
                        .AlignMiddle()
                        .Element(element => ApplyAlignment(element, cell.Alignment))
                        .Text(cell.Value)
                        .Style(styleSelector(true, cell.UseCjkFallback));
                }
            }
        });
    }

    private static IContainer ApplyAlignment(IContainer container, ReportTextAlign alignment)
    {
        return alignment switch
        {
            ReportTextAlign.Center => container.AlignCenter(),
            ReportTextAlign.Right => container.AlignRight(),
            _ => container.AlignLeft()
        };
    }
}
