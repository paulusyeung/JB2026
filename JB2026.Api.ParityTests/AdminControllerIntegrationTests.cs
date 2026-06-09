using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace JB2026.Api.ParityTests;

public sealed class AdminControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<Program> _factory;

    public AdminControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<string> GetAuthTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v2/auth/token", new TokenRequest
        {
            Username = "admin",
            Password = "password123"
        });

        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(SerializerOptions);

        Assert.NotNull(tokenResponse);
        return tokenResponse.AccessToken;
    }

    [Fact]
    public async Task SummarizeCustomerContact_Returns200_WhenValidInput()
    {
        // Arrange
        var testCustomerId = Guid.NewGuid();
        var expectedJson = """{"company_name":"Acme Corp","address":"123 Main St","phone":"+1 555-1234","fax":"","attention_to":"John Doe","detected_language":"en"}""";

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ICustomerStoredProcedureGateway>(new FakeCustomerGateway(testCustomerId));
                services.AddSingleton<AISummaryService>(sp =>
                {
                    var ollamaClient = new FakeOllamaApiClient(expectedJson);
                    var options = Microsoft.Extensions.Options.Options.Create(new OllamaOptions { Enabled = true });
                    var logger = sp.GetRequiredService<ILogger<AISummaryService>>();
                    return new AISummaryService(ollamaClient, options, logger);
                });
            });
        }).CreateClient();

        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SummarizeCustomerContactRequest
        {
            RawContactText = "Acme Corp\nJohn Doe\n123 Main St\nTel: +1 555-1234",
            PersistResult = false,
            OverwriteExistingSummary = false
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/v2/admin/customers/{testCustomerId}/summarize-contact", request, SerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SummarizeCustomerContactResponse>(SerializerOptions);
        Assert.NotNull(result);
        Assert.Equal(testCustomerId, result.CustomerId);
        Assert.NotNull(result.Summary);
        Assert.Equal("Acme Corp", result.Summary.CompanyName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SummarizeCustomerContact_Returns400_WhenInputIsEmpty()
    {
        // Arrange
        var testCustomerId = Guid.NewGuid();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ICustomerStoredProcedureGateway>(new FakeCustomerGateway(testCustomerId));
            });
        }).CreateClient();

        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SummarizeCustomerContactRequest
        {
            RawContactText = "",
            PersistResult = false,
            OverwriteExistingSummary = false
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/v2/admin/customers/{testCustomerId}/summarize-contact", request, SerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SummarizeCustomerContact_Returns400_WhenInputExceedsMaxSize()
    {
        // Arrange
        var testCustomerId = Guid.NewGuid();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ICustomerStoredProcedureGateway>(new FakeCustomerGateway(testCustomerId));
            });
        }).CreateClient();

        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SummarizeCustomerContactRequest
        {
            RawContactText = new string('x', 10 * 1024 + 1), // Exceeds 10KB limit
            PersistResult = false,
            OverwriteExistingSummary = false
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/v2/admin/customers/{testCustomerId}/summarize-contact", request, SerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SummarizeCustomerContact_Returns404_WhenCustomerNotFound()
    {
        // Arrange
        var nonExistentCustomerId = Guid.NewGuid();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ICustomerStoredProcedureGateway>(new FakeCustomerGateway(null)); // Returns null for any ID
            });
        }).CreateClient();

        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SummarizeCustomerContactRequest
        {
            RawContactText = "Some text",
            PersistResult = false,
            OverwriteExistingSummary = false
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/v2/admin/customers/{nonExistentCustomerId}/summarize-contact", request, SerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SummarizeCustomerContact_Returns200WithErrorMessage_WhenFeatureDisabled()
    {
        // Arrange
        var testCustomerId = Guid.NewGuid();
        var disabledOptions = new OllamaOptions { Enabled = false };

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ICustomerStoredProcedureGateway>(new FakeCustomerGateway(testCustomerId));
                // Replace OllamaOptions with disabled configuration
                services.AddSingleton(Microsoft.Extensions.Options.Options.Create(disabledOptions));
            });
        }).CreateClient();

        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SummarizeCustomerContactRequest
        {
            RawContactText = "Some text",
            PersistResult = false,
            OverwriteExistingSummary = false
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/v2/admin/customers/{testCustomerId}/summarize-contact", request, SerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SummarizeCustomerContactResponse>(SerializerOptions);
        Assert.NotNull(result);
        Assert.Equal(testCustomerId, result.CustomerId);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("disabled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Fake gateway that returns a customer for a specific ID or null.
    /// </summary>
    private sealed class FakeCustomerGateway : ICustomerStoredProcedureGateway
    {
        private readonly Guid? _customerId;

        public FakeCustomerGateway(Guid? customerId)
        {
            _customerId = customerId;
        }

        public Task<CustomerStoredProcedureRecord?> SelectAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_customerId.HasValue && _customerId.Value == id)
            {
                return Task.FromResult(new CustomerStoredProcedureRecord(
                    CustomerId: id,
                    CustomerName: "Test Customer",
                    LoginAccount: "test",
                    LoginPassword: "password",
                    MetadataXml: null,
                    CreatedOn: DateTime.Now,
                    CreatedBy: Guid.NewGuid(),
                    ModifiedOn: DateTime.Now,
                    ModifiedBy: Guid.NewGuid(),
                    Retired: false,
                    RetiredOn: null,
                    RetiredBy: null
                ));
            }

            return Task.FromResult<CustomerStoredProcedureRecord?>(null);
        }

        public Task<Guid> InsertAsync(CreateCustomerStoredProcedureRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<bool> UpdateAsync(UpdateCustomerStoredProcedureRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Fake Ollama client that returns a predefined response.
    /// </summary>
    private sealed class FakeOllamaApiClient : IOllamaApiClient
    {
        private readonly string _response;

        public FakeOllamaApiClient(string response)
        {
            _response = response;
        }

        public Uri Uri { get => new("http://localhost:11434"); set { } }
        public string SelectedModel { get => "llama3"; set { } }

        public async IAsyncEnumerable<GenerateResponseStream?> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new GenerateResponseStream
            {
                Response = _response,
                Done = true
            };
        }

        public IAsyncEnumerable<ChatResponseStream?> ChatAsync(ChatRequest request, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();
        public Task CopyModelAsync(CopyModelRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
        public IAsyncEnumerable<CreateModelResponse?> CreateModelAsync(CreateModelRequest request, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();
        public Task DeleteModelAsync(DeleteModelRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
        public Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();
        public Task<IEnumerable<Model>> ListLocalModelsAsync(CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<Model>());
        public Task<IEnumerable<RunningModel>> ListRunningModelsAsync(CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<RunningModel>());
        public IAsyncEnumerable<PullModelResponse?> PullModelAsync(PullModelRequest request, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();
        public IAsyncEnumerable<PushModelResponse?> PushModelAsync(PushModelRequest request, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();
        public Task<ShowModelResponse> ShowModelAsync(ShowModelRequest request, CancellationToken cancellationToken)
            => throw new System.NotImplementedException();
        public Task<bool> IsRunningAsync(CancellationToken cancellationToken)
            => Task.FromResult(true);
        public Task<string> GetVersionAsync(CancellationToken cancellationToken)
            => Task.FromResult("0.0.0");
        public Task PushBlobAsync(string digest, byte[] bytes, CancellationToken cancellationToken)
            => Task.CompletedTask;
        public Task<bool> IsBlobExistsAsync(string digest, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }
}
