using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class StockInOutStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_stock_in_out()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.StockInOuts.AsNoTracking().FirstAsync();
        var productId = template.ProductId ?? await readContext.Products.AsNoTracking().Select(x => x.ProductId).FirstAsync();
        var gateway = new StockInOutStoredProcedureGateway(readContext, writeContext);

        var inOutId = await gateway.InsertAsync(new CreateStockInOutStoredProcedureRequest(
            ProductId: productId,
            InOutDate: template.InOutDate,
            Reference: TrimToMaxLength($"OPSX-STOCK-{Guid.NewGuid():N}", 32),
            Qty: template.Qty,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy));

        try
        {
            var procRecord = await gateway.SelectAsync(inOutId);
            var tableRecord = await readContext.StockInOuts.AsNoTracking().FirstOrDefaultAsync(x => x.InOutId == inOutId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.InOutId, procRecord!.InOutId);
            Assert.Equal(tableRecord.ProductId, procRecord.ProductId);
            Assert.Equal(tableRecord.Reference, procRecord.Reference);
            Assert.Equal(tableRecord.Qty, procRecord.Qty);
        }
        finally
        {
            await gateway.DeleteAsync(inOutId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_stock_in_out()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.StockInOuts.AsNoTracking().FirstAsync();
        var productId = template.ProductId ?? await readContext.Products.AsNoTracking().Select(x => x.ProductId).FirstAsync();
        var gateway = new StockInOutStoredProcedureGateway(readContext, writeContext);

        var inOutId = await gateway.InsertAsync(new CreateStockInOutStoredProcedureRequest(
            ProductId: productId,
            InOutDate: template.InOutDate,
            Reference: TrimToMaxLength($"OPSX-STOCK-U-{Guid.NewGuid():N}", 32),
            Qty: template.Qty,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateStockInOutStoredProcedureRequest(
                InOutId: inOutId,
                ProductId: productId,
                InOutDate: template.InOutDate,
                Reference: TrimToMaxLength($"OPSX-STOCK-V-{Guid.NewGuid():N}", 32),
                Qty: template.Qty + 1,
                CreatedOn: template.CreatedOn,
                CreatedBy: template.CreatedBy,
                ModifiedOn: DateTime.UtcNow,
                ModifiedBy: template.ModifiedBy));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(inOutId);
            var tableRecord = await readContext.StockInOuts.AsNoTracking().FirstOrDefaultAsync(x => x.InOutId == inOutId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.Reference, procRecord!.Reference);
            Assert.Equal(tableRecord.Qty, procRecord.Qty);
            Assert.Equal(tableRecord.ModifiedBy, procRecord.ModifiedBy);
        }
        finally
        {
            await gateway.DeleteAsync(inOutId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
