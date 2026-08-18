namespace JB2026.Api.Models;

public sealed class JobOrderPrintRequest
{
    public string Layout { get; init; } = "default";

    public bool NoPicture { get; init; }

    public bool NoProductDetails { get; init; }

    public bool NoRemarks { get; init; }

    public IReadOnlyList<int> SelectedWorkflowIndices { get; init; } = Array.Empty<int>();
}
