using Dapper;
using Npgsql;

namespace BranchBooking.Infrastructure.Persistence
{
    public class DbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(string connectionString)
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            _connectionString = connectionString;
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
