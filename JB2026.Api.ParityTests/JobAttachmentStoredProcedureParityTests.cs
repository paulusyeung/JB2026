using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class JobAttachmentStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_job_attachment()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        var options = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var writeContext = new JB5LegacyWriteContext(options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var orderId = await readContext.JobOrders.AsNoTracking().Select(order => order.OrderId).FirstOrDefaultAsync();
        Assert.NotEqual(Guid.Empty, orderId);

        var gateway = new JobAttachmentStoredProcedureGateway(readContext, writeContext);
        var fileName = $"opsx-parity-{Guid.NewGuid():N}.txt";

        var insertedId = await gateway.InsertAsync(new CreateJobAttachmentStoredProcedureRequest(
            OrderId: orderId,
            AttachmentType: 91,
            AttachmentIndex: 991,
            OriginalFileName: fileName));

        try
        {
            var procRecord = await gateway.SelectAsync(insertedId);
            Assert.NotNull(procRecord);

            var tableRecord = await readContext.JobAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(attachment => attachment.AttachmentId == insertedId);

            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.AttachmentId, procRecord!.AttachmentId);
            Assert.Equal(tableRecord.OrderId, procRecord.OrderId);
            Assert.Equal(tableRecord.AttachmentType, procRecord.AttachmentType);
            Assert.Equal(tableRecord.AttachmentIndex, procRecord.AttachmentIndex);
            Assert.Equal(tableRecord.OriginalFileName, procRecord.OriginalFileName);
        }
        finally
        {
            await gateway.DeleteAsync(insertedId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_job_attachment()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        var options = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var writeContext = new JB5LegacyWriteContext(options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var orderId = await readContext.JobOrders.AsNoTracking().Select(order => order.OrderId).FirstOrDefaultAsync();
        Assert.NotEqual(Guid.Empty, orderId);

        var gateway = new JobAttachmentStoredProcedureGateway(readContext, writeContext);
        var insertedId = await gateway.InsertAsync(new CreateJobAttachmentStoredProcedureRequest(
            OrderId: orderId,
            AttachmentType: 77,
            AttachmentIndex: 977,
            OriginalFileName: $"opsx-update-before-{Guid.NewGuid():N}.txt"));

        try
        {
            var updatedFileName = $"opsx-update-after-{Guid.NewGuid():N}.txt";
            var updated = await gateway.UpdateAsync(new UpdateJobAttachmentStoredProcedureRequest(
                AttachmentId: insertedId,
                OrderId: orderId,
                AttachmentType: 78,
                AttachmentIndex: 978,
                OriginalFileName: updatedFileName));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(insertedId);
            var tableRecord = await readContext.JobAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(attachment => attachment.AttachmentId == insertedId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.AttachmentType, procRecord!.AttachmentType);
            Assert.Equal(tableRecord.AttachmentIndex, procRecord.AttachmentIndex);
            Assert.Equal(tableRecord.OriginalFileName, procRecord.OriginalFileName);
        }
        finally
        {
            await gateway.DeleteAsync(insertedId);
        }
    }

}
