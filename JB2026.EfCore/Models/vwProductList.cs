using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwProductList
{
    public string? CategoryCode { get; set; }

    public string? CategoryName { get; set; }

    public Guid ProductId { get; set; }

    public string? StockNumber { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductName { get; set; }

    public string? Description { get; set; }

    public int MOQ { get; set; }

    public int Balance { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal COGS { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public string? RetiredBy { get; set; }
}
