using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class SmlRtfExtractToDNStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_sml_rtf_extract_to_dn()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SmlRtfExtractToDNs.AsNoTracking().FirstAsync();
        var gateway = new SmlRtfExtractToDNStoredProcedureGateway(readContext, writeContext);

        var dnId = await gateway.InsertAsync(new CreateSmlRtfExtractToDNStoredProcedureRequest(
            HeaderId: template.HeaderId,
            DNNumber: TrimToMaxLength($"DN-{Guid.NewGuid():N}", 16),
            DNDate: template.DNDate,
            DNType: template.DNType,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy));

        try
        {
            var procRecord = await gateway.SelectAsync(dnId);
            var tableRecord = await readContext.SmlRtfExtractToDNs.AsNoTracking().FirstOrDefaultAsync(x => x.DNId == dnId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.DNId, procRecord!.DNId);
            Assert.Equal(tableRecord.HeaderId, procRecord.HeaderId);
            Assert.Equal(tableRecord.DNNumber, procRecord.DNNumber);
            Assert.Equal(tableRecord.DNType, procRecord.DNType);
        }
        finally
        {
            await gateway.DeleteAsync(dnId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_sml_rtf_extract_to_dn()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.SmlRtfExtractToDNs.AsNoTracking().FirstAsync();
        var gateway = new SmlRtfExtractToDNStoredProcedureGateway(readContext, writeContext);

        var dnId = await gateway.InsertAsync(new CreateSmlRtfExtractToDNStoredProcedureRequest(
            HeaderId: template.HeaderId,
            DNNumber: TrimToMaxLength($"DN-{Guid.NewGuid():N}", 16),
            DNDate: template.DNDate,
            DNType: template.DNType,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateSmlRtfExtractToDNStoredProcedureRequest(
                DNId: dnId,
                HeaderId: template.HeaderId,
                DNNumber: TrimToMaxLength($"DU-{Guid.NewGuid():N}", 16),
                DNDate: template.DNDate.AddDays(1),
                DNType: (template.DNType ?? 0) + 1,
                CreatedOn: template.CreatedOn,
                CreatedBy: template.CreatedBy));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(dnId);
            var tableRecord = await readContext.SmlRtfExtractToDNs.AsNoTracking().FirstOrDefaultAsync(x => x.DNId == dnId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.DNNumber, procRecord!.DNNumber);
            Assert.Equal(tableRecord.DNDate, procRecord.DNDate);
            Assert.Equal(tableRecord.DNType, procRecord.DNType);
        }
        finally
        {
            await gateway.DeleteAsync(dnId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
