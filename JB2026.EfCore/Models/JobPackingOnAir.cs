using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class JobPackingOnAir
{
    public Guid OnAirId { get; set; }

    public Guid OrderId { get; set; }

    public DateTime OnAiredOn { get; set; }

    public Guid OnAiredBy { get; set; }

    public int? Priority { get; set; }

    public int? Status { get; set; }

    public DateTime CompletedOn { get; set; }

    public Guid? CompletedBy { get; set; }

    public bool? Cancelled { get; set; }

    public DateTime CancelledOn { get; set; }

    public Guid? CancelledBy { get; set; }

    public int? RescheduledCount { get; set; }

    public DateTime RescheduledOn { get; set; }

    public Guid? RescheduledBy { get; set; }

    public virtual JobOrder Order { get; set; } = null!;
}
