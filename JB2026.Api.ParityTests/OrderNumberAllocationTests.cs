using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

/// <summary>
/// Guards the fix for duplicate order numbers. The order number used to be read from
/// settings by the browser, sent to the API, and incremented by the browser afterwards.
/// Two users who opened the order dialog at the same time therefore submitted the same
/// number, and dbo.JobOrder has no unique index to reject it. Allocation is now server-side
/// and happens inside the create request.
/// </summary>
public sealed class OrderNumberAllocationTests
{
    [Fact]
    public async Task AllocateNextOrderNumberAsync_sequential_calls_never_repeat_a_number()
    {
        var service = new InMemorySettingsService();

        var allocated = new List<string>();
        for (var i = 0; i < 25; i++)
        {
            allocated.Add(await service.AllocateNextOrderNumberAsync());
        }

        Assert.Equal(allocated.Count, allocated.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task AllocateNextOrderNumberAsync_advances_the_persisted_counter()
    {
        var service = new InMemorySettingsService();
        var start = service.Get().NextOrderNumber;

        var first = await service.AllocateNextOrderNumberAsync();
        var second = await service.AllocateNextOrderNumberAsync();

        Assert.Equal(start, first);
        Assert.Equal(int.Parse(start) + 1, int.Parse(second));
        Assert.Equal(int.Parse(start) + 2, int.Parse(service.Get().NextOrderNumber));
    }

    [Fact]
    public async Task AllocateNextOrderNumberAsync_concurrent_calls_never_repeat_a_number()
    {
        var service = new InMemorySettingsService();

        var allocated = await Task.WhenAll(
            Enumerable.Range(0, 64).Select(_ => Task.Run(() => service.AllocateNextOrderNumberAsync())));

        Assert.Equal(allocated.Length, allocated.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task CreateJobOrder_without_order_number_allocates_one_server_side()
    {
        var settings = new InMemorySettingsService();
        var repository = new InMemoryJobManagementRepository(settings);
        var expected = settings.Get().NextOrderNumber;

        var created = await repository.CreateJobOrder(BuildRequest(orderNumber: null), "admin");

        Assert.Equal(expected, created.OrderNumber);
    }

    [Fact]
    public async Task CreateJobOrder_without_order_number_allocates_distinct_numbers_per_call()
    {
        var repository = new InMemoryJobManagementRepository(new InMemorySettingsService());

        var created = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            created.Add((await repository.CreateJobOrder(BuildRequest(orderNumber: null), "admin")).OrderNumber);
        }

        Assert.Equal(created.Count, created.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task CreateJobOrder_with_order_number_keeps_the_supplied_value()
    {
        var settings = new InMemorySettingsService();
        var repository = new InMemoryJobManagementRepository(settings);
        var counterBefore = settings.Get().NextOrderNumber;

        // Adding a job under an existing order must not consume a new order number.
        var created = await repository.CreateJobOrder(BuildRequest("170390", jobNumber: "2"), "admin");

        Assert.Equal("170390", created.OrderNumber);
        Assert.Equal("2", created.JobNumber);
        Assert.Equal(counterBefore, settings.Get().NextOrderNumber);
    }

    [Fact]
    public async Task CreateJobOrder_without_order_number_fails_loudly_when_no_allocator_is_wired()
    {
        var repository = new InMemoryJobManagementRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.CreateJobOrder(BuildRequest(orderNumber: null), "admin"));
    }

    [Fact]
    public async Task EfCore_create_without_order_number_allocates_distinct_numbers_under_concurrency()
    {
        var settings = new InMemorySettingsService();
        var start = int.Parse(settings.Get().NextOrderNumber);
        const int attempts = 16;

        var created = new string[attempts];
        await Task.WhenAll(Enumerable.Range(0, attempts).Select(async i =>
        {
            await using var writeContext = CreateWriteContext();
            var repository = new EfJobManagementRepository(
                CreateReadContext(),
                writeContext,
                NullLogger<EfJobManagementRepository>.Instance,
                settingsService: settings);

            created[i] = (await repository.CreateJobOrder(BuildRequest(orderNumber: null), "admin")).OrderNumber;
        }));

        Assert.Equal(attempts, created.Distinct(StringComparer.Ordinal).Count());
        Assert.All(created, number => Assert.InRange(int.Parse(number), start, start + attempts - 1));
    }

    private static JB5LegacyReadContext CreateReadContext()
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase($"order-number-read-{Guid.NewGuid():N}")
            .Options;
        return new JB5LegacyReadContext(options);
    }

    private static JB5LegacyWriteContext CreateWriteContext()
    {
        var options = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseInMemoryDatabase($"order-number-write-{Guid.NewGuid():N}")
            .Options;
        return new JB5LegacyWriteContext(options);
    }

    private static CreateJobOrderRequest BuildRequest(string? orderNumber, string jobNumber = "0")
    {
        return new CreateJobOrderRequest
        {
            OrderNumber = orderNumber ?? string.Empty,
            JobNumber = jobNumber,
            CustomerName = "Allocation Test Customer",
            OrderTitle = "Allocation Test Order",
            OrderedBy = "admin",
            OrderedOn = new DateTime(2026, 1, 2),
            RequiredOn = new DateTime(2026, 1, 9),
            Qty = 1,
        };
    }
}
