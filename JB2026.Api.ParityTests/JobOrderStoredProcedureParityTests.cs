using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class JobOrderStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_job_order()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.JobOrders.AsNoTracking().FirstAsync();
        var gateway = new JobOrderStoredProcedureGateway(readContext, writeContext);

        var orderId = await gateway.InsertAsync(new CreateJobOrderStoredProcedureRequest(
            OrderType: template.OrderType,
            OrderNumber: $"O{Guid.NewGuid():N}"[..10],
            JobNumber: template.JobNumber,
            CustomerName: template.CustomerName,
            CustomerRef: template.CustomerRef,
            OrderTitle: template.OrderTitle,
            ProductCode: template.ProductCode,
            ProductStyle: template.ProductStyle,
            ProductDetails: template.ProductDetails,
            OrderedOn: template.OrderedOn,
            OrderedBy: template.OrderedBy,
            OutputRef: template.OutputRef,
            InvoiceRef: template.InvoiceRef,
            InvoiceAmount: template.InvoiceAmount,
            Qty: template.Qty,
            QtyText: template.QtyText,
            RequiredOn: template.RequiredOn,
            CompletedOn: template.CompletedOn,
            SONumber: template.SONumber,
            PONumber: template.PONumber,
            OriginalSONumber: template.OriginalSONumber,
            OriginalPONumber: template.OriginalPONumber,
            PaymentTerms: template.PaymentTerms,
            Remarks: template.Remarks,
            Status: template.Status,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: null,
            RetiredBy: null));

        try
        {
            var procRecord = await gateway.SelectAsync(orderId);
            var tableRecord = await readContext.JobOrders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.OrderId, procRecord!.OrderId);
            Assert.Equal(tableRecord.OrderNumber, procRecord.OrderNumber);
            Assert.Equal(tableRecord.CustomerName, procRecord.CustomerName);
            Assert.Equal(tableRecord.Status, procRecord.Status);
        }
        finally
        {
            await gateway.DeleteAsync(orderId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_job_order()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.JobOrders.AsNoTracking().FirstAsync();
        var gateway = new JobOrderStoredProcedureGateway(readContext, writeContext);

        var orderId = await gateway.InsertAsync(new CreateJobOrderStoredProcedureRequest(
            OrderType: template.OrderType,
            OrderNumber: $"U{Guid.NewGuid():N}"[..10],
            JobNumber: template.JobNumber,
            CustomerName: template.CustomerName,
            CustomerRef: template.CustomerRef,
            OrderTitle: template.OrderTitle,
            ProductCode: template.ProductCode,
            ProductStyle: template.ProductStyle,
            ProductDetails: template.ProductDetails,
            OrderedOn: template.OrderedOn,
            OrderedBy: template.OrderedBy,
            OutputRef: template.OutputRef,
            InvoiceRef: template.InvoiceRef,
            InvoiceAmount: template.InvoiceAmount,
            Qty: template.Qty,
            QtyText: template.QtyText,
            RequiredOn: template.RequiredOn,
            CompletedOn: template.CompletedOn,
            SONumber: template.SONumber,
            PONumber: template.PONumber,
            OriginalSONumber: template.OriginalSONumber,
            OriginalPONumber: template.OriginalPONumber,
            PaymentTerms: template.PaymentTerms,
            Remarks: template.Remarks,
            Status: template.Status,
            CreatedOn: template.CreatedOn,
            CreatedBy: template.CreatedBy,
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy,
            Retired: false,
            RetiredOn: null,
            RetiredBy: null));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateJobOrderStoredProcedureRequest(
                OrderId: orderId,
                OrderType: template.OrderType,
                OrderNumber: $"Z{Guid.NewGuid():N}"[..10],
                JobNumber: template.JobNumber,
                CustomerName: "OPSX Updated Customer",
                CustomerRef: template.CustomerRef,
                OrderTitle: template.OrderTitle,
                ProductCode: template.ProductCode,
                ProductStyle: template.ProductStyle,
                ProductDetails: template.ProductDetails,
                OrderedOn: template.OrderedOn,
                OrderedBy: template.OrderedBy,
                OutputRef: template.OutputRef,
                InvoiceRef: template.InvoiceRef,
                InvoiceAmount: template.InvoiceAmount,
                Qty: template.Qty,
                QtyText: template.QtyText,
                RequiredOn: template.RequiredOn,
                CompletedOn: template.CompletedOn,
                SONumber: template.SONumber,
                PONumber: template.PONumber,
                OriginalSONumber: template.OriginalSONumber,
                OriginalPONumber: template.OriginalPONumber,
                PaymentTerms: template.PaymentTerms,
                Remarks: "OPSX parity update",
                Status: 8,
                CreatedOn: template.CreatedOn,
                CreatedBy: template.CreatedBy,
                ModifiedOn: DateTime.UtcNow,
                ModifiedBy: template.ModifiedBy,
                Retired: false,
                RetiredOn: null,
                RetiredBy: null));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(orderId);
            var tableRecord = await readContext.JobOrders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.OrderNumber, procRecord!.OrderNumber);
            Assert.Equal(tableRecord.CustomerName, procRecord.CustomerName);
            Assert.Equal(tableRecord.Remarks, procRecord.Remarks);
            Assert.Equal(tableRecord.Status, procRecord.Status);
        }
        finally
        {
            await gateway.DeleteAsync(orderId);
        }
    }
}
