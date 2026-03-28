using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class InvoiceItemStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_invoice_item()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.InvoiceItems.AsNoTracking().FirstAsync();
        var headerId = template.HeaderId;
        var smlRtfHeaderId = template.SmlRtfHeaderId ?? await readContext.SmlRtfHeaders.AsNoTracking().Select(x => x.HeaderId).FirstAsync();
        var gateway = new InvoiceItemStoredProcedureGateway(readContext, writeContext);

        var itemId = await gateway.InsertAsync(new CreateInvoiceItemStoredProcedureRequest(
            HeaderId: headerId,
            SmlRtfHeaderId: smlRtfHeaderId,
            LineNumber: template.LineNumber,
            Notes: TrimToMaxLength($"OPSX-INV-ITEM-{Guid.NewGuid():N}", 128)));

        try
        {
            var procRecord = await gateway.SelectAsync(itemId);
            var tableRecord = await readContext.InvoiceItems.AsNoTracking().FirstOrDefaultAsync(x => x.ItemId == itemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.ItemId, procRecord!.ItemId);
            Assert.Equal(tableRecord.HeaderId, procRecord.HeaderId);
            Assert.Equal(tableRecord.SmlRtfHeaderId, procRecord.SmlRtfHeaderId);
            Assert.Equal(tableRecord.Notes, procRecord.Notes);
        }
        finally
        {
            await gateway.DeleteAsync(itemId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_invoice_item()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.InvoiceItems.AsNoTracking().FirstAsync();
        var headerId = template.HeaderId;
        var smlRtfHeaderId = template.SmlRtfHeaderId ?? await readContext.SmlRtfHeaders.AsNoTracking().Select(x => x.HeaderId).FirstAsync();
        var gateway = new InvoiceItemStoredProcedureGateway(readContext, writeContext);

        var itemId = await gateway.InsertAsync(new CreateInvoiceItemStoredProcedureRequest(
            HeaderId: headerId,
            SmlRtfHeaderId: smlRtfHeaderId,
            LineNumber: template.LineNumber,
            Notes: TrimToMaxLength($"OPSX-INV-U-{Guid.NewGuid():N}", 128)));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateInvoiceItemStoredProcedureRequest(
                ItemId: itemId,
                HeaderId: headerId,
                SmlRtfHeaderId: smlRtfHeaderId,
                LineNumber: template.LineNumber + 1,
                Notes: TrimToMaxLength($"OPSX-INV-V-{Guid.NewGuid():N}", 128)));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(itemId);
            var tableRecord = await readContext.InvoiceItems.AsNoTracking().FirstOrDefaultAsync(x => x.ItemId == itemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.LineNumber, procRecord!.LineNumber);
            Assert.Equal(tableRecord.Notes, procRecord.Notes);
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
