using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class Product
{
    public Guid ProductId { get; set; }

    public Guid? CategoryId { get; set; }

    public string? StockNumber { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductName { get; set; }

    public string? Description { get; set; }

    public string? Remarks { get; set; }

    public int MOQ { get; set; }

    public int Balance { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal COGS { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual Z_Category? Category { get; set; }

    public virtual ICollection<ProductAttachment> ProductAttachments { get; set; } = new List<ProductAttachment>();

    public virtual ICollection<StockInOut> StockInOuts { get; set; } = new List<StockInOut>();
}
