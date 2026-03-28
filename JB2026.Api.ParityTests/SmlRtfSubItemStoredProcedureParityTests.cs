using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class SmlRtfSubItemStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_sml_rtf_sub_item()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SmlRtfSubItems.AsNoTracking().FirstAsync();
        var gateway = new SmlRtfSubItemStoredProcedureGateway(readContext, writeContext);

        var subItemId = await gateway.InsertAsync(new CreateSmlRtfSubItemStoredProcedureRequest(
            ItemId: template.ItemId,
            SubLineNumber: template.SubLineNumber,
            Start_End: TrimToMaxLength(template.Start_End ?? "S", 256),
            ReferenceNumber: TrimToMaxLength($"REF-{Guid.NewGuid():N}", 32),
            LabelSize: TrimToMaxLength(template.LabelSize ?? "L", 32),
            Qty: TrimToMaxLength(template.Qty ?? "1", 10)));

        try
        {
            var procRecord = await gateway.SelectAsync(subItemId);
            var tableRecord = await readContext.SmlRtfSubItems.AsNoTracking().FirstOrDefaultAsync(x => x.SubItemId == subItemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SubItemId, procRecord!.SubItemId);
            Assert.Equal(tableRecord.ItemId, procRecord.ItemId);
            Assert.Equal(tableRecord.ReferenceNumber, procRecord.ReferenceNumber);
            Assert.Equal(tableRecord.Qty, procRecord.Qty);
        }
        finally
        {
            await gateway.DeleteAsync(subItemId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_sml_rtf_sub_item()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SmlRtfSubItems.AsNoTracking().FirstAsync();
        var gateway = new SmlRtfSubItemStoredProcedureGateway(readContext, writeContext);

        var subItemId = await gateway.InsertAsync(new CreateSmlRtfSubItemStoredProcedureRequest(
            ItemId: template.ItemId,
            SubLineNumber: template.SubLineNumber,
            Start_End: TrimToMaxLength(template.Start_End ?? "S", 256),
            ReferenceNumber: TrimToMaxLength($"REF-{Guid.NewGuid():N}", 32),
            LabelSize: TrimToMaxLength(template.LabelSize ?? "L", 32),
            Qty: TrimToMaxLength(template.Qty ?? "1", 10)));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateSmlRtfSubItemStoredProcedureRequest(
                SubItemId: subItemId,
                ItemId: template.ItemId,
                SubLineNumber: template.SubLineNumber + 1,
                Start_End: TrimToMaxLength(template.Start_End ?? "S", 256),
                ReferenceNumber: TrimToMaxLength($"REV-{Guid.NewGuid():N}", 32),
                LabelSize: TrimToMaxLength(template.LabelSize ?? "L", 32),
                Qty: TrimToMaxLength(template.Qty ?? "1", 10)));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(subItemId);
            var tableRecord = await readContext.SmlRtfSubItems.AsNoTracking().FirstOrDefaultAsync(x => x.SubItemId == subItemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SubLineNumber, procRecord!.SubLineNumber);
            Assert.Equal(tableRecord.ReferenceNumber, procRecord.ReferenceNumber);
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
