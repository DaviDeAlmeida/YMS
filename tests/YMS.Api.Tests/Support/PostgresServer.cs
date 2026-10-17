using Npgsql;
using Testcontainers.PostgreSql;

namespace YMS.Api.Tests.Support;

/// <summary>
/// One PostgreSQL container for the whole test run, started on first use; each factory gets its own database on it.
/// Testcontainers removes the container when the run ends.
/// </summary>
public static class PostgresServer
{
    private static readonly PostgreSqlContainer Container = new PostgreSqlBuilder("postgres:18-alpine").Build();
    private static readonly Lazy<Task> Started = new(() => Container.StartAsync());

    /// <summary>Connection string to a fresh, not yet created database; the API's start-up migration creates it.</summary>
    public static async Task<string> NewDatabaseAsync()
    {
        await Started.Value;
        return new NpgsqlConnectionStringBuilder(Container.GetConnectionString()) { Database = $"yms_tests_{Guid.NewGuid():N}" }
            .ConnectionString;
    }
}
