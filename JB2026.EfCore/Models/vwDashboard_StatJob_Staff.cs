using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwDashboard_StatJob_Staff
{
    public string SalesRep { get; set; } = null!;

    public int? Year { get; set; }

    public int? Month { get; set; }

    public decimal? TCost { get; set; }

    public decimal? TBill { get; set; }

    public int? TCount { get; set; }
}
