using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class FCMHistory
{
    public Guid FCMHistoryId { get; set; }

    public string? MessageTitle { get; set; }

    public string? MessageBody { get; set; }

    public DateTime DeliveredOn { get; set; }

    public string? Topic { get; set; }

    public string? RecipientList { get; set; }

    public string? UserIdList { get; set; }
}
