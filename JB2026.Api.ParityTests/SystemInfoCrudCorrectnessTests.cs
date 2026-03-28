using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class SystemInfoCrudCorrectnessTests
{
    [Fact]
    public async Task EfCore_crud_roundtrip_persists_expected_values_for_system_info()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.SystemInfos.AsNoTracking().FirstAsync();

        var created = new SystemInfo
        {
            SystemId = Guid.NewGuid(),
            OwnerName = TrimToMaxLength($"OPSX-OWNER-{Guid.NewGuid():N}", 255),
            MetadataXml = template.MetadataXml
        };

        writeContext.SystemInfos.Add(created);
        await writeContext.SaveChangesAsync();

        try
        {
            var inserted = await readContext.SystemInfos.AsNoTracking().FirstOrDefaultAsync(x => x.SystemId == created.SystemId);
            Assert.NotNull(inserted);
            Assert.Equal(created.OwnerName, inserted!.OwnerName);
            Assert.Equal(created.MetadataXml, inserted.MetadataXml);

            created.OwnerName = TrimToMaxLength($"OPSX-UPD-{Guid.NewGuid():N}", 255);
            writeContext.SystemInfos.Update(created);
            await writeContext.SaveChangesAsync();

            var updated = await readContext.SystemInfos.AsNoTracking().FirstOrDefaultAsync(x => x.SystemId == created.SystemId);
            Assert.NotNull(updated);
            Assert.Equal(created.OwnerName, updated!.OwnerName);
        }
        finally
        {
            var toDelete = await writeContext.SystemInfos.FirstOrDefaultAsync(x => x.SystemId == created.SystemId);
            if (toDelete is not null)
            {
                writeContext.SystemInfos.Remove(toDelete);
                await writeContext.SaveChangesAsync();
            }
        }

        var deleted = await readContext.SystemInfos.AsNoTracking().FirstOrDefaultAsync(x => x.SystemId == created.SystemId);
        Assert.Null(deleted);
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
