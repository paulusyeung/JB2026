using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class SystemInfoStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_system_info()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SystemInfos.AsNoTracking().FirstAsync();
        var gateway = new SystemInfoStoredProcedureGateway(readContext, writeContext);

        var systemId = await gateway.InsertAsync(new CreateSystemInfoStoredProcedureRequest(
            OwnerName: TrimToMaxLength($"OWN-{Guid.NewGuid():N}", 255),
            MetadataXml: template.MetadataXml));

        try
        {
            var procRecord = await gateway.SelectAsync(systemId);
            var tableRecord = await readContext.SystemInfos.AsNoTracking().FirstOrDefaultAsync(x => x.SystemId == systemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SystemId, procRecord!.SystemId);
            Assert.Equal(tableRecord.OwnerName, procRecord.OwnerName);
            Assert.Equal(tableRecord.MetadataXml, procRecord.MetadataXml);
        }
        finally
        {
            await gateway.DeleteAsync(systemId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_system_info()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SystemInfos.AsNoTracking().FirstAsync();
        var gateway = new SystemInfoStoredProcedureGateway(readContext, writeContext);

        var systemId = await gateway.InsertAsync(new CreateSystemInfoStoredProcedureRequest(
            OwnerName: TrimToMaxLength($"OWN-{Guid.NewGuid():N}", 255),
            MetadataXml: template.MetadataXml));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateSystemInfoStoredProcedureRequest(
                SystemId: systemId,
                OwnerName: TrimToMaxLength($"UPD-{Guid.NewGuid():N}", 255),
                MetadataXml: template.MetadataXml));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(systemId);
            var tableRecord = await readContext.SystemInfos.AsNoTracking().FirstOrDefaultAsync(x => x.SystemId == systemId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.OwnerName, procRecord!.OwnerName);
            Assert.Equal(tableRecord.MetadataXml, procRecord.MetadataXml);
        }
        finally
        {
            await gateway.DeleteAsync(systemId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
