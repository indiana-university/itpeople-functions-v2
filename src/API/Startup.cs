using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using Database;

[assembly: FunctionsStartup(typeof(API.Startup))]
namespace API
{
    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Debug, true, true);
            MigrateDatabaseToLatest();
        }

        private static void MigrateDatabaseToLatest()
        {            
            try
            {
                Console.Error.WriteLine($"[Startup] Creating database context for migration...");
                using (var context = PeopleContext.Create())
                {
                    Console.Error.WriteLine($"[Startup] Migrating database...");
                    context.Database.Migrate();
                    Console.Error.WriteLine($"[Startup] Migrated database.");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error when migrating database: {e.Message}");
                throw new Exception($"Error when migrating database.", e);
            }
        }
    }
}