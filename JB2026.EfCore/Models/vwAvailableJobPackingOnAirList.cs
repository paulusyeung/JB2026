using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwAvailableJobPackingOnAirList
{
    public Guid? OrderId { get; set; }

    public int? OrderType { get; set; }

    public string? OrderNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? OrderTitle { get; set; }

    public int? OnAirCount { get; set; }

    public int? Priority { get; set; }

    public int? Status { get; set; }

    public Guid OnAirId { get; set; }
}
