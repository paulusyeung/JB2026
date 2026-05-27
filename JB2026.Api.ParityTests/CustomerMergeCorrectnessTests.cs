using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JB2026.Api.ParityTests;

public sealed class CustomerMergeCorrectnessTests
{
    [Fact]
    public async Task MergeCustomers_reassigns_invoice_and_quotation_headers_and_retires_sources()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var customerTemplate = await readContext.Customers.AsNoTracking().FirstAsync();
        var invoiceTemplate = await readContext.InvoiceHeaders.AsNoTracking().FirstAsync();
        var qtTemplate = await readContext.QtHeaders.AsNoTracking().FirstAsync();

        var now = DateTime.Now;
        var actorId = Guid.NewGuid();

        var targetId = Guid.NewGuid();
        var source1Id = Guid.NewGuid();
        var source2Id = Guid.NewGuid();

        var target = BuildCustomer(targetId, "OPSX-MERGE-TARGET", customerTemplate, now);
        var source1 = BuildCustomer(source1Id, "OPSX-MERGE-SRC1", customerTemplate, now);
        var source2 = BuildCustomer(source2Id, "OPSX-MERGE-SRC2", customerTemplate, now);

        writeContext.Customers.AddRange(target, source1, source2);
        await writeContext.SaveChangesAsync();

        var invoice1Id = Guid.NewGuid();
        var invoice2Id = Guid.NewGuid();
        var qtHeaderId = Guid.NewGuid();

        var invoice1 = BuildInvoiceHeader(invoice1Id, source1Id, invoiceTemplate, now);
        var invoice2 = BuildInvoiceHeader(invoice2Id, source2Id, invoiceTemplate, now);
        var qtHeader = BuildQtHeader(qtHeaderId, source1Id, qtTemplate, now);

        writeContext.InvoiceHeaders.AddRange(invoice1, invoice2);
        writeContext.QtHeaders.Add(qtHeader);
        await writeContext.SaveChangesAsync();

        try
        {
            var controller = BuildController(actorId);
            var request = new MergeAdminCustomersRequest
            {
                TargetCustomerId = targetId,
                CustomerIds = [targetId, source1Id, source2Id],
            };

            var result = await controller.MergeCustomers(writeContext, request, CancellationToken.None);
            Assert.IsType<NoContentResult>(result);

            // Verify target is not retired
            var refreshedTarget = await readContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == targetId);
            Assert.NotNull(refreshedTarget);
            Assert.False(refreshedTarget!.Retired);

            // Verify sources are retired with audit stamps
            var refreshedSource1 = await readContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == source1Id);
            var refreshedSource2 = await readContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == source2Id);
            Assert.NotNull(refreshedSource1);
            Assert.NotNull(refreshedSource2);
            Assert.True(refreshedSource1!.Retired);
            Assert.True(refreshedSource2!.Retired);
            Assert.NotNull(refreshedSource1.RetiredOn);
            Assert.NotNull(refreshedSource2.RetiredOn);
            Assert.Equal(actorId, refreshedSource1.RetiredBy);
            Assert.Equal(actorId, refreshedSource2.RetiredBy);

            // Verify invoice headers are reassigned to target
            var refreshedInvoice1 = await readContext.InvoiceHeaders.AsNoTracking()
                .FirstOrDefaultAsync(h => h.HeaderId == invoice1Id);
            var refreshedInvoice2 = await readContext.InvoiceHeaders.AsNoTracking()
                .FirstOrDefaultAsync(h => h.HeaderId == invoice2Id);
            Assert.NotNull(refreshedInvoice1);
            Assert.NotNull(refreshedInvoice2);
            Assert.Equal(targetId, refreshedInvoice1!.CustomerId);
            Assert.Equal(targetId, refreshedInvoice2!.CustomerId);

            // Verify quotation header is reassigned to target
            var refreshedQt = await readContext.QtHeaders.AsNoTracking()
                .FirstOrDefaultAsync(h => h.HeaderId == qtHeaderId);
            Assert.NotNull(refreshedQt);
            Assert.Equal(targetId, refreshedQt!.CustomerId);
        }
        finally
        {
            await CleanupAsync(writeContext, [invoice1Id, invoice2Id], [qtHeaderId], [targetId, source1Id, source2Id]);
        }
    }

    [Fact]
    public async Task MergeCustomers_returns_bad_request_when_fewer_than_two_distinct_customers()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();

        var id = Guid.NewGuid();
        var controller = BuildController(Guid.NewGuid());
        var request = new MergeAdminCustomersRequest
        {
            TargetCustomerId = id,
            CustomerIds = [id],
        };

        var result = await controller.MergeCustomers(writeContext, request, CancellationToken.None);
        var validation = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, validation.StatusCode);
    }

    [Fact]
    public async Task MergeCustomers_returns_bad_request_when_target_not_in_selection()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();

        var controller = BuildController(Guid.NewGuid());
        var request = new MergeAdminCustomersRequest
        {
            TargetCustomerId = Guid.NewGuid(),
            CustomerIds = [Guid.NewGuid(), Guid.NewGuid()],
        };

        var result = await controller.MergeCustomers(writeContext, request, CancellationToken.None);
        var validation = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, validation.StatusCode);
    }

    [Fact]
    public async Task MergeCustomers_returns_bad_request_when_target_is_retired()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var customerTemplate = await readContext.Customers.AsNoTracking().FirstAsync();
        var now = DateTime.Now;

        var retiredTargetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        var retiredTarget = BuildCustomer(retiredTargetId, "OPSX-MERGE-RETARGET", customerTemplate, now);
        retiredTarget.Retired = true;
        retiredTarget.RetiredOn = now;
        retiredTarget.RetiredBy = Guid.NewGuid();
        var source = BuildCustomer(sourceId, "OPSX-MERGE-SRC-RT", customerTemplate, now);

        writeContext.Customers.AddRange(retiredTarget, source);
        await writeContext.SaveChangesAsync();

        try
        {
            var controller = BuildController(Guid.NewGuid());
            var request = new MergeAdminCustomersRequest
            {
                TargetCustomerId = retiredTargetId,
                CustomerIds = [retiredTargetId, sourceId],
            };

            var result = await controller.MergeCustomers(writeContext, request, CancellationToken.None);
            var validation = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, validation.StatusCode);
        }
        finally
        {
            await CleanupAsync(writeContext, [], [], [retiredTargetId, sourceId]);
        }
    }

    [Fact]
    public async Task MergeCustomers_returns_bad_request_when_source_is_retired()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var customerTemplate = await readContext.Customers.AsNoTracking().FirstAsync();
        var now = DateTime.Now;

        var targetId = Guid.NewGuid();
        var retiredSourceId = Guid.NewGuid();

        var target = BuildCustomer(targetId, "OPSX-MERGE-TGT-RS", customerTemplate, now);
        var retiredSource = BuildCustomer(retiredSourceId, "OPSX-MERGE-RESSRC", customerTemplate, now);
        retiredSource.Retired = true;
        retiredSource.RetiredOn = now;
        retiredSource.RetiredBy = Guid.NewGuid();

        writeContext.Customers.AddRange(target, retiredSource);
        await writeContext.SaveChangesAsync();

        try
        {
            var controller = BuildController(Guid.NewGuid());
            var request = new MergeAdminCustomersRequest
            {
                TargetCustomerId = targetId,
                CustomerIds = [targetId, retiredSourceId],
            };

            var result = await controller.MergeCustomers(writeContext, request, CancellationToken.None);
            var validation = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, validation.StatusCode);
        }
        finally
        {
            await CleanupAsync(writeContext, [], [], [targetId, retiredSourceId]);
        }
    }

    [Fact]
    public async Task MergeCustomers_returns_not_found_when_customer_no_longer_exists()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();

        var existingId = Guid.NewGuid();
        var missingId = Guid.NewGuid();

        // existingId is not in DB; call will find 0 of 2 requested
        var controller = BuildController(Guid.NewGuid());
        var request = new MergeAdminCustomersRequest
        {
            TargetCustomerId = existingId,
            CustomerIds = [existingId, missingId],
        };

        var result = await controller.MergeCustomers(writeContext, request, CancellationToken.None);
        var notFound = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private static AdminController BuildController(Guid actorId)
    {
        var controller = new AdminController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
                ])),
            },
        };
        return controller;
    }

    private static Customer BuildCustomer(Guid id, string namePrefix, Customer template, DateTime now) =>
        new()
        {
            CustomerId = id,
            CustomerName = $"{namePrefix}-{id:N}"[..Math.Min($"{namePrefix}-{id:N}".Length, 128)],
            LoginAccount = $"acc-{id:N}"[..Math.Min($"acc-{id:N}".Length, 64)],
            LoginPassword = $"pwd-{id:N}"[..Math.Min($"pwd-{id:N}".Length, 64)],
            MetadataXml = template.MetadataXml,
            CreatedOn = now,
            CreatedBy = template.CreatedBy,
            ModifiedOn = now,
            ModifiedBy = template.ModifiedBy,
            Retired = false,
        };

    private static InvoiceHeader BuildInvoiceHeader(Guid id, Guid customerId, InvoiceHeader template, DateTime now) =>
        new()
        {
            HeaderId = id,
            CustomerId = customerId,
            BillTo = $"BILLTO-{id:N}"[..Math.Min($"BILLTO-{id:N}".Length, 256)],
            InvoiceDate = template.InvoiceDate,
            InvoiceNumber = $"OPSX{id:N}"[..8],
            InvoiceAmount = template.InvoiceAmount,
            CreatedOn = now,
            CreatedBy = template.CreatedBy,
            ModifiedOn = now,
            ModifiedBy = template.ModifiedBy,
            Retired = false,
        };

    private static QtHeader BuildQtHeader(Guid id, Guid customerId, QtHeader template, DateTime now) =>
        new()
        {
            HeaderId = id,
            CustomerId = customerId,
            MachineType = template.MachineType,
            QuoteNumber = 0,
            QuoteNumberIndex = 0,
            QuotedOn = now,
            QuotedBy = template.QuotedBy,
            Status = template.Status,
            CreatedOn = now,
            CreatedBy = template.CreatedBy,
            ModifiedOn = now,
            ModifiedBy = template.ModifiedBy,
            Retired = false,
        };

    private static async Task CleanupAsync(
        JB2026.EfCore.Data.JB5LegacyWriteContext writeContext,
        Guid[] invoiceHeaderIds,
        Guid[] qtHeaderIds,
        Guid[] customerIds)
    {
        // Use per-ID queries to avoid OPENJSON ($) on legacy DB compatibility level.
        foreach (var id in invoiceHeaderIds)
        {
            var inv = await writeContext.InvoiceHeaders.FirstOrDefaultAsync(h => h.HeaderId == id);
            if (inv is not null) writeContext.InvoiceHeaders.Remove(inv);
        }

        foreach (var id in qtHeaderIds)
        {
            var qt = await writeContext.QtHeaders.FirstOrDefaultAsync(h => h.HeaderId == id);
            if (qt is not null) writeContext.QtHeaders.Remove(qt);
        }

        foreach (var id in customerIds)
        {
            var c = await writeContext.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);
            if (c is not null) writeContext.Customers.Remove(c);
        }

        await writeContext.SaveChangesAsync();
    }
}
