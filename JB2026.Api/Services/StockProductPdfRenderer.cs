using JB2026.Api.Models;
using QuestPDF.Fluent;

namespace JB2026.Api.Services;

public sealed class StockProductPdfRenderer : IStockProductPdfRenderer
{
    public byte[] Render(StockProductPrintDocument document)
    {
        var report = new StockPrintDocument(document);
        return report.GeneratePdf();
    }
}
