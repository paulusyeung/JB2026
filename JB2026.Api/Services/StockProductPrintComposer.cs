using JB2026.Api.Models;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class StockProductPrintComposer : IStockProductPrintComposer
{
    private readonly JB5LegacyReadContext _readContext;

    public StockProductPrintComposer(JB5LegacyReadContext readContext)
    {
        _readContext = readContext;
    }

    public async Task<StockProductPrintDocument?> ComposeAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _readContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ProductId == productId && !item.Retired, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var ascending = await (
            from item in _readContext.StockInOuts.AsNoTracking()
            where item.ProductId == productId
            join userInfo in _readContext.UserInfos.AsNoTracking() on item.ModifiedBy equals userInfo.UserId into userInfoGroup
            from userInfo in userInfoGroup
                .OrderByDescending(entry => entry.PrimaryRec)
                .Take(1)
                .DefaultIfEmpty()
            orderby item.InOutDate, item.ModifiedOn
            select new
            {
                item.InOutDate,
                item.Reference,
                item.Qty,
                item.ModifiedOn,
                item.ModifiedBy,
                UserName = userInfo != null ? userInfo.UserName : null,
                UserAlias = userInfo != null ? userInfo.UserAlias : null,
            })
            .ToListAsync(cancellationToken);

        var runningBalance = 0;
        var withRunningBalance = ascending.Select(item =>
        {
            runningBalance += item.Qty;
            var alias = (item.UserAlias ?? string.Empty).Trim();
            var name = (item.UserName ?? string.Empty).Trim();
            var displayName = string.IsNullOrWhiteSpace(alias) ? name : alias;

            return new
            {
                item.InOutDate,
                Reference = item.Reference ?? string.Empty,
                item.Qty,
                RunningBalance = runningBalance,
                item.ModifiedOn,
                ModifiedBy = string.IsNullOrWhiteSpace(displayName)
                    ? item.ModifiedBy.ToString("D")
                    : displayName,
            };
        });

        var orderedRows = withRunningBalance
            .OrderBy(item => item.InOutDate)
            .ThenBy(item => item.ModifiedOn)
            .ToList();

        var rows = orderedRows
            .Select((item, index) => new StockProductPrintMovementRow
            {
                RowNumber = index + 1,
                InOutDate = item.InOutDate,
                Reference = item.Reference,
                Qty = item.Qty,
                RunningBalance = item.RunningBalance,
                ModifiedOn = item.ModifiedOn,
                ModifiedBy = item.ModifiedBy,
            })
            .ToList();

        return new StockProductPrintDocument
        {
            ProductId = product.ProductId,
            StockNumber = product.StockNumber ?? string.Empty,
            ProductCode = product.ProductCode ?? string.Empty,
            ProductName = product.ProductName ?? string.Empty,
            ProductionInfo = product.Description ?? string.Empty,
            Remarks = product.Remarks ?? string.Empty,
            MOQ = product.MOQ,
            Balance = product.Balance,
            Movements = rows,
        };
    }
}
