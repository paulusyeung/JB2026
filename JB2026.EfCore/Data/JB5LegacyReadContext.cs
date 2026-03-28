using Microsoft.EntityFrameworkCore;

namespace JB2026.EfCore.Data;

public sealed class JB5LegacyReadContext : JB5LegacyContext
{
    public JB5LegacyReadContext(DbContextOptions<JB5LegacyReadContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}
