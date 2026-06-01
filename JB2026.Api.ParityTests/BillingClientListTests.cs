using JB2026.Api.Models.Billing;
using JB2026.Api.Options;
using JB2026.Api.Services.Billing;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

public sealed class BillingClientListTests
{
    [Fact]
    public async Task GetBillingClientsAsync_MapsNameCodeAndOutstandingBalance()
    {
        await using var readContext = CreateReadContext();

        var service = CreateBillingService(
            readContext,
            new StubInvoiceNinjaHttpClient([
                new InvoiceNinjaClientResponse
                {
                    Id = "client-1",
                    Name = "Acme Printing",
                    DisplayName = "Acme Printing Ltd",
                    IdNumber = "C-0001",
                    Balance = 1234.5m,
                }
            ]));

        var clients = await service.GetBillingClientsAsync(null);

        var client = Assert.Single(clients);
        Assert.Equal("client-1", client.ExternalClientId);
        Assert.Equal("Acme Printing", client.Name);
        Assert.Equal("Acme Printing Ltd", client.DisplayName);
        Assert.Equal("C-0001", client.IdNumber);
        Assert.Equal(1234.5m, client.OutstandingBalance);
    }

    [Fact]
    public async Task GetBillingClientsAsync_FallsBackToNameWhenDisplayNameMissing()
    {
        await using var readContext = CreateReadContext();

        var service = CreateBillingService(
            readContext,
            new StubInvoiceNinjaHttpClient([
                new InvoiceNinjaClientResponse
                {
                    Id = "client-2",
                    Name = "Fallback Name",
                    DisplayName = string.Empty,
                    IdNumber = "C-0002",
                    Balance = 0m,
                }
            ]));

        var clients = await service.GetBillingClientsAsync("Fallback");

        var client = Assert.Single(clients);
        Assert.Equal("Fallback Name", client.DisplayName);
    }

    private static BillingService CreateBillingService(
        JB5LegacyReadContext readContext,
        IInvoiceNinjaHttpClient invoiceNinjaHttpClient)
    {
        var services = new ServiceCollection();
        services.AddSingleton(readContext);
        var provider = services.BuildServiceProvider();

        return new BillingService(
            invoiceNinjaHttpClient,
            Microsoft.Extensions.Options.Options.Create(new BillingOptions()),
            provider,
            NullLogger<BillingService>.Instance);
    }

    private static JB5LegacyReadContext CreateReadContext()
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase($"billing-client-list-{Guid.NewGuid():N}")
            .Options;
        return new JB5LegacyReadContext(options);
    }

    private sealed class StubInvoiceNinjaHttpClient(IReadOnlyList<InvoiceNinjaClientResponse> clients) : IInvoiceNinjaHttpClient
    {
        public Task<T?> GetAsync<T>(string endpoint) where T : class
        {
            if (typeof(T) == typeof(List<InvoiceNinjaClientResponse>))
            {
                return Task.FromResult(clients.Select(Clone).ToList() as T);
            }

            return Task.FromResult<T?>(null);
        }

        public Task<T> PostAsync<T>(string endpoint, object body) where T : class => throw new NotSupportedException();

        public Task<T> PutAsync<T>(string endpoint, object body) where T : class => throw new NotSupportedException();

        public Task<bool> IsConnectedAsync() => Task.FromResult(true);

        public (bool isValid, string errorMessage) ValidateConfiguration() => (true, string.Empty);

        public Task<byte[]?> GetStreamAsync(string endpoint) => Task.FromResult<byte[]?>(null);

        private static InvoiceNinjaClientResponse Clone(InvoiceNinjaClientResponse client) => new()
        {
            Id = client.Id,
            Name = client.Name,
            DisplayName = client.DisplayName,
            IdNumber = client.IdNumber,
            Balance = client.Balance,
            CurrencyId = client.CurrencyId,
            Email = client.Email,
            UpdatedAt = client.UpdatedAt,
            CustomValues = new Dictionary<string, string?>(client.CustomValues),
        };
    }
}