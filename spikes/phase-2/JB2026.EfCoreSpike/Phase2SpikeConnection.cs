namespace JB2026.EfCoreSpike;

public static class Phase2SpikeConnection
{
    public const string DatabaseName = "JB2026_Phase2Spike";

    public const string LocalDbServer = "(localdb)\\MSSQLLocalDB";

    public static string ConnectionString =>
        $"Server={LocalDbServer};Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public static string MasterConnectionString =>
        $"Server={LocalDbServer};Database=master;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
}