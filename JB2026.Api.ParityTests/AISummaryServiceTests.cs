using System.Runtime.CompilerServices;
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

public sealed class AISummaryServiceTests
{
    [Fact]
    public async Task SummarizeAsync_ReturnsNull_WhenInputIsEmpty()
    {
        var service = CreateService();
        var result = await service.SummarizeAsync("", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_WhenInputIsWhitespace()
    {
        var service = CreateService();
        var result = await service.SummarizeAsync("   ", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsSummary_WhenOllamaReturnsValidJson()
    {
        var expectedJson = """{"company_name":"Acme Corp","address":"123 Main St","phone":"+1 555-1234","fax":"+1 555-5678","attention_to":"John Doe","detected_language":"en"}""";
        var client = new FakeOllamaApiClient(expectedJson);
        var service = CreateService(client);

        var result = await service.SummarizeAsync("Acme Corp\nJohn Doe\n123 Main St\nTel: +1 555-1234", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Acme Corp", result!.CompanyName);
        Assert.Equal("123 Main St", result.Address);
        Assert.Equal("+1 555-1234", result.Phone);
        Assert.Equal("+1 555-5678", result.Fax);
        Assert.Equal("John Doe", result.AttentionTo);
        Assert.Equal("en", result.DetectedLanguage);
    }

    [Fact]
    public async Task SummarizeAsync_ParsesMarkdownWrappedJson()
    {
        var markdownWrapped = "```json\n{\"company_name\":\"Sodexo GmbH\",\"address\":\"Musterstraße 1\",\"phone\":\"+49 30 123456\",\"fax\":\"\",\"attention_to\":\"\",\"detected_language\":\"de\"}\n```";
        var client = new FakeOllamaApiClient(markdownWrapped);
        var service = CreateService(client);

        var result = await service.SummarizeAsync("Sodexo GmbH\nMusterstraße 1\nTel: +49 30 123456", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Sodexo GmbH", result!.CompanyName);
        Assert.Equal("Musterstraße 1", result.Address);
        Assert.Equal("+49 30 123456", result.Phone);
        Assert.Equal("de", result.DetectedLanguage);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_WhenOllamaReturnsMalformedJson()
    {
        var client = new FakeOllamaApiClient("not valid json at all");
        var service = CreateService(client);

        var result = await service.SummarizeAsync("some text", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_WhenOllamaReturnsEmptyResponse()
    {
        var client = new FakeOllamaApiClient("");
        var service = CreateService(client);

        var result = await service.SummarizeAsync("some text", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_WhenOllamaThrowsTimeout()
    {
        var client = new FakeOllamaApiClient(timeout: true);
        var service = CreateService(client);

        var result = await service.SummarizeAsync("some text", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_WhenOllamaThrowsHttpError()
    {
        var client = new FakeOllamaApiClient(httpError: true);
        var service = CreateService(client);

        var result = await service.SummarizeAsync("some text", CancellationToken.None);

        Assert.Null(result);
    }

    private static AISummaryService CreateService(IOllamaApiClient? client = null)
    {
        client ??= new FakeOllamaApiClient("""{"company_name":"Test","address":"","phone":"","fax":"","attention_to":"","detected_language":"en"}""");
        var options = Microsoft.Extensions.Options.Options.Create(new OllamaOptions
        {
            BaseUrl = "http://localhost:11434",
            DefaultModel = "llama3",
            TimeoutSeconds = 30
        });
        var logger = new FakeLogger<AISummaryService>();
        return new AISummaryService(client, options, logger);
    }

    private sealed class FakeOllamaApiClient : IOllamaApiClient
    {
        private readonly string? _response;
        private readonly bool _timeout;
        private readonly bool _httpError;

        public FakeOllamaApiClient(string response) { _response = response; }
        public FakeOllamaApiClient(bool timeout = false, bool httpError = false)
        {
            _timeout = timeout;
            _httpError = httpError;
        }

        public Uri Uri { get => new("http://localhost:11434"); set { } }
        public string SelectedModel { get => "llama3"; set { } }

        public IAsyncEnumerable<GenerateResponseStream> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken)
        {
            if (_timeout)
            {
                throw new OperationCanceledException("Simulated timeout");
            }
            if (_httpError)
            {
                throw new HttpRequestException("Simulated HTTP error");
            }

            var stream = new GenerateResponseStream
            {
                Response = _response ?? string.Empty,
                Done = true
            };

            return AsyncEnumerableSingle(stream, cancellationToken);
        }

        private static async IAsyncEnumerable<GenerateResponseStream> AsyncEnumerableSingle(
            GenerateResponseStream item,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        public IAsyncEnumerable<ChatResponseStream> ChatAsync(ChatRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();
        public Task CopyModelAsync(CopyModelRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
        public IAsyncEnumerable<CreateModelResponse?> CreateModelAsync(CreateModelRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();
        public Task DeleteModelAsync(DeleteModelRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
        public Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();
        public Task<IEnumerable<Model>> ListLocalModelsAsync(CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<Model>());
        public Task<IEnumerable<RunningModel>> ListRunningModelsAsync(CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<RunningModel>());
        public IAsyncEnumerable<PullModelResponse?> PullModelAsync(PullModelRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();
        public IAsyncEnumerable<PushModelResponse?> PushModelAsync(PushModelRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();
        public Task<ShowModelResponse> ShowModelAsync(ShowModelRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();
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

public sealed class FakeLogger<T> : ILogger<T>
{
    public List<string> LoggedMessages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LoggedMessages.Add($"[{logLevel}] {formatter(state, exception)}");
    }
}
