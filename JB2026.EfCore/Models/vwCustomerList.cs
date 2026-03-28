using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwCustomerList
{
    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string LoginAccount { get; set; } = null!;

    public string LoginPassword { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }
}
