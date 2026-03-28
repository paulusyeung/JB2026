using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class CustomerCrudCorrectnessTests
{
    [Fact]
    public async Task EfCore_crud_roundtrip_persists_expected_values_for_customer()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.Customers.AsNoTracking().FirstAsync();
        var timestamp = DateTime.UtcNow;

        var created = new Customer
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = TrimToMaxLength($"OPSX-CUST-{Guid.NewGuid():N}", 128),
            LoginAccount = TrimToMaxLength($"ACC-{Guid.NewGuid():N}", 64),
            LoginPassword = TrimToMaxLength($"PWD-{Guid.NewGuid():N}", 64),
            MetadataXml = template.MetadataXml,
            CreatedOn = timestamp,
            CreatedBy = template.CreatedBy,
            ModifiedOn = timestamp,
            ModifiedBy = template.ModifiedBy,
            Retired = false
        };

        writeContext.Customers.Add(created);
        await writeContext.SaveChangesAsync();

        try
        {
            var inserted = await readContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CustomerId == created.CustomerId);
            Assert.NotNull(inserted);
            Assert.Equal(created.CustomerName, inserted!.CustomerName);
            Assert.Equal(created.LoginAccount, inserted.LoginAccount);
            Assert.Equal(created.Retired, inserted.Retired);

            created.CustomerName = TrimToMaxLength($"OPSX-CUST-UPD-{Guid.NewGuid():N}", 128);
            created.ModifiedOn = timestamp.AddMinutes(1);
            created.Retired = true;
            created.RetiredOn = timestamp.AddMinutes(1);
            created.RetiredBy = template.ModifiedBy;
            writeContext.Customers.Update(created);
            await writeContext.SaveChangesAsync();

            var updated = await readContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CustomerId == created.CustomerId);
            Assert.NotNull(updated);
            Assert.Equal(created.CustomerName, updated!.CustomerName);
            Assert.True(updated.Retired);
            Assert.Equal(template.ModifiedBy, updated.RetiredBy);
        }
        finally
        {
            var toDelete = await writeContext.Customers.FirstOrDefaultAsync(x => x.CustomerId == created.CustomerId);
            if (toDelete is not null)
            {
                writeContext.Customers.Remove(toDelete);
                await writeContext.SaveChangesAsync();
            }
        }

        var deleted = await readContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CustomerId == created.CustomerId);
        Assert.Null(deleted);
    }

    private static string TrimToMaxLength(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
