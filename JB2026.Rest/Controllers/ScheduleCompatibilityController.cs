using System.Security.Claims;
using System.Net;
using System.Text.RegularExpressions;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using JB2026.Rest.Helpers;
using JB2026.Rest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class ScheduleCompatibilityController : ControllerBase
{
    private const int UserRoleManager = 3;
    private const int UserRoleAdmin = 4;
    private const int WorkflowReadyStatus = 2;

    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IFcmEventHelperService _fcmEventHelper;

    public ScheduleCompatibilityController(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IFcmEventHelperService fcmEventHelper)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _fcmEventHelper = fcmEventHelper;
    }

    [HttpGet("api/Schedule/{scheduleId:guid}")]
    public async Task<IActionResult> GetSchedule(Guid scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await _readContext.vwJobSchedule_OnAirLists
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ScheduleId == scheduleId, cancellationToken);

        if (schedule is null)
        {
            return NotFound();
        }

        var enriched = await EnrichOnAirRowsAsync([schedule], cancellationToken);
        return Ok(enriched[0]);
    }

    [HttpGet("api/Schedule/machine/{machine}")]
    public async Task<IActionResult> GetScheduleByMachine(string machine, CancellationToken cancellationToken)
    {
        var query = _readContext.vwJobSchedule_OnAirLists
            .AsNoTracking()
            .Where(x => x.OrderType == 0);

        if (string.Equals(machine, "0", StringComparison.OrdinalIgnoreCase))
        {
            var (role, alias) = await GetCurrentAccessAsync(cancellationToken);
            if (role == UserRoleManager && !string.IsNullOrWhiteSpace(alias))
            {
                query = query.Where(x => x.OrderedBy == alias);
            }
            else if (role != UserRoleAdmin)
            {
                query = query.Where(x => false);
            }
        }
        else
        {
            query = query.Where(x => x.MachineNumber == machine);
        }

        var list = await query
            .OrderBy(x => x.MachineNumber)
            .ThenByDescending(x => x.UrgencyLevel)
            .ThenBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        var enriched = await EnrichOnAirRowsAsync(list, cancellationToken);
        return Ok(enriched);
    }

    [HttpPost("api/Schedule/{orderId:guid}/{type:int}/{status:int}")]
    public async Task<IActionResult> PostRegister(Guid orderId, int type, int status, CancellationToken cancellationToken)
    {
        var workflow = await _writeContext.JobWorkflows
            .SingleOrDefaultAsync(x => x.OrderId == orderId && x.WorkIndex == type, cancellationToken);

        if (workflow is null)
        {
            return NotFound();
        }

        workflow.WorkStatus = status;
        workflow.ModifiedOn = DateTime.Now;
        await _writeContext.SaveChangesAsync(cancellationToken);

        if (status == WorkflowReadyStatus)
        {
            if (type == 0)
            {
                await _fcmEventHelper.NotifyReadyPaperAsync(orderId, cancellationToken);
            }
            else if (type == 1)
            {
                await _fcmEventHelper.NotifyReadyPlateAsync(orderId, cancellationToken);
            }
        }

        return Ok();
    }

    [HttpGet("api/Schedule/Scheduled/{machine:int}")]
    public async Task<IActionResult> GetScheduleScheduled(int machine, CancellationToken cancellationToken)
    {
        var machineText = machine.ToString();
        var jobs = await _readContext.vwJobScheduleList_OnAirs
            .AsNoTracking()
            .Where(x => x.MachineNumber == machineText)
            .OrderByDescending(x => x.UrgencyLevel)
            .ThenBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        return Ok(jobs);
    }

    [HttpGet("api/Schedule/Completed/{machine:int}")]
    public async Task<IActionResult> GetScheduleCompleted(int machine, CancellationToken cancellationToken)
    {
        var machineText = machine.ToString();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var jobs = await _readContext.vwJobScheduleLists
            .AsNoTracking()
            .Where(x =>
                x.OrderType == 0
                && x.MachineNumber == machineText
                && x.CompletedOn.HasValue
                && x.CompletedOn.Value >= today
                && x.CompletedOn.Value < tomorrow)
            .OrderByDescending(x => x.CompletedOn)
            .ToListAsync(cancellationToken);

        var enriched = await EnrichCompletedRowsAsync(jobs, cancellationToken);
        return Ok(enriched);
    }

    private async Task<List<VwJobScheduleEx>> EnrichOnAirRowsAsync(
        List<vwJobSchedule_OnAirList> source,
        CancellationToken cancellationToken)
    {
        var snapshot = await BuildSnapshotAsync(source.Select(x => x.OrderId), cancellationToken);
        return source.Select(row =>
        {
            var details = snapshot.TryGetValue(row.OrderId ?? Guid.Empty, out var value)
                ? value
                : ScheduleSnapshot.Empty;

            return new VwJobScheduleEx
            {
                OrderId = row.OrderId,
                OrderType = row.OrderType,
                OrderNumber = row.OrderNumber,
                CustomerName = row.CustomerName,
                OrderTitle = row.OrderTitle,
                ScheduleCount = row.ScheduleCount,
                Priority = row.Priority,
                MachineNumber = row.MachineNumber,
                Status = row.Status,
                ShouldReview = row.ShouldReview,
                ScheduleId = row.ScheduleId,
                UrgencyLevel = row.UrgencyLevel,
                OrderedBy = row.OrderedBy,
                PrintInfo_1 = details.PrintInfo1,
                PrintInfo_2 = details.PrintInfo2,
                PrintInfo_3 = details.PrintInfo3,
                Light_1 = details.Light1,
                Light_2 = details.Light2
            };
        }).ToList();
    }

    private async Task<List<VwJobScheduleEx>> EnrichCompletedRowsAsync(
        List<vwJobScheduleList> source,
        CancellationToken cancellationToken)
    {
        var snapshot = await BuildSnapshotAsync(source.Select(x => x.OrderId), cancellationToken);
        return source.Select(row =>
        {
            var details = snapshot.TryGetValue(row.OrderId ?? Guid.Empty, out var value)
                ? value
                : ScheduleSnapshot.Empty;

            return new VwJobScheduleEx
            {
                OrderId = row.OrderId,
                OrderType = row.OrderType,
                OrderNumber = row.OrderNumber,
                CustomerName = row.CustomerName,
                OrderTitle = row.OrderTitle,
                ScheduleCount = row.ScheduleCount,
                Priority = row.Priority,
                MachineNumber = row.MachineNumber,
                Status = row.Status,
                ScheduledOn = row.ScheduledOn,
                CompletedOn = row.CompletedOn,
                OrderedOn = row.OrderedOn,
                RequiredOn = row.RequiredOn,
                ShouldReview = row.ShouldReview,
                ScheduleId = Guid.Empty,
                UrgencyLevel = row.UrgencyLevel,
                OutputRef = row.OutputRef,
                PrintInfo_1 = details.PrintInfo1,
                PrintInfo_2 = details.PrintInfo2,
                PrintInfo_3 = details.PrintInfo3,
                Light_1 = details.Light1,
                Light_2 = details.Light2
            };
        }).ToList();
    }

    private async Task<Dictionary<Guid, ScheduleSnapshot>> BuildSnapshotAsync(
        IEnumerable<Guid?> orderIds,
        CancellationToken cancellationToken)
    {
        var ids = orderIds
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, ScheduleSnapshot>();
        }

        var orders = await _readContext.JobOrders
            .AsNoTracking()
            .Where(x => ids.Contains(x.OrderId))
            .Select(x => new { x.OrderId, x.ProductDetails, x.OrderTitle })
            .ToListAsync(cancellationToken);

        var workflowStatuses = await _readContext.JobWorkflows
            .AsNoTracking()
            .Where(x => ids.Contains(x.OrderId) && (x.WorkIndex == 0 || x.WorkIndex == 1))
            .Select(x => new { x.OrderId, x.WorkIndex, x.WorkStatus })
            .ToListAsync(cancellationToken);

        var workflowLookup = workflowStatuses
            .GroupBy(x => x.OrderId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(y => y.WorkIndex).ToList());

        var result = new Dictionary<Guid, ScheduleSnapshot>(ids.Count);
        foreach (var order in orders)
        {
            workflowLookup.TryGetValue(order.OrderId, out var workflow);
            var light1 = workflow?.FirstOrDefault(x => x.WorkIndex == 0)?.WorkStatus ?? 0;
            var light2 = workflow?.FirstOrDefault(x => x.WorkIndex == 1)?.WorkStatus ?? 0;

            var printInfo = ExtractPrintInfo(order.ProductDetails, order.OrderTitle);
            result[order.OrderId] = new ScheduleSnapshot(printInfo[0], printInfo[1], printInfo[2], light1, light2);
        }

        return result;
    }

    private static string[] ExtractPrintInfo(string? productDetails, string? orderTitle)
    {
        var plainText = StripHtml(productDetails);

        // Legacy (JB2015) ProductDetails comes in two shapes:
        //   _2016   : numbered sections, e.g. "3. 印刷" / "石數：12500石"
        //   _BF2016 : flat lines, e.g. "石數：12500石" anywhere in the text
        // 石數 belongs to the "印刷" section in the sectioned format.
        string paperScope = plainText;
        string printScope = plainText;
        if (HasNumberedSections(plainText))
        {
            paperScope = GetSection(plainText, "用紙");
            printScope = GetSection(plainText, "印刷");
        }

        var info1 = GetLabeledValue(paperScope, ["印張尺寸", "尺寸", "size"]);
        var info2 = GetLabeledValue(printScope, ["顏色", "color"]);
        var info3 = GetLabeledValue(printScope, ["石數", "石数", "數量", "数量", "qty", "quantity"]);

        if (string.IsNullOrWhiteSpace(info3))
        {
            info3 = orderTitle ?? string.Empty;
        }

        return [info1, info2, info3];
    }

    private static bool HasNumberedSections(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (var rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            var dotIndex = line.IndexOf('.');
            if (dotIndex > 0 && int.TryParse(line[..dotIndex].Trim(), out _))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetSection(string text, string title)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var buffer = new System.Text.StringBuilder();
        var inSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            // A section header is a numeric prefix followed by a dot, e.g. "3. 印刷".
            var dotIndex = line.IndexOf('.');
            if (dotIndex > 0 && int.TryParse(line[..dotIndex].Trim(), out _))
            {
                var headerTitle = line[(dotIndex + 1)..].Trim().TrimEnd(':', '：').Trim();
                if (headerTitle == title)
                {
                    inSection = true;
                    continue;
                }

                if (inSection)
                {
                    break;
                }
            }

            if (inSection)
            {
                buffer.AppendLine(line);
            }
        }

        return buffer.ToString();
    }

    private static string StripHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var noTags = Regex.Replace(input, "<.*?>", " ", RegexOptions.Singleline);
        return WebUtility.HtmlDecode(noTags);
    }

    private static string GetLabeledValue(string text, string[] labels)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            foreach (var label in labels)
            {
                if (!trimmed.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parts = trimmed.Split(new[] { ':', '：' }, 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    return parts[1].Trim();
                }
            }
        }

        return string.Empty;
    }

    private readonly record struct ScheduleSnapshot(
        string PrintInfo1,
        string PrintInfo2,
        string PrintInfo3,
        int Light1,
        int Light2)
    {
        public static ScheduleSnapshot Empty => new(string.Empty, string.Empty, string.Empty, 0, 0);
    }

    private async Task<(int Role, string Alias)> GetCurrentAccessAsync(CancellationToken cancellationToken)
    {
        var sid = ResolveCurrentSid();
        if (sid is null)
        {
            return (0, string.Empty);
        }

        var user = await _readContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserSid == sid.Value || x.UserId == sid.Value, cancellationToken);

        if (user is null)
        {
            return (0, string.Empty);
        }

        var userInfo = await _readContext.UserInfos
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == user.UserId || x.UserId == sid.Value, cancellationToken);

        return (userInfo?.UserRole ?? 0, user.Alias ?? string.Empty);
    }

    private Guid? ResolveCurrentSid()
    {
        var candidate = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(candidate, out var sid) ? sid : null;
    }
}
