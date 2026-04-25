using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface IStockProductPrintComposer
{
    Task<StockProductPrintDocument?> ComposeAsync(Guid productId, CancellationToken cancellationToken = default);
}
