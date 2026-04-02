using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface IQuotationRepository
{
    IReadOnlyList<QuotationListItemResponse> GetRange(DateOnly startOn, int days);

    IReadOnlyList<QuotationListItemResponse> Search(string keyword);

    (byte[] Content, string FileName)? GetPdf(Guid headerId);

    QuotationListItemResponse Create(UpsertQuotationRequest request, string actor);

    QuotationListItemResponse? Update(Guid headerId, UpsertQuotationRequest request, string actor);
}
