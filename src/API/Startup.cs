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
            MigrateDatabaseToLatest();
        }

        private static void MigrateDatabaseToLatest()
        {
            try
            {
                using (var context = PeopleContext.Create())
                {
                    context.Database.Migrate();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error when migrating database: {e.Message}");
                throw new Exception($"Error when migrating database: {e.Message}");
            }
        }
    }
}