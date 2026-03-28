using Microsoft.EntityFrameworkCore;

namespace JB2026.EfCore.Data;

public sealed class JB5LegacyWriteContext : JB5LegacyContext
{
    public JB5LegacyWriteContext(DbContextOptions<JB5LegacyWriteContext> options)
        : base(options)
    {
    }
}
