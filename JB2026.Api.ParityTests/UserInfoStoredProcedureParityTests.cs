using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class UserInfoStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_user_info()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.UserInfos.AsNoTracking().FirstAsync();
        var gateway = new UserInfoStoredProcedureGateway(readContext, writeContext);
        var timestamp = DateTime.UtcNow;

        var userId = await gateway.InsertAsync(new CreateUserInfoStoredProcedureRequest(
            PrimaryRec: template.PrimaryRec,
            UserName: TrimToMaxLength($"U-{Guid.NewGuid():N}", 64),
            UserPassword: TrimToMaxLength($"P-{Guid.NewGuid():N}", 64),
            UserAlias: TrimToMaxLength($"A-{Guid.NewGuid():N}", 64),
            UserRole: template.UserRole,
            MetadataXml: template.MetadataXml,
            CreatedOn: timestamp,
            CreatedBy: template.CreatedBy,
            ModifiedOn: timestamp,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: timestamp,
            RetiredBy: template.ModifiedBy));

        try
        {
            var procRecord = await gateway.SelectAsync(userId);
            var tableRecord = await readContext.UserInfos.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.UserId, procRecord!.UserId);
            Assert.Equal(tableRecord.UserName, procRecord.UserName);
            Assert.Equal(tableRecord.UserAlias, procRecord.UserAlias);
            Assert.Equal(tableRecord.UserRole, procRecord.UserRole);
            Assert.Equal(tableRecord.Retired, procRecord.Retired);
        }
        finally
        {
            await gateway.DeleteAsync(userId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_user_info()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.UserInfos.AsNoTracking().FirstAsync();
        var gateway = new UserInfoStoredProcedureGateway(readContext, writeContext);
        var timestamp = DateTime.UtcNow;

        var userId = await gateway.InsertAsync(new CreateUserInfoStoredProcedureRequest(
            PrimaryRec: template.PrimaryRec,
            UserName: TrimToMaxLength($"U-{Guid.NewGuid():N}", 64),
            UserPassword: TrimToMaxLength($"P-{Guid.NewGuid():N}", 64),
            UserAlias: TrimToMaxLength($"A-{Guid.NewGuid():N}", 64),
            UserRole: template.UserRole,
            MetadataXml: template.MetadataXml,
            CreatedOn: timestamp,
            CreatedBy: template.CreatedBy,
            ModifiedOn: timestamp,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: timestamp,
            RetiredBy: template.ModifiedBy));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateUserInfoStoredProcedureRequest(
                UserId: userId,
                PrimaryRec: template.PrimaryRec,
                UserName: TrimToMaxLength($"UU-{Guid.NewGuid():N}", 64),
                UserPassword: TrimToMaxLength($"UP-{Guid.NewGuid():N}", 64),
                UserAlias: TrimToMaxLength($"UA-{Guid.NewGuid():N}", 64),
                UserRole: template.UserRole,
                MetadataXml: template.MetadataXml,
                CreatedOn: timestamp,
                CreatedBy: template.CreatedBy,
                ModifiedOn: timestamp.AddMinutes(1),
                ModifiedBy: template.ModifiedBy,
                Retired: false,
                RetiredOn: timestamp,
                RetiredBy: template.ModifiedBy));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(userId);
            var tableRecord = await readContext.UserInfos.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.UserName, procRecord!.UserName);
            Assert.Equal(tableRecord.UserAlias, procRecord.UserAlias);
            Assert.Equal(tableRecord.UserPassword, procRecord.UserPassword);
            Assert.Equal(tableRecord.ModifiedOn, procRecord.ModifiedOn);
        }
        finally
        {
            await gateway.DeleteAsync(userId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
