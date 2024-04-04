using System.Data;
using Npgsql;
using StackExchange.Profiling;

namespace IPUSenpaiBackend.DBContext;

public class DapperContext : IDapperContext
{
    private readonly string? _connectionString;
    private readonly ILogger _logger;
    private static int _count = 0;

    public DapperContext(IConfiguration configuration, ILogger<DapperContext> logger)
    {
        _connectionString = configuration.GetConnectionString("CONNSTR");
        _logger = logger;
        _logger.LogInformation("DapperContext created");
    }

    public IDbConnection CreateConnection()
    {
        _logger.LogInformation($"Connection ID: {++_count} created");
        // Leaving this here, ProfiledDbConnection is very minimal overhead anyway
        return new StackExchange.Profiling.Data.ProfiledDbConnection(new NpgsqlConnection(_connectionString),
            MiniProfiler.Current);
    }
}