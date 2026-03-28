using System;
using System.Collections.Generic;

namespace JB2026.EfCoreSpike.Models;

public partial class JobOrder
{
    public Guid OrderId { get; set; }

    public int OrderType { get; set; }

    public string OrderNumber { get; set; } = null!;

    public int? JobNumber { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerRef { get; set; }

    public string OrderTitle { get; set; } = null!;

    public string? ProductCode { get; set; }

    public string? ProductStyle { get; set; }

    public DateTime? OrderedOn { get; set; }

    public string OrderedBy { get; set; } = null!;

    public DateTime? RequiredOn { get; set; }

    public string? Remarks { get; set; }

    public decimal? Qty { get; set; }

    public int Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual ICollection<JobAttachment> JobAttachments { get; set; } = new List<JobAttachment>();

    public virtual ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();

    public virtual ICollection<JobWorkflow> JobWorkflows { get; set; } = new List<JobWorkflow>();
}
