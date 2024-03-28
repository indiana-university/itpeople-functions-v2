using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using Database;
using System.Threading.Tasks;

namespace API
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services => {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();
                })
                .Build();

            MigrateDatabaseToLatest();

            host.Run();
            await Task.CompletedTask;
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
                Console.Error.WriteLine($"\tMOAR: {e.StackTrace}");
                // throw new Exception($"Error when migrating database.", e);
                throw;
            }
        }
    }
}