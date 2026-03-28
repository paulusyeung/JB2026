using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class ProductAttachmentStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_product_attachment()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var productId = await readContext.Products.AsNoTracking().Select(x => x.ProductId).FirstOrDefaultAsync();
        Assert.NotEqual(Guid.Empty, productId);

        var gateway = new ProductAttachmentStoredProcedureGateway(readContext, writeContext);
        var fileName = TrimToMaxLength($"opsx-product-attachment-{Guid.NewGuid():N}.txt", 255);

        var attachmentId = await gateway.InsertAsync(new CreateProductAttachmentStoredProcedureRequest(
            ProductId: productId,
            AttachmentIndex: 401,
            OriginalFileName: fileName));

        try
        {
            var procRecord = await gateway.SelectAsync(attachmentId);
            var tableRecord = await readContext.ProductAttachments.AsNoTracking().FirstOrDefaultAsync(x => x.AttachmentId == attachmentId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.AttachmentId, procRecord!.AttachmentId);
            Assert.Equal(tableRecord.ProductId, procRecord.ProductId);
            Assert.Equal(tableRecord.AttachmentIndex, procRecord.AttachmentIndex);
            Assert.Equal(tableRecord.OriginalFileName, procRecord.OriginalFileName);
        }
        finally
        {
            await gateway.DeleteAsync(attachmentId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_product_attachment()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var productId = await readContext.Products.AsNoTracking().Select(x => x.ProductId).FirstOrDefaultAsync();
        Assert.NotEqual(Guid.Empty, productId);

        var gateway = new ProductAttachmentStoredProcedureGateway(readContext, writeContext);
        var attachmentId = await gateway.InsertAsync(new CreateProductAttachmentStoredProcedureRequest(
            ProductId: productId,
            AttachmentIndex: 402,
            OriginalFileName: TrimToMaxLength($"opsx-product-attachment-before-{Guid.NewGuid():N}.txt", 255)));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateProductAttachmentStoredProcedureRequest(
                AttachmentId: attachmentId,
                ProductId: productId,
                AttachmentIndex: 499,
                OriginalFileName: TrimToMaxLength($"opsx-product-attachment-after-{Guid.NewGuid():N}.txt", 255)));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(attachmentId);
            var tableRecord = await readContext.ProductAttachments.AsNoTracking().FirstOrDefaultAsync(x => x.AttachmentId == attachmentId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.AttachmentIndex, procRecord!.AttachmentIndex);
            Assert.Equal(tableRecord.OriginalFileName, procRecord.OriginalFileName);
        }
        finally
        {
            await gateway.DeleteAsync(attachmentId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
