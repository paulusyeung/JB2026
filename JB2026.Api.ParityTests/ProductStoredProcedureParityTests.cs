using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class ProductStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_product()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Products.AsNoTracking().FirstAsync();
        var gateway = new ProductStoredProcedureGateway(readContext, writeContext);

        var productId = await gateway.InsertAsync(new CreateProductStoredProcedureRequest(
            CategoryId: template.CategoryId,
            StockNumber: TrimToMaxLength($"SN-{Guid.NewGuid():N}", 32),
            ProductCode: TrimToMaxLength($"PC-{Guid.NewGuid():N}", 32),
            ProductName: TrimToMaxLength($"OPSX Product {Guid.NewGuid():N}", 64),
            Description: template.Description,
            Remarks: template.Remarks,
            MOQ: template.MOQ,
            Balance: template.Balance,
            SellingPrice: template.SellingPrice,
            COGS: template.COGS,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: null,
            RetiredBy: null));

        try
        {
            var procRecord = await gateway.SelectAsync(productId);
            var tableRecord = await readContext.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.ProductId, procRecord!.ProductId);
            Assert.Equal(tableRecord.ProductCode, procRecord.ProductCode);
            Assert.Equal(tableRecord.ProductName, procRecord.ProductName);
            Assert.Equal(tableRecord.SellingPrice, procRecord.SellingPrice);
        }
        finally
        {
            await gateway.DeleteAsync(productId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_product()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Products.AsNoTracking().FirstAsync();
        var gateway = new ProductStoredProcedureGateway(readContext, writeContext);

        var productId = await gateway.InsertAsync(new CreateProductStoredProcedureRequest(
            CategoryId: template.CategoryId,
            StockNumber: TrimToMaxLength($"SU-{Guid.NewGuid():N}", 32),
            ProductCode: TrimToMaxLength($"PU-{Guid.NewGuid():N}", 32),
            ProductName: TrimToMaxLength($"OPSX Product U {Guid.NewGuid():N}", 64),
            Description: template.Description,
            Remarks: template.Remarks,
            MOQ: template.MOQ,
            Balance: template.Balance,
            SellingPrice: template.SellingPrice,
            COGS: template.COGS,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: null,
            RetiredBy: null));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateProductStoredProcedureRequest(
                ProductId: productId,
                CategoryId: template.CategoryId,
                StockNumber: TrimToMaxLength($"SV-{Guid.NewGuid():N}", 32),
                ProductCode: TrimToMaxLength($"PV-{Guid.NewGuid():N}", 32),
                ProductName: "OPSX Updated Product",
                Description: template.Description,
                Remarks: "OPSX parity update",
                MOQ: template.MOQ + 1,
                Balance: template.Balance + 1,
                SellingPrice: template.SellingPrice,
                COGS: template.COGS,
                CreatedOn: template.CreatedOn,
                CreatedBy: template.CreatedBy,
                ModifiedOn: DateTime.UtcNow,
                ModifiedBy: template.ModifiedBy,
                Retired: false,
                RetiredOn: null,
                RetiredBy: null));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(productId);
            var tableRecord = await readContext.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.ProductCode, procRecord!.ProductCode);
            Assert.Equal(tableRecord.ProductName, procRecord.ProductName);
            Assert.Equal(tableRecord.MOQ, procRecord.MOQ);
            Assert.Equal(tableRecord.Balance, procRecord.Balance);
        }
        finally
        {
            await gateway.DeleteAsync(productId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
