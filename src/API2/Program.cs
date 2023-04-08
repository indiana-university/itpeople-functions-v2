using Database;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace API2
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker => worker.UseNewtonsoftJson(Models.Json.JsonSerializerSettings))
                .Build();

            MigrateDatabaseToLatest();

            host.Run();
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