using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class CustomerStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_customer()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Customers.AsNoTracking().FirstAsync();
        var gateway = new CustomerStoredProcedureGateway(readContext, writeContext);

        var customerId = await gateway.InsertAsync(new CreateCustomerStoredProcedureRequest(
            CustomerName: TrimToMaxLength($"OPSX Customer {Guid.NewGuid():N}", 64),
            LoginAccount: TrimToMaxLength($"c{Guid.NewGuid():N}", 20),
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
            var procRecord = await gateway.SelectAsync(customerId);
            var tableRecord = await readContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == customerId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.CustomerId, procRecord!.CustomerId);
            Assert.Equal(tableRecord.CustomerName, procRecord.CustomerName);
            Assert.Equal(tableRecord.LoginAccount, procRecord.LoginAccount);
            Assert.Equal(tableRecord.Retired, procRecord.Retired);
        }
        finally
        {
            await gateway.DeleteAsync(customerId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_customer()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Customers.AsNoTracking().FirstAsync();
        var gateway = new CustomerStoredProcedureGateway(readContext, writeContext);

        var customerId = await gateway.InsertAsync(new CreateCustomerStoredProcedureRequest(
            CustomerName: TrimToMaxLength($"OPSX Customer U {Guid.NewGuid():N}", 64),
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
            var updated = await gateway.UpdateAsync(new UpdateCustomerStoredProcedureRequest(
                CustomerId: customerId,
                CustomerName: "OPSX Updated Customer",
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

            var procRecord = await gateway.SelectAsync(customerId);
            var tableRecord = await readContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == customerId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.CustomerName, procRecord!.CustomerName);
            Assert.Equal(tableRecord.LoginAccount, procRecord.LoginAccount);
            Assert.Equal(tableRecord.LoginPassword, procRecord.LoginPassword);
            Assert.Equal(tableRecord.Retired, procRecord.Retired);
        }
        finally
        {
            await gateway.DeleteAsync(customerId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
