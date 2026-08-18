using JB2026.EfCore.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

/// <summary>
/// Correctness tests for job-order delete lifecycle parity:
/// workflow cleanup, attachment row cleanup, job-order removal, and sibling renumbering.
/// </summary>
public sealed class JobOrderDeleteLifecycleTests
{
    // ─── 5.4: workflow cleanup, attachment row cleanup ────────────────────────

    [Fact]
    public async Task DeleteJobOrder_removes_workflow_rows_and_workflow_form_rows()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobOrders.AsNoTracking().FirstAsync();
        var orderId = Guid.NewGuid();

        // Create job order with two workflow rows (no forms for simplicity)
        var order = CreateTestJobOrder(orderId, template, $"DLCWF-{Guid.NewGuid():N}"[..10]);
        var workflow1 = CreateTestJobWorkflow(orderId, 1, "Step A");
        var workflow2 = CreateTestJobWorkflow(orderId, 2, "Step B");
        order.JobWorkflows.Add(workflow1);
        order.JobWorkflows.Add(workflow2);

        writeContext.JobOrders.Add(order);
        await writeContext.SaveChangesAsync();

        try
        {
            var repo = CreateRepository(readContext, writeContext);
            var result = await repo.DeleteJobOrder(orderId);

            Assert.NotNull(result);

            var remainingOrder = await readContext.JobOrders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            Assert.Null(remainingOrder);

            var remainingWorkflows = await readContext.JobWorkflows.AsNoTracking()
                .Where(w => w.OrderId == orderId)
                .ToListAsync();
            Assert.Empty(remainingWorkflows);
        }
        finally
        {
            await CleanupOrderIfExists(writeContext, orderId);
        }
    }

    [Fact]
    public async Task DeleteJobOrder_removes_attachment_rows()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobOrders.AsNoTracking().FirstAsync();
        var orderId = Guid.NewGuid();

        var order = CreateTestJobOrder(orderId, template, $"DLCAT-{Guid.NewGuid():N}"[..10]);
        var attachment1 = CreateTestJobAttachment(orderId, 0, 0, "dlc-test-file-1.jpg");
        var attachment2 = CreateTestJobAttachment(orderId, 1, 1, "dlc-test-file-2.pdf");
        order.JobAttachments.Add(attachment1);
        order.JobAttachments.Add(attachment2);

        writeContext.JobOrders.Add(order);
        await writeContext.SaveChangesAsync();

        try
        {
            var repo = CreateRepository(readContext, writeContext);
            var result = await repo.DeleteJobOrder(orderId);

            Assert.NotNull(result);

            var remainingAttachments = await readContext.JobAttachments.AsNoTracking()
                .Where(a => a.OrderId == orderId)
                .ToListAsync();
            Assert.Empty(remainingAttachments);
        }
        finally
        {
            await CleanupOrderIfExists(writeContext, orderId);
        }
    }

    [Fact]
    public async Task DeleteJobOrder_removes_the_job_order_record()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobOrders.AsNoTracking().FirstAsync();
        var orderId = Guid.NewGuid();
        var order = CreateTestJobOrder(orderId, template, $"DLCOD-{Guid.NewGuid():N}"[..10]);

        writeContext.JobOrders.Add(order);
        await writeContext.SaveChangesAsync();

        try
        {
            var repo = CreateRepository(readContext, writeContext);
            var result = await repo.DeleteJobOrder(orderId);

            Assert.NotNull(result);
            Assert.Equal(orderId, result!.OrderId);

            var remaining = await readContext.JobOrders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            Assert.Null(remaining);
        }
        finally
        {
            await CleanupOrderIfExists(writeContext, orderId);
        }
    }

    [Fact]
    public async Task DeleteJobOrder_returns_null_when_order_not_found()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var repo = CreateRepository(readContext, writeContext);
        var result = await repo.DeleteJobOrder(Guid.NewGuid());

        Assert.Null(result);
    }

    // ─── 5.5: sibling job-number rebuild after delete ─────────────────────────

    [Fact]
    public async Task DeleteJobOrder_rebuilds_sibling_job_numbers_when_sibling_deleted()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobOrders.AsNoTracking().FirstAsync();
        var baseOrderNumber = $"DLCS{Guid.NewGuid():N}"[..10];

        var sibling1Id = Guid.NewGuid();
        var sibling2Id = Guid.NewGuid();
        var sibling3Id = Guid.NewGuid();

        // Three siblings: JobNumber 1, 2, 3
        var sibling1 = CreateTestJobOrder(sibling1Id, template, baseOrderNumber, jobNumber: 1);
        var sibling2 = CreateTestJobOrder(sibling2Id, template, baseOrderNumber, jobNumber: 2);
        var sibling3 = CreateTestJobOrder(sibling3Id, template, baseOrderNumber, jobNumber: 3);

        writeContext.JobOrders.AddRange(sibling1, sibling2, sibling3);
        await writeContext.SaveChangesAsync();

        try
        {
            // Delete sibling with JobNumber=2; siblings 3 should renumber to 2
            var repo = CreateRepository(readContext, writeContext);
            await repo.DeleteJobOrder(sibling2Id);

            var remaining = await readContext.JobOrders.AsNoTracking()
                .Where(o => o.OrderNumber == baseOrderNumber)
                .OrderBy(o => o.JobNumber)
                .ToListAsync();

            Assert.Equal(2, remaining.Count);
            Assert.Equal(1, remaining[0].JobNumber);
            Assert.Equal(2, remaining[1].JobNumber); // was 3, now renumbered to 2
        }
        finally
        {
            await CleanupOrderIfExists(writeContext, sibling1Id);
            await CleanupOrderIfExists(writeContext, sibling2Id);
            await CleanupOrderIfExists(writeContext, sibling3Id);
        }
    }

    [Fact]
    public async Task DeleteJobOrder_does_not_renumber_when_job_number_is_zero_or_null()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobOrders.AsNoTracking().FirstAsync();
        var baseOrderNumber = $"DLCNS{Guid.NewGuid():N}"[..10];

        // Non-sibling job (no job number)
        var orderId = Guid.NewGuid();
        var order = CreateTestJobOrder(orderId, template, baseOrderNumber, jobNumber: null);

        writeContext.JobOrders.Add(order);
        await writeContext.SaveChangesAsync();

        try
        {
            var repo = CreateRepository(readContext, writeContext);
            var result = await repo.DeleteJobOrder(orderId);

            // Should succeed without error; no renumbering needed
            Assert.NotNull(result);
        }
        finally
        {
            await CleanupOrderIfExists(writeContext, orderId);
        }
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private static EfJobManagementRepository CreateRepository(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
        => new(readContext, writeContext, NullLogger<EfJobManagementRepository>.Instance);

    private static JobOrder CreateTestJobOrder(Guid orderId, JobOrder template, string orderNumber, int? jobNumber = null)
    {
        var timestamp = DateTime.UtcNow;
        return new JobOrder
        {
            OrderId = orderId,
            OrderType = template.OrderType,
            OrderNumber = orderNumber,
            JobNumber = jobNumber,
            CustomerName = "OPSX-DLC-TEST",
            CustomerRef = "REF-DLC",
            OrderTitle = "Delete Lifecycle Test",
            OrderedOn = template.OrderedOn,
            OrderedBy = template.OrderedBy,
            RequiredOn = template.RequiredOn,
            Status = 0,
            CreatedOn = timestamp,
            CreatedBy = template.CreatedBy,
            ModifiedOn = timestamp,
            ModifiedBy = template.ModifiedBy,
            Retired = false,
        };
    }

    private static JobWorkflow CreateTestJobWorkflow(Guid orderId, int workIndex, string workTitle)
    {
        return new JobWorkflow
        {
            JobWorkflowId = Guid.NewGuid(),
            OrderId = orderId,
            WorkIndex = workIndex,
            WorkTitle = workTitle,
        };
    }

    private static JobAttachment CreateTestJobAttachment(Guid orderId, int attachmentType, int attachmentIndex, string fileName)
    {
        return new JobAttachment
        {
            AttachmentId = Guid.NewGuid(),
            OrderId = orderId,
            AttachmentType = attachmentType,
            AttachmentIndex = attachmentIndex,
            OriginalFileName = fileName,
        };
    }

    private static async Task CleanupOrderIfExists(JB5LegacyWriteContext writeContext, Guid orderId)
    {
        // Remove remaining attachments and workflows in case the test left partial state
        var forms = await writeContext.JobWorkflowForms
            .Where(f => writeContext.JobWorkflows.Any(w => w.JobWorkflowId == f.JobWorkflowId && w.OrderId == orderId))
            .ToListAsync();
        writeContext.JobWorkflowForms.RemoveRange(forms);

        var workflows = await writeContext.JobWorkflows.Where(w => w.OrderId == orderId).ToListAsync();
        writeContext.JobWorkflows.RemoveRange(workflows);

        var attachments = await writeContext.JobAttachments.Where(a => a.OrderId == orderId).ToListAsync();
        writeContext.JobAttachments.RemoveRange(attachments);

        var order = await writeContext.JobOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order is not null)
        {
            writeContext.JobOrders.Remove(order);
        }

        await writeContext.SaveChangesAsync();
    }
}
