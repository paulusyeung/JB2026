using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface IStockProductPdfRenderer
{
    byte[] Render(StockProductPrintDocument document);
}
