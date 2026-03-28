using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class ZFormStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_z_form()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_Forms.AsNoTracking().FirstAsync();
        var gateway = new ZFormStoredProcedureGateway(readContext, writeContext);

        var formId = await gateway.InsertAsync(new CreateZFormStoredProcedureRequest(
            FormObjectEnum: template.FormObjectEnum,
            FormName: TrimToMaxLength($"OP{Guid.NewGuid():N}"[..10], 10),
            FormName_Chs: TrimToMaxLength($"C{Guid.NewGuid():N}"[..10], 10),
            FormName_Cht: TrimToMaxLength($"T{Guid.NewGuid():N}"[..10], 10),
            MetadataXml: template.MetadataXml));

        try
        {
            var procRecord = await gateway.SelectAsync(formId);
            var tableRecord = await readContext.Z_Forms.AsNoTracking().FirstOrDefaultAsync(x => x.FormId == formId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.FormId, procRecord!.FormId);
            Assert.Equal(tableRecord.FormObjectEnum, procRecord.FormObjectEnum);
            Assert.Equal(tableRecord.FormName, procRecord.FormName);
            Assert.Equal(tableRecord.FormName_Chs, procRecord.FormName_Chs);
            Assert.Equal(tableRecord.FormName_Cht, procRecord.FormName_Cht);
        }
        finally
        {
            await gateway.DeleteAsync(formId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_z_form()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_Forms.AsNoTracking().FirstAsync();
        var gateway = new ZFormStoredProcedureGateway(readContext, writeContext);

        var formId = await gateway.InsertAsync(new CreateZFormStoredProcedureRequest(
            FormObjectEnum: template.FormObjectEnum,
            FormName: TrimToMaxLength($"OP{Guid.NewGuid():N}"[..10], 10),
            FormName_Chs: TrimToMaxLength($"C{Guid.NewGuid():N}"[..10], 10),
            FormName_Cht: TrimToMaxLength($"T{Guid.NewGuid():N}"[..10], 10),
            MetadataXml: template.MetadataXml));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateZFormStoredProcedureRequest(
                FormId: formId,
                FormObjectEnum: template.FormObjectEnum,
                FormName: TrimToMaxLength($"UP{Guid.NewGuid():N}"[..10], 10),
                FormName_Chs: TrimToMaxLength($"UC{Guid.NewGuid():N}"[..10], 10),
                FormName_Cht: TrimToMaxLength($"UT{Guid.NewGuid():N}"[..10], 10),
                MetadataXml: template.MetadataXml));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(formId);
            var tableRecord = await readContext.Z_Forms.AsNoTracking().FirstOrDefaultAsync(x => x.FormId == formId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.FormName, procRecord!.FormName);
            Assert.Equal(tableRecord.FormName_Chs, procRecord.FormName_Chs);
            Assert.Equal(tableRecord.FormName_Cht, procRecord.FormName_Cht);
        }
        finally
        {
            await gateway.DeleteAsync(formId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
