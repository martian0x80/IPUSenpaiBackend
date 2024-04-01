using System.Data;

namespace IPUSenpaiBackend.DBContext;

public interface IDapperContext
{
    public IDbConnection CreateConnection();
}