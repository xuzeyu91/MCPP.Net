using Microsoft.EntityFrameworkCore;

namespace MCPP.Net.Database
{
    /// <summary>
    /// DatabaseExtensions
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// </summary>
        public static void Configure(this DbContextOptionsBuilder options, IConfiguration configuration)
        {
            var databaseOptions = configuration.GetSection("Database").Get<DatabaseOptions>()!;

            switch (databaseOptions.DbType.ToLower())
            {
                case "sqlite":
                    options.UseSqlite(databaseOptions.ConnectionString);
                    break;
                case "postgresql":
                    options.UseNpgsql(databaseOptions.ConnectionString);
                    break;
                default:
                    throw new NotSupportedException($"Database type '{databaseOptions.DbType}' is not supported.");
            }
        }
    }
}
