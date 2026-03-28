using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class InvoiceSubItemStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_invoice_sub_item()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.InvoiceSubItems.AsNoTracking().FirstAsync();
        var itemId = template.ItemId;
        var gateway = new InvoiceSubItemStoredProcedureGateway(readContext, writeContext);

        var subItemId = await gateway.InsertAsync(new CreateInvoiceSubItemStoredProcedureRequest(
            ItemId: itemId,
            SubLineNumber: template.SubLineNumber,
            Description: TrimToMaxLength($"OPSX-ISI-{Guid.NewGuid():N}", 64),
            Quantity: template.Quantity,
            UoM: TrimToMaxLength(template.UoM ?? "ea", 10),
            Price: template.Price,
            Amount: template.Amount));

        try
        {
            var procRecord = await gateway.SelectAsync(subItemId);
            var tableRecord = await readContext.InvoiceSubItems.AsNoTracking().FirstOrDefaultAsync(x => x.SubItemId == subItemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SubItemId, procRecord!.SubItemId);
            Assert.Equal(tableRecord.ItemId, procRecord.ItemId);
            Assert.Equal(tableRecord.SubLineNumber, procRecord.SubLineNumber);
            Assert.Equal(tableRecord.Description, procRecord.Description);
            Assert.Equal(tableRecord.UoM, procRecord.UoM);
        }
        finally
        {
            await gateway.DeleteAsync(subItemId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_invoice_sub_item()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.InvoiceSubItems.AsNoTracking().FirstAsync();
        var itemId = template.ItemId;
        var gateway = new InvoiceSubItemStoredProcedureGateway(readContext, writeContext);

        var subItemId = await gateway.InsertAsync(new CreateInvoiceSubItemStoredProcedureRequest(
            ItemId: itemId,
            SubLineNumber: template.SubLineNumber,
            Description: TrimToMaxLength($"OPSX-ISI-U-{Guid.NewGuid():N}", 64),
            Quantity: template.Quantity,
            UoM: TrimToMaxLength(template.UoM ?? "ea", 10),
            Price: template.Price,
            Amount: template.Amount));

        try
        {
            var updatedDescription = TrimToMaxLength($"OPSX-ISI-V-{Guid.NewGuid():N}", 64);
            var updated = await gateway.UpdateAsync(new UpdateInvoiceSubItemStoredProcedureRequest(
                SubItemId: subItemId,
                ItemId: itemId,
                SubLineNumber: template.SubLineNumber + 1,
                Description: updatedDescription,
                Quantity: template.Quantity,
                UoM: TrimToMaxLength(template.UoM ?? "ea", 10),
                Price: template.Price,
                Amount: template.Amount));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(subItemId);
            var tableRecord = await readContext.InvoiceSubItems.AsNoTracking().FirstOrDefaultAsync(x => x.SubItemId == subItemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SubLineNumber, procRecord!.SubLineNumber);
            Assert.Equal(tableRecord.Description, procRecord.Description);
        }
        finally
        {
            await gateway.DeleteAsync(subItemId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
