using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class InvoiceHeaderStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_invoice_header()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.InvoiceHeaders.AsNoTracking().FirstAsync();
        var customerId = template.CustomerId ?? await readContext.Customers.AsNoTracking().Select(x => x.CustomerId).FirstAsync();
        var gateway = new InvoiceHeaderStoredProcedureGateway(readContext, writeContext);

        var headerId = await gateway.InsertAsync(new CreateInvoiceHeaderStoredProcedureRequest(
            CustomerId: customerId,
            BillTo: template.BillTo,
            ShipTo: template.ShipTo,
            InvoiceDate: template.InvoiceDate,
            InvoiceNumber: TrimToMaxLength($"I{Guid.NewGuid():N}", 10),
            InvoiceAmount: template.InvoiceAmount,
            ICNumber: template.ICNumber,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: null,
            RetiredBy: null));

        try
        {
            var procRecord = await gateway.SelectAsync(headerId);
            var tableRecord = await readContext.InvoiceHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.HeaderId == headerId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.HeaderId, procRecord!.HeaderId);
            Assert.Equal(tableRecord.CustomerId, procRecord.CustomerId);
            Assert.Equal(tableRecord.InvoiceNumber, procRecord.InvoiceNumber);
            Assert.Equal(tableRecord.InvoiceAmount, procRecord.InvoiceAmount);
        }
        finally
        {
            await gateway.DeleteAsync(headerId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_invoice_header()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.InvoiceHeaders.AsNoTracking().FirstAsync();
        var customerId = template.CustomerId ?? await readContext.Customers.AsNoTracking().Select(x => x.CustomerId).FirstAsync();
        var gateway = new InvoiceHeaderStoredProcedureGateway(readContext, writeContext);

        var headerId = await gateway.InsertAsync(new CreateInvoiceHeaderStoredProcedureRequest(
            CustomerId: customerId,
            BillTo: template.BillTo,
            ShipTo: template.ShipTo,
            InvoiceDate: template.InvoiceDate,
            InvoiceNumber: TrimToMaxLength($"U{Guid.NewGuid():N}", 10),
            InvoiceAmount: template.InvoiceAmount,
            ICNumber: template.ICNumber,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: null,
            RetiredBy: null));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateInvoiceHeaderStoredProcedureRequest(
                HeaderId: headerId,
                CustomerId: customerId,
                BillTo: template.BillTo,
                ShipTo: template.ShipTo,
                InvoiceDate: template.InvoiceDate,
                InvoiceNumber: TrimToMaxLength($"V{Guid.NewGuid():N}", 10),
                InvoiceAmount: template.InvoiceAmount,
                ICNumber: TrimToMaxLength("OPSX-IC-UPDATED", 32),
                CreatedOn: template.CreatedOn,
                CreatedBy: template.CreatedBy,
                ModifiedOn: DateTime.UtcNow,
                ModifiedBy: template.ModifiedBy,
                Retired: false,
                RetiredOn: null,
                RetiredBy: null));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(headerId);
            var tableRecord = await readContext.InvoiceHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.HeaderId == headerId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.InvoiceNumber, procRecord!.InvoiceNumber);
            Assert.Equal(tableRecord.ICNumber, procRecord.ICNumber);
            Assert.Equal(tableRecord.ModifiedBy, procRecord.ModifiedBy);
        }
        finally
        {
            await gateway.DeleteAsync(headerId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
