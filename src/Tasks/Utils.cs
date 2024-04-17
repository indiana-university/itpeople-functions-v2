using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;

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
                return await resp.Content.ReadFromJsonAsync<T>();
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

        public static async Task StartOrchestratorAsSingleton(TimerInfo timer, DurableTaskClient starter, string orchestratorName)
        {
            // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
            var existingInstance = await starter.GetInstanceAsync(orchestratorName);
            if (existingInstance == null 
                || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed 
                || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed 
                || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
            {
                // An instance with the specified ID doesn't exist or an existing one stopped running, create one.
                await starter.ScheduleNewOrchestrationInstanceAsync(orchestratorName);
                Logging.GetLogger(orchestratorName, timer)
                    .Debug($"Started orchestration '{orchestratorName}'.");
            }
            else
            {
                Logging.GetLogger(orchestratorName, timer)
                    .Warning($"Orchestration not started; current status is '{existingInstance.RuntimeStatus}'.");
            }
        }

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> sequence, int size) {
            List<T> partition = new List<T>(size);
            foreach(var item in sequence) {
                partition.Add(item);
                if (partition.Count == size) {
                    yield return partition;
                    partition = new List<T>(size);
                }
            }
            if (partition.Count > 0)
                yield return partition;
        }
    }
}
