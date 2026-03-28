using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class StockInOut
{
    public Guid InOutId { get; set; }

    public Guid? ProductId { get; set; }

    public DateTime InOutDate { get; set; }

    public string? Reference { get; set; }

    public int Qty { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public virtual Product? Product { get; set; }
}
