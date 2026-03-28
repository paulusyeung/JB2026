using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class SmlRtfItemStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_sml_rtf_item()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SmlRtfItems.AsNoTracking().FirstAsync();
        var gateway = new SmlRtfItemStoredProcedureGateway(readContext, writeContext);

        var itemId = await gateway.InsertAsync(new CreateSmlRtfItemStoredProcedureRequest(
            HeaderId: template.HeaderId,
            LineNumber: template.LineNumber,
            ProductCode: TrimToMaxLength($"P-{Guid.NewGuid():N}", 128),
            ProductDescription: TrimToMaxLength($"PD-{Guid.NewGuid():N}", 256),
            Price: TrimToMaxLength(template.Price ?? "0", 16),
            Discount: TrimToMaxLength(template.Discount ?? "0", 16),
            Qty: TrimToMaxLength(template.Qty ?? "1", 16),
            Amount: TrimToMaxLength(template.Amount ?? "0", 16),
            PostProcess: TrimToMaxLength(template.PostProcess ?? "", 64)));

        try
        {
            var procRecord = await gateway.SelectAsync(itemId);
            var tableRecord = await readContext.SmlRtfItems.AsNoTracking().FirstOrDefaultAsync(x => x.ItemId == itemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.ItemId, procRecord!.ItemId);
            Assert.Equal(tableRecord.HeaderId, procRecord.HeaderId);
            Assert.Equal(tableRecord.ProductCode, procRecord.ProductCode);
            Assert.Equal(tableRecord.ProductDescription, procRecord.ProductDescription);
        }
        finally
        {
            await gateway.DeleteAsync(itemId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_sml_rtf_item()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SmlRtfItems.AsNoTracking().FirstAsync();
        var gateway = new SmlRtfItemStoredProcedureGateway(readContext, writeContext);

        var itemId = await gateway.InsertAsync(new CreateSmlRtfItemStoredProcedureRequest(
            HeaderId: template.HeaderId,
            LineNumber: template.LineNumber,
            ProductCode: TrimToMaxLength($"P-{Guid.NewGuid():N}", 128),
            ProductDescription: TrimToMaxLength($"PD-{Guid.NewGuid():N}", 256),
            Price: TrimToMaxLength(template.Price ?? "0", 16),
            Discount: TrimToMaxLength(template.Discount ?? "0", 16),
            Qty: TrimToMaxLength(template.Qty ?? "1", 16),
            Amount: TrimToMaxLength(template.Amount ?? "0", 16),
            PostProcess: TrimToMaxLength(template.PostProcess ?? "", 64)));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateSmlRtfItemStoredProcedureRequest(
                ItemId: itemId,
                HeaderId: template.HeaderId,
                LineNumber: template.LineNumber + 1,
                ProductCode: TrimToMaxLength($"PV-{Guid.NewGuid():N}", 128),
                ProductDescription: TrimToMaxLength($"PDV-{Guid.NewGuid():N}", 256),
                Price: TrimToMaxLength(template.Price ?? "0", 16),
                Discount: TrimToMaxLength(template.Discount ?? "0", 16),
                Qty: TrimToMaxLength(template.Qty ?? "1", 16),
                Amount: TrimToMaxLength(template.Amount ?? "0", 16),
                PostProcess: TrimToMaxLength(template.PostProcess ?? "", 64)));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(itemId);
            var tableRecord = await readContext.SmlRtfItems.AsNoTracking().FirstOrDefaultAsync(x => x.ItemId == itemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.LineNumber, procRecord!.LineNumber);
            Assert.Equal(tableRecord.ProductCode, procRecord.ProductCode);
        }
        finally
        {
            await gateway.DeleteAsync(itemId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
