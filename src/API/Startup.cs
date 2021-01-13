using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Database;

[assembly: FunctionsStartup(typeof(API.Startup))]
namespace API
{
    class Startup : FunctionsStartup
    {
        public static string Env(string key)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception($"Required environment variable '{key}' was not found.");
            return value;
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            MigrateDatabaseToLatest();
        }

        private static void MigrateDatabaseToLatest()
        {
            using (var context = PeopleContext.Create())
            {
                try
                {
                    context.Database.Migrate();
                }
                catch (Exception e)
                {
                    throw new Exception($"Error when migrating database: {e.Message}");
                }
            }
        }
    }
}