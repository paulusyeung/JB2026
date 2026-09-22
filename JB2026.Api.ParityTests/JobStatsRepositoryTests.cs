using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

/// <summary>
/// Unit tests for <see cref="EfJobManagementRepository.GetJobStats"/> using an in-memory EF Core store.
/// Verifies the LINQ-over-JobOrder replacement of the legacy stats views and OriginalSONumber cost derivation.
/// </summary>
public sealed class JobStatsRepositoryTests
{
    private static JB5LegacyReadContext CreateReadContext()
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase($"job-stats-{Guid.NewGuid():N}")
            .Options;
        return new JB5LegacyReadContext(options);
    }

    private static EfJobManagementRepository CreateRepository(JB5LegacyReadContext readContext)
    {
        var writeOptions = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseInMemoryDatabase($"job-stats-write-{Guid.NewGuid():N}")
            .Options;
        var writeContext = new JB5LegacyWriteContext(writeOptions);
        return new EfJobManagementRepository(readContext, writeContext, NullLogger<EfJobManagementRepository>.Instance);
    }

    [Fact]
    public void GetJobStats_CostDerivation_ComesFromOriginalSONumber()
    {
        using var readContext = CreateReadContext();

        SeedJob(readContext, "JB260301", 1, 1000m, "1234.5", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        SeedJob(readContext, "JB260302", 2, 1000m, null, new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        SeedJob(readContext, "JB260303", 3, 1000m, string.Empty, new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        SeedJob(readContext, "JB260304", 4, 1000m, "PO-123", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        SeedJob(readContext, "JB260305", 5, 1000m, "12.34.56", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        SeedJob(readContext, "JB260306", 6, 1000m, "00123.5", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        var repo = CreateRepository(readContext);

        var rows = repo.GetJobStats(null, null).ToDictionary(row => row.JobNumber);

        Assert.Equal(1234.5m, rows["JB260301-1"].Cost);
        Assert.Equal(0m, rows["JB260302-2"].Cost);
        Assert.Equal(0m, rows["JB260303-3"].Cost);
        Assert.Equal(0m, rows["JB260304-4"].Cost);
        Assert.Equal(0m, rows["JB260305-5"].Cost);
        Assert.Equal(123.5m, rows["JB260306-6"].Cost);
    }

    [Fact]
    public void GetJobStats_ExcludesZeroInvoiceAmounts_AndDerivesCompositeNumber()
    {
        using var readContext = CreateReadContext();

        SeedJob(readContext, "JB260301", 1, 0m, "50.00", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        SeedJob(readContext, "JB260302", 2, null, "50.00", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        SeedJob(readContext, "JB260303", 3, 200m, "50.00", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        var repo = CreateRepository(readContext);

        var rows = repo.GetJobStats(null, null).Select(row => row.JobNumber).ToList();

        Assert.Single(rows);
        Assert.Equal("JB260303-3", rows[0]);
    }

    [Fact]
    public void GetJobStats_InvoiceDateFallsBackToRequiredOnWhenCompletedIsEpochYear()
    {
        using var readContext = CreateReadContext();

        SeedJob(readContext, "JB260301", 1, 200m, "50.00", new DateTime(1900, 1, 1), new DateTime(2026, 1, 10, 12, 0, 0));
        SeedJob(readContext, "JB260302", 2, 200m, "50.00", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 20, 12, 0, 0));
        var repo = CreateRepository(readContext);

        var rows = repo.GetJobStats(null, null).OrderBy(row => row.JobNumber).ToList();

        Assert.Equal(new DateOnly(2026, 1, 10), rows.First(row => row.JobNumber == "JB260301-1").InvDate);
        Assert.Equal(new DateOnly(2026, 1, 5), rows.First(row => row.JobNumber == "JB260302-2").InvDate);
    }

    [Fact]
    public void GetJobStats_GrossProfitUsesPercentageSemantics()
    {
        using var readContext = CreateReadContext();

        SeedJob(readContext, "JB260301", 1, 200m, "50.00", new DateTime(2026, 1, 5, 12, 0, 0), new DateTime(2026, 1, 6, 12, 0, 0));
        var repo = CreateRepository(readContext);

        var row = repo.GetJobStats(null, null).Single();

        Assert.Equal(50m, row.Cost);
        Assert.Equal(200m, row.InvoiceAmount);
        Assert.Equal(75.00m, row.GrossProfit);
    }

    [Fact]
    public void GetJobStats_OrdersByInvoiceDateThenInvoiceNumber()
    {
        using var readContext = CreateReadContext();

        SeedJob(readContext, "JB260301", 1, 100m, "10.00", new DateTime(2026, 2, 1, 12, 0, 0), new DateTime(2026, 2, 2, 12, 0, 0), "INV-B");
        SeedJob(readContext, "JB260302", 2, 100m, "10.00", new DateTime(2026, 1, 15, 12, 0, 0), new DateTime(2026, 1, 16, 12, 0, 0), "INV-A");
        SeedJob(readContext, "JB260303", 3, 100m, "10.00", new DateTime(2026, 1, 15, 12, 0, 0), new DateTime(2026, 1, 16, 12, 0, 0), "INV-C");
        var repo = CreateRepository(readContext);

        var rows = repo.GetJobStats(null, null).ToList();

        Assert.Equal(new[] { "JB260302-2", "JB260303-3", "JB260301-1" }, rows.Select(row => row.JobNumber).ToArray());
    }

    [Fact]
    public void GetJobStats_DateRangeFiltering_IsInclusiveOfStartAndEndDay()
    {
        using var readContext = CreateReadContext();

        SeedJob(readContext, "JB260301", 1, 100m, "10.00", new DateTime(2026, 1, 15, 12, 0, 0), new DateTime(2026, 1, 16, 12, 0, 0), "INV-A");
        SeedJob(readContext, "JB260302", 2, 100m, "10.00", new DateTime(2026, 2, 1, 12, 0, 0), new DateTime(2026, 2, 2, 12, 0, 0), "INV-B");
        SeedJob(readContext, "JB260303", 3, 100m, "10.00", new DateTime(2026, 3, 1, 12, 0, 0), new DateTime(2026, 3, 2, 12, 0, 0), "INV-C");
        var repo = CreateRepository(readContext);

        var fromStart = repo.GetJobStats(new DateOnly(2026, 1, 15), null).Select(row => row.JobNumber).ToArray();
        var throughEnd = repo.GetJobStats(null, new DateOnly(2026, 2, 1)).Select(row => row.JobNumber).ToArray();
        var singleDay = repo.GetJobStats(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1)).Select(row => row.JobNumber).ToArray();

        Assert.Equal(new[] { "JB260301-1", "JB260302-2", "JB260303-3" }, fromStart);
        Assert.Equal(new[] { "JB260301-1", "JB260302-2" }, throughEnd);
        Assert.Equal(new[] { "JB260302-2" }, singleDay);
    }

    private static JobOrder SeedJob(
        JB5LegacyReadContext readContext,
        string orderNumber,
        int jobNumber,
        decimal? invoiceAmount,
        string? originalSONumber,
        DateTime? completedOn,
        DateTime requiredOn,
        string? invoiceRef = null)
    {
        var order = new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = orderNumber,
            JobNumber = jobNumber,
            CustomerName = $"Customer {orderNumber}",
            OrderTitle = $"Title {orderNumber}",
            CustomerRef = $"PO-{orderNumber}",
            OrderedBy = "rep",
            InvoiceRef = invoiceRef ?? $"INV-{orderNumber}",
            InvoiceAmount = invoiceAmount,
            OriginalSONumber = originalSONumber,
            CompletedOn = completedOn,
            RequiredOn = requiredOn,
        };

        readContext.JobOrders.Add(order);
        readContext.SaveChanges();
        return order;
    }
}