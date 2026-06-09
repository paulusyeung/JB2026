using System.Text.Json;
using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace JB2026.Api.Tests;

public sealed class CustomerSummaryServiceTests
{
    [Fact]
    public void MergeAiSummaryIntoMetadata_AddsSummaryToEmptyMetadata()
    {
        var summary = new ContactInfoSummary
        {
            CompanyName = "Acme Corp",
            Address = "123 Main St",
            Phone = "+1 555-1234",
            AttentionTo = "John Doe",
            DetectedLanguage = "en"
        };

        var result = CustomerSummaryService.MergeAiSummaryIntoMetadata(null, summary);

        Assert.NotNull(result);
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("AiContactSummary", out var aiSummary));
        Assert.Equal("Acme Corp", aiSummary.GetProperty("CompanyName").GetString());
        Assert.Equal("123 Main St", aiSummary.GetProperty("Address").GetString());
        Assert.Equal("+1 555-1234", aiSummary.GetProperty("Phone").GetString());
        Assert.Equal("John Doe", aiSummary.GetProperty("AttentionTo").GetString());
        Assert.Equal("en", aiSummary.GetProperty("DetectedLanguage").GetString());
    }

    [Fact]
    public void MergeAiSummaryIntoMetadata_PreservesExistingFields()
    {
        var existing = """{"CustomerCode":"C-1001","BillTo":"Existing BillTo","ShipToAddresses":[{"Name":"Main","Address":"123"}],"invoiceNinjaClientId":"ninja-123"}""";
        var summary = new ContactInfoSummary
        {
            CompanyName = "New Corp",
            Address = "456 New St",
            DetectedLanguage = "fr"
        };

        var result = CustomerSummaryService.MergeAiSummaryIntoMetadata(existing, summary);

        Assert.NotNull(result);
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal("C-1001", root.GetProperty("CustomerCode").GetString());
        Assert.Equal("Existing BillTo", root.GetProperty("BillTo").GetString());
        Assert.Equal("ninja-123", root.GetProperty("invoiceNinjaClientId").GetString());

        var aiSummary = root.GetProperty("AiContactSummary");
        Assert.Equal("New Corp", aiSummary.GetProperty("CompanyName").GetString());
        Assert.Equal("456 New St", aiSummary.GetProperty("Address").GetString());
        Assert.Equal("fr", aiSummary.GetProperty("DetectedLanguage").GetString());
    }

    [Fact]
    public void MergeAiSummaryIntoMetadata_PreservesUnknownKeys()
    {
        var existing = """{"CustomerCode":"C-1001","SomeUnknownKey":"some-value","AnotherRandomKey":42}""";
        var summary = new ContactInfoSummary { CompanyName = "Test Corp" };

        var result = CustomerSummaryService.MergeAiSummaryIntoMetadata(existing, summary);

        Assert.NotNull(result);
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal("C-1001", root.GetProperty("CustomerCode").GetString());
        Assert.Equal("some-value", root.GetProperty("SomeUnknownKey").GetString());
        Assert.Equal(42, root.GetProperty("AnotherRandomKey").GetInt32());
    }

    [Fact]
    public void MergeAiSummaryIntoMetadata_ReplacesExistingAiContactSummary()
    {
        var existing = """{"CustomerCode":"C-1001","AiContactSummary":{"CompanyName":"Old Corp","Address":"Old Address"}}""";
        var summary = new ContactInfoSummary
        {
            CompanyName = "New Corp",
            Address = "New Address",
            Phone = "+1 555-0000"
        };

        var result = CustomerSummaryService.MergeAiSummaryIntoMetadata(existing, summary);

        Assert.NotNull(result);
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal("C-1001", root.GetProperty("CustomerCode").GetString());
        var aiSummary = root.GetProperty("AiContactSummary");
        Assert.Equal("New Corp", aiSummary.GetProperty("CompanyName").GetString());
        Assert.Equal("New Address", aiSummary.GetProperty("Address").GetString());
        Assert.Equal("+1 555-0000", aiSummary.GetProperty("Phone").GetString());
    }

    [Fact]
    public void MergeAiSummaryIntoMetadata_ReturnsNull_WhenExistingMetadataIsMalformed()
    {
        var existing = "this is not valid json";
        var summary = new ContactInfoSummary { CompanyName = "Test Corp" };

        var result = CustomerSummaryService.MergeAiSummaryIntoMetadata(existing, summary);

        Assert.Null(result);
    }

    [Fact]
    public void MergeAiSummaryIntoMetadata_ReturnsNull_WhenExistingMetadataIsNotAnObject()
    {
        var existing = """["this", "is", "an", "array"]""";
        var summary = new ContactInfoSummary { CompanyName = "Test Corp" };

        var result = CustomerSummaryService.MergeAiSummaryIntoMetadata(existing, summary);

        Assert.Null(result);
    }

    [Fact]
    public async Task SummarizeAsync_SkipsPersistence_WhenExistingSummaryPresentAndOverwriteFalse()
    {
        // Arrange
        var testCustomerId = Guid.NewGuid();
        var existingMetadata = """{"CustomerCode":"C-1001","AiContactSummary":{"CompanyName":"Old Corp"}}""";
        var summaryJson = """{"company_name":"New Corp","address":"","phone":"","fax":"","attention_to":"","detected_language":"en"}""";

        var fakeGateway = new FakeCustomerGateway(testCustomerId, existingMetadata);
        var fakeOllamaClient = new FakeOllamaApiClient(summaryJson);
        var aiService = CreateAISummaryService(fakeOllamaClient);
        var ollamaOptions = Microsoft.Extensions.Options.Options.Create(new OllamaOptions { Enabled = true });
        var logger = new FakeLogger<CustomerSummaryService>();

        var service = new CustomerSummaryService(aiService, fakeGateway, ollamaOptions, logger);

        var request = new SummarizeCustomerContactRequest
        {
            RawContactText = "Some text",
            PersistResult = true,
            OverwriteExistingSummary = false
        };

        // Act
        var result = await service.SummarizeAsync(testCustomerId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testCustomerId, result.CustomerId);
        Assert.True(result.ExistingCustomerSummaryPresent);
        Assert.False(result.Persisted); // Should NOT persist because existing summary is present and overwrite is false
        Assert.False(fakeGateway.UpdateCalled); // Update should NOT have been called
    }

    [Fact]
    public async Task SummarizeAsync_Persists_WhenExistingSummaryPresentAndOverwriteTrue()
    {
        // Arrange
        var testCustomerId = Guid.NewGuid();
        var existingMetadata = """{"CustomerCode":"C-1001","AiContactSummary":{"CompanyName":"Old Corp"}}""";
        var summaryJson = """{"company_name":"New Corp","address":"","phone":"","fax":"","attention_to":"","detected_language":"en"}""";

        var fakeGateway = new FakeCustomerGateway(testCustomerId, existingMetadata);
        var fakeOllamaClient = new FakeOllamaApiClient(summaryJson);
        var aiService = CreateAISummaryService(fakeOllamaClient);
        var ollamaOptions = Microsoft.Extensions.Options.Options.Create(new OllamaOptions { Enabled = true });
        var logger = new FakeLogger<CustomerSummaryService>();

        var service = new CustomerSummaryService(aiService, fakeGateway, ollamaOptions, logger);

        var request = new SummarizeCustomerContactRequest
        {
            RawContactText = "Some text",
            PersistResult = true,
            OverwriteExistingSummary = true
        };

        // Act
        var result = await service.SummarizeAsync(testCustomerId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testCustomerId, result.CustomerId);
        Assert.True(result.ExistingCustomerSummaryPresent);
        Assert.True(result.Persisted); // SHOULD persist because overwrite is true
        Assert.True(fakeGateway.UpdateCalled); // Update SHOULD have been called
    }

    private static AISummaryService CreateAISummaryService(IOllamaApiClient client)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new OllamaOptions
        {
            BaseUrl = "http://localhost:11434",
            DefaultModel = "llama3",
            TimeoutSeconds = 30,
            Enabled = true
        });
        var logger = new FakeLogger<AISummaryService>();
        return new AISummaryService(client, options, logger);
    }

    /// <summary>
    /// Fake gateway that tracks whether Update was called.
    /// </summary>
    private sealed class FakeCustomerGateway : ICustomerStoredProcedureGateway
    {
        private readonly Guid _customerId;
        private readonly string _metadataXml;

        public bool UpdateCalled { get; private set; }

        public FakeCustomerGateway(Guid customerId, string metadataXml)
        {
            _customerId = customerId;
            _metadataXml = metadataXml;
        }

        public Task<CustomerStoredProcedureRecord?> SelectAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == _customerId)
            {
                return Task.FromResult(new CustomerStoredProcedureRecord(
                    CustomerId: id,
                    CustomerName: "Test Customer",
                    LoginAccount: "test",
                    LoginPassword: "password",
                    MetadataXml: _metadataXml,
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
            UpdateCalled = true;
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
