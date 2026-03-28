using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class Z_Category
{
    public Guid CategoryId { get; set; }

    public string? CategoryCode { get; set; }

    public string? CategoryName { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
