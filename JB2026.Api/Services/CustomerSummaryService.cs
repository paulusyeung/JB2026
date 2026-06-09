using System.Text.Json;
using System.Text.Json.Nodes;
using JB2026.Api.Models;
using JB2026.Api.Options;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services;

public sealed class CustomerSummaryService
{
    private const string AiContactSummaryKey = "AiContactSummary";

    private readonly AISummaryService _aiSummaryService;
    private readonly ICustomerStoredProcedureGateway _customerGateway;
    private readonly IOptions<OllamaOptions> _ollamaOptions;
    private readonly ILogger<CustomerSummaryService> _logger;

    public CustomerSummaryService(
        AISummaryService aiSummaryService,
        ICustomerStoredProcedureGateway customerGateway,
        IOptions<OllamaOptions> ollamaOptions,
        ILogger<CustomerSummaryService> logger)
    {
        _aiSummaryService = aiSummaryService;
        _customerGateway = customerGateway;
        _ollamaOptions = ollamaOptions;
        _logger = logger;
    }

    public async Task<SummarizeCustomerContactResponse?> SummarizeAsync(
        Guid customerId,
        SummarizeCustomerContactRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_ollamaOptions.Value.Enabled)
        {
            return new SummarizeCustomerContactResponse
            {
                CustomerId = customerId,
                Summary = new ContactInfoSummary(),
                Persisted = false,
                ExistingCustomerSummaryPresent = false,
                ErrorMessage = "AI summarization is currently disabled."
            };
        }

        var customer = await _customerGateway.SelectAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var hasExistingSummary = HasExistingAiSummary(customer.MetadataXml);

        var summary = await _aiSummaryService.SummarizeAsync(request.RawContactText, cancellationToken);
        if (summary is null)
        {
            return new SummarizeCustomerContactResponse
            {
                CustomerId = customerId,
                Summary = new ContactInfoSummary(),
                Persisted = false,
                ExistingCustomerSummaryPresent = hasExistingSummary
            };
        }

        var persisted = false;
        if (request.PersistResult)
        {
            if (hasExistingSummary && !request.OverwriteExistingSummary)
            {
                _logger.LogInformation(
                    "Skipping persistence for customer {CustomerId}: existing AiContactSummary present and OverwriteExistingSummary is false.",
                    customerId);
            }
            else
            {
                var mergedMetadata = MergeAiSummaryIntoMetadata(customer.MetadataXml, summary);
                if (mergedMetadata is not null)
                {
                    var updateRequest = new UpdateCustomerStoredProcedureRequest(
                        CustomerId: customerId,
                        CustomerName: customer.CustomerName,
                        LoginAccount: customer.LoginAccount,
                        LoginPassword: customer.LoginPassword,
                        MetadataXml: mergedMetadata,
                        CreatedOn: customer.CreatedOn,
                        CreatedBy: customer.CreatedBy,
                        ModifiedOn: DateTime.Now,
                        ModifiedBy: customer.ModifiedBy,
                        Retired: customer.Retired,
                        RetiredOn: customer.RetiredOn,
                        RetiredBy: customer.RetiredBy);

                    await _customerGateway.UpdateAsync(updateRequest, cancellationToken);
                    persisted = true;
                    hasExistingSummary = true;

                    _logger.LogInformation(
                        "Persisted AiContactSummary for customer {CustomerId}.", customerId);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to merge AiContactSummary for customer {CustomerId}: malformed metadata.",
                        customerId);
                }
            }
        }

        return new SummarizeCustomerContactResponse
        {
            CustomerId = customerId,
            Summary = summary,
            Persisted = persisted,
            ExistingCustomerSummaryPresent = hasExistingSummary
        };
    }

    private static bool HasExistingAiSummary(string? metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataXml.Trim());
            return document.RootElement.TryGetProperty(AiContactSummaryKey, out _);
        }
        catch
        {
            return false;
        }
    }

    public static string? MergeAiSummaryIntoMetadata(string? existingMetadataXml, ContactInfoSummary summary)
    {
        JsonObject root;

        if (string.IsNullOrWhiteSpace(existingMetadataXml))
        {
            root = [];
        }
        else
        {
            try
            {
                var parsed = JsonNode.Parse(existingMetadataXml.Trim());
                if (parsed is JsonObject obj)
                {
                    root = obj;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        var summaryObj = new JsonObject
        {
            ["CompanyName"] = summary.CompanyName,
            ["Address"] = summary.Address,
            ["Phone"] = summary.Phone,
            ["Fax"] = summary.Fax,
            ["AttentionTo"] = summary.AttentionTo,
            ["DetectedLanguage"] = summary.DetectedLanguage
        };

        root[AiContactSummaryKey] = summaryObj;

        return root.ToJsonString();
    }
}
