using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwAvailableJobOrderList
{
    public Guid OrderId { get; set; }

    public int OrderType { get; set; }

    public string? OrderNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? OrderTitle { get; set; }

    public int ScheduleCount { get; set; }
}
