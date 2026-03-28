using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class DbContextFactoryIntegrationTests
{
    [Fact]
    public async Task Factory_creates_read_and_write_contexts_for_real_test_database()
    {
        await using var readContext = LegacyDbContextFactory.CreateReadContext();
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();

        var readProbe = await readContext.SystemInfos.AsNoTracking().Take(1).ToListAsync();
        var writeProbe = await writeContext.SystemInfos.AsNoTracking().Take(1).ToListAsync();

        Assert.NotNull(readProbe);
        Assert.NotNull(writeProbe);
    }
}
