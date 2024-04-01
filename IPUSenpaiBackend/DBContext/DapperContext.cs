using System.Data;
using Npgsql;

namespace IPUSenpaiBackend.DBContext;

public class DapperContext : IDapperContext
{
    private readonly string? _connectionString;
    private readonly ILogger<DapperContext> _logger;
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
        return new NpgsqlConnection(_connectionString);
    }
}