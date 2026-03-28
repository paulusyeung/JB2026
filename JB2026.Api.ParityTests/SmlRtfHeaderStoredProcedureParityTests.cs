using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class SmlRtfHeaderStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_sml_rtf_header()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SmlRtfHeaders.AsNoTracking().FirstAsync();
        var gateway = new SmlRtfHeaderStoredProcedureGateway(readContext, writeContext);

        var headerId = await gateway.InsertAsync(new CreateSmlRtfHeaderStoredProcedureRequest(
            RtfFileName: TrimToMaxLength($"OPSX-RTF-{Guid.NewGuid():N}", 256),
            PurchaseOrder: TrimToMaxLength($"PO{Guid.NewGuid():N}", 16),
            CustomerPO: TrimToMaxLength($"CPO{Guid.NewGuid():N}", 16),
            OrderedOn: template.OrderedOn,
            OrderedBy: TrimToMaxLength(template.OrderedBy ?? "opsx", 32),
            OriginalPO: TrimToMaxLength(template.OriginalPO ?? "opo", 16),
            SalesOrder: TrimToMaxLength(template.SalesOrder ?? "so", 16),
            OriginalSO: TrimToMaxLength(template.OriginalSO ?? "oso", 16),
            Remarks: TrimToMaxLength($"OPSX-RMK-{Guid.NewGuid():N}", 512),
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy ?? template.ModifiedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: template.Retired,
            RetiredOn: template.RetiredOn,
            RetiredBy: template.RetiredBy ?? template.ModifiedBy));

        try
        {
            var procRecord = await gateway.SelectAsync(headerId);
            var tableRecord = await readContext.SmlRtfHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.HeaderId == headerId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.HeaderId, procRecord!.HeaderId);
            Assert.Equal(tableRecord.RtfFileName, procRecord.RtfFileName);
            Assert.Equal(tableRecord.PurchaseOrder, procRecord.PurchaseOrder);
            Assert.Equal(tableRecord.CustomerPO, procRecord.CustomerPO);
        }
        finally
        {
            await gateway.DeleteAsync(headerId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_sml_rtf_header()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SmlRtfHeaders.AsNoTracking().FirstAsync();
        var gateway = new SmlRtfHeaderStoredProcedureGateway(readContext, writeContext);

        var headerId = await gateway.InsertAsync(new CreateSmlRtfHeaderStoredProcedureRequest(
            RtfFileName: TrimToMaxLength($"OPSX-RTF-U-{Guid.NewGuid():N}", 256),
            PurchaseOrder: TrimToMaxLength($"PO{Guid.NewGuid():N}", 16),
            CustomerPO: TrimToMaxLength($"CPO{Guid.NewGuid():N}", 16),
            OrderedOn: template.OrderedOn,
            OrderedBy: TrimToMaxLength(template.OrderedBy ?? "opsx", 32),
            OriginalPO: TrimToMaxLength(template.OriginalPO ?? "opo", 16),
            SalesOrder: TrimToMaxLength(template.SalesOrder ?? "so", 16),
            OriginalSO: TrimToMaxLength(template.OriginalSO ?? "oso", 16),
            Remarks: TrimToMaxLength($"OPSX-RMK-U-{Guid.NewGuid():N}", 512),
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy ?? template.ModifiedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: template.Retired,
            RetiredOn: template.RetiredOn,
            RetiredBy: template.RetiredBy ?? template.ModifiedBy));

        try
        {
            var updatedRemarks = TrimToMaxLength($"OPSX-RMK-V-{Guid.NewGuid():N}", 512);
            var updated = await gateway.UpdateAsync(new UpdateSmlRtfHeaderStoredProcedureRequest(
                HeaderId: headerId,
                RtfFileName: TrimToMaxLength($"OPSX-RTF-V-{Guid.NewGuid():N}", 256),
                PurchaseOrder: TrimToMaxLength($"PV{Guid.NewGuid():N}", 16),
                CustomerPO: TrimToMaxLength($"CPV{Guid.NewGuid():N}", 16),
                OrderedOn: template.OrderedOn,
                OrderedBy: TrimToMaxLength(template.OrderedBy ?? "opsx", 32),
                OriginalPO: TrimToMaxLength(template.OriginalPO ?? "opo", 16),
                SalesOrder: TrimToMaxLength(template.SalesOrder ?? "so", 16),
                OriginalSO: TrimToMaxLength(template.OriginalSO ?? "oso", 16),
                Remarks: updatedRemarks,
                CreatedOn: template.CreatedOn,
                CreatedBy: template.CreatedBy ?? template.ModifiedBy,
                ModifiedOn: template.ModifiedOn,
                ModifiedBy: template.ModifiedBy,
                Retired: template.Retired,
                RetiredOn: template.RetiredOn,
                RetiredBy: template.RetiredBy ?? template.ModifiedBy));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(headerId);
            var tableRecord = await readContext.SmlRtfHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.HeaderId == headerId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.RtfFileName, procRecord!.RtfFileName);
            Assert.Equal(tableRecord.PurchaseOrder, procRecord.PurchaseOrder);
            Assert.Equal(tableRecord.Remarks, procRecord.Remarks);
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
