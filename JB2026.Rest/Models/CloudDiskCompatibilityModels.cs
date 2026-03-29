namespace JB2026.Rest.Models;

public class CloudDiskResourceInfo
{
    public int Idx { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public int Size { get; set; }
    public string? ETag { get; set; }
    public string? ContentType { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime Created { get; set; }
    public int QuotaUsed { get; set; }
    public int QuotaAvailable { get; set; }
    public bool Checked { get; set; }
}

public sealed class CloudDiskResourceInfoEx : CloudDiskResourceInfo
{
    public string? CupsJobId { get; set; }
    public string? CupsJobTitle { get; set; }
    public string? PlateSize { get; set; }
    public string? VpsFileName { get; set; }
    public int VpsPrintQueueId { get; set; }
}

public sealed class CloudDiskActionEmailRequest
{
    public string? Recipient { get; set; }
    public string? Remarks { get; set; }
    public bool ExpiryChecked { get; set; }
    public DateTime? ExpiredOn { get; set; }
    public string? Password { get; set; }
    public List<CloudDiskResourceInfo>? Items { get; set; }
}

public sealed class CloudDiskActionReprintRequest
{
    public int ClientId { get; set; }
    public string? Remarks { get; set; }
    public List<CloudDiskResourceInfoEx>? Items { get; set; }
}

public sealed class CloudDiskActionOutputRequest
{
    public int ClientId { get; set; }
    public int Priority { get; set; }
    public string? Workshop { get; set; }
    public bool Pickup { get; set; }
    public bool Deliver { get; set; }
    public int DeliverTo { get; set; }
    public string? Remarks { get; set; }
    public List<CloudDiskResourceInfoEx>? Items { get; set; }
}