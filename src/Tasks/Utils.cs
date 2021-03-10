using System;
using System.Net.Http;
using System.Threading.Tasks;
using Database;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Tasks
{
    public static class Utils
    {
        public static string Env(string key, bool required=false)
        {
            var value = System.Environment.GetEnvironmentVariable(key);
            if (required && string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"Missing required environment setting: {key}");
            }
            return value;
        }

        public static async Task<T> DeserializeResponse<T>(IDurableActivityContext ctx, HttpResponseMessage resp, string description)
        {
            if (false == resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                var err = new {url=resp.RequestMessage.RequestUri, status=resp.StatusCode, msg=msg??("none")};
                Logging.GetLogger(ctx, err).Error($"Failed HTTP operation to {description}");
                throw new Exception($"Failed HTTP operation to {description}. URL: {err.url} Status: {err.status}. Message: {err.msg}");
            }
            return await resp.Content.ReadAsAsync<T>();
        }

        public static async Task DatabaseCommand(IDurableActivityContext ctx, string description, Func<PeopleContext, Task> op)
        {
            try
            {
                var connStr = Utils.Env("DatabaseConnectionString", required: true);
                using (var db = PeopleContext.Create(connStr))
                {
                    await op(db);
                }
            }
            catch (Exception ex)
            {
                Logging.GetLogger(ctx).Error(ex, $"Failed to execute database command: {description}.");
                throw;
            }
        }

        public static async Task<T> DatabaseQuery<T>(IDurableActivityContext ctx, string description, Func<PeopleContext, Task<T>> op)
        {
            try
            {
                var connStr = Utils.Env("DatabaseConnectionString", required: true);
                using (var db = PeopleContext.Create(connStr))
                {
                    return await op(db);
                }
            }
            catch (Exception ex)
            {
                Logging.GetLogger(ctx).Error(ex, $"Failed to execute database query: {description}.");
                throw;
            }
        }
    }
}
