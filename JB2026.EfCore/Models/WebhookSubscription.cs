using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class WebhookSubscription
{
    public int Id { get; set; }

    public string Url { get; set; } = null!;

    public string EventTypes { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
