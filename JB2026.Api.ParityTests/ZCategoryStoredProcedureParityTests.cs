using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class ZCategoryStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_z_category()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_Categories.AsNoTracking().FirstAsync();
        var gateway = new ZCategoryStoredProcedureGateway(readContext, writeContext);

        var categoryId = await gateway.InsertAsync(new CreateZCategoryStoredProcedureRequest(
            CategoryCode: TrimToMaxLength("OPX", 3),
            CategoryName: TrimToMaxLength($"OPSX-CAT-{Guid.NewGuid():N}", 64),
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: template.Retired,
            RetiredOn: template.RetiredOn,
            RetiredBy: template.RetiredBy));

        try
        {
            var procRecord = await gateway.SelectAsync(categoryId);
            var tableRecord = await readContext.Z_Categories.AsNoTracking().FirstOrDefaultAsync(x => x.CategoryId == categoryId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.CategoryId, procRecord!.CategoryId);
            Assert.Equal(tableRecord.CategoryCode, procRecord.CategoryCode);
            Assert.Equal(tableRecord.CategoryName, procRecord.CategoryName);
            Assert.Equal(tableRecord.Retired, procRecord.Retired);
        }
        finally
        {
            await gateway.DeleteAsync(categoryId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_z_category()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_Categories.AsNoTracking().FirstAsync();
        var gateway = new ZCategoryStoredProcedureGateway(readContext, writeContext);

        var categoryId = await gateway.InsertAsync(new CreateZCategoryStoredProcedureRequest(
            CategoryCode: TrimToMaxLength("OPX", 3),
            CategoryName: TrimToMaxLength($"OPSX-CAT-U-{Guid.NewGuid():N}", 64),
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: template.Retired,
            RetiredOn: template.RetiredOn,
            RetiredBy: template.RetiredBy));

        try
        {
            var updatedName = TrimToMaxLength($"OPSX-CAT-V-{Guid.NewGuid():N}", 64);
            var updated = await gateway.UpdateAsync(new UpdateZCategoryStoredProcedureRequest(
                CategoryId: categoryId,
                CategoryCode: TrimToMaxLength("OPV", 3),
                CategoryName: updatedName,
                CreatedOn: template.CreatedOn,
                CreatedBy: template.CreatedBy,
                ModifiedOn: template.ModifiedOn,
                ModifiedBy: template.ModifiedBy,
                Retired: template.Retired,
                RetiredOn: template.RetiredOn,
                RetiredBy: template.RetiredBy));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(categoryId);
            var tableRecord = await readContext.Z_Categories.AsNoTracking().FirstOrDefaultAsync(x => x.CategoryId == categoryId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.CategoryCode, procRecord!.CategoryCode);
            Assert.Equal(tableRecord.CategoryName, procRecord.CategoryName);
        }
        finally
        {
            await gateway.DeleteAsync(categoryId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
