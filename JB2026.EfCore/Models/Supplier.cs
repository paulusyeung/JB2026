using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class Supplier
{
    public Guid SupplierId { get; set; }

    public string? SupplierName { get; set; }

    public string? LoginAccount { get; set; }

    public string? LoginPassword { get; set; }

    public string? MetadataXml { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }
}
