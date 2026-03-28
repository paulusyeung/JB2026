using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class SystemInfo
{
    public Guid SystemId { get; set; }

    public string? OwnerName { get; set; }

    public string? MetadataXml { get; set; }
}
