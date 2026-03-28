using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

internal static class LegacyDbContextFactory
{
    public static JB5LegacyReadContext CreateReadContext()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseSqlServer(connectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new JB5LegacyReadContext(options);
    }

    public static JB5LegacyWriteContext CreateWriteContext()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        var options = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new JB5LegacyWriteContext(options);
    }
}
