using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class SupplierStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_supplier()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Suppliers.AsNoTracking().FirstAsync();
        var gateway = new SupplierStoredProcedureGateway(readContext, writeContext);

        var supplierId = await gateway.InsertAsync(new CreateSupplierStoredProcedureRequest(
            SupplierName: TrimToMaxLength($"OPSX Supplier {Guid.NewGuid():N}", 64),
            LoginAccount: TrimToMaxLength($"a{Guid.NewGuid():N}", 20),
            LoginPassword: TrimToMaxLength($"p{Guid.NewGuid():N}", 20),
            MetadataXml: template.MetadataXml,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: null,
            RetiredBy: null));

        try
        {
            var procRecord = await gateway.SelectAsync(supplierId);
            var tableRecord = await readContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.SupplierId == supplierId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SupplierId, procRecord!.SupplierId);
            Assert.Equal(tableRecord.SupplierName, procRecord.SupplierName);
            Assert.Equal(tableRecord.LoginAccount, procRecord.LoginAccount);
            Assert.Equal(tableRecord.Retired, procRecord.Retired);
        }
        finally
        {
            await gateway.DeleteAsync(supplierId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_supplier()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Suppliers.AsNoTracking().FirstAsync();
        var gateway = new SupplierStoredProcedureGateway(readContext, writeContext);

        var supplierId = await gateway.InsertAsync(new CreateSupplierStoredProcedureRequest(
            SupplierName: TrimToMaxLength($"OPSX Supplier U {Guid.NewGuid():N}", 64),
            LoginAccount: TrimToMaxLength($"u{Guid.NewGuid():N}", 20),
            LoginPassword: TrimToMaxLength($"v{Guid.NewGuid():N}", 20),
            MetadataXml: template.MetadataXml,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: null,
            RetiredBy: null));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateSupplierStoredProcedureRequest(
                SupplierId: supplierId,
                SupplierName: "OPSX Updated Supplier",
                LoginAccount: TrimToMaxLength($"z{Guid.NewGuid():N}", 20),
                LoginPassword: TrimToMaxLength($"y{Guid.NewGuid():N}", 20),
                MetadataXml: template.MetadataXml,
                CreatedOn: template.CreatedOn,
                CreatedBy: template.CreatedBy,
                ModifiedOn: DateTime.UtcNow,
                ModifiedBy: template.ModifiedBy,
                Retired: false,
                RetiredOn: null,
                RetiredBy: null));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(supplierId);
            var tableRecord = await readContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.SupplierId == supplierId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SupplierName, procRecord!.SupplierName);
            Assert.Equal(tableRecord.LoginAccount, procRecord.LoginAccount);
            Assert.Equal(tableRecord.LoginPassword, procRecord.LoginPassword);
            Assert.Equal(tableRecord.Retired, procRecord.Retired);
        }
        finally
        {
            await gateway.DeleteAsync(supplierId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
