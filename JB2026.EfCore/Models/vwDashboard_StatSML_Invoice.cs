using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwDashboard_StatSML_Invoice
{
    public string CustomerName { get; set; } = null!;

    public int? Year { get; set; }

    public int? Month { get; set; }

    public decimal? TAmount { get; set; }

    public int? TCount { get; set; }
}
