using System;
using System.Net;
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

        public static async Task<T> DeserializeResponse<T>(string functionName, HttpResponseMessage resp, string description)
        {
            var msg = await resp.Content.ReadAsStringAsync();
            var err = new {url=resp.RequestMessage.RequestUri, status=resp.StatusCode, msg=msg??("none")};

            if (false == resp.IsSuccessStatusCode)
            {
                Logging.GetLogger(functionName, err).Error($"Failed HTTP operation to {description}");
                throw new Exception($"Failed HTTP operation to {description}. URL: {err.url} Status: {err.status}. Message: {err.msg}");
            }
            
            try
            {
                return await resp.Content.ReadAsAsync<T>();
            }
            catch (Exception ex)
            {
                Logging.GetLogger(functionName, err).Error($"Failed to deserialize HTTP response for {description}");
                throw new Exception($"Failed to deserialize HTTP response for {description}. URL: {err.url} Status: {err.status}. Message: {err.msg}", ex);
            }
        }

        public static async Task DatabaseCommand(string functionName, string description, Func<PeopleContext, Task> op)
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
                string msg = $"Failed to execute database command: {description}.";
                Logging.GetLogger(functionName).Error(ex, msg);
                throw new Exception (msg, ex);
            }
        }

        public static async Task<T> DatabaseQuery<T>(string functionName, string description, Func<PeopleContext, Task<T>> op)
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
                string msg = $"Failed to execute database query: {description}.";
                Logging.GetLogger(functionName).Error(ex, msg);
                throw new Exception(msg, ex);
            }
        }

        public static HttpClient HttpClient = new HttpClient(
            new HttpClientHandler(){
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }
        );
    }
}
