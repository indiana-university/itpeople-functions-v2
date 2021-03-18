using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Tasks
{
    public static class Buildings
    {
        // Runs at 40 minutes past every hour (00:40 AM, 01:40 AM, 02:40 AM, ...)
        [FunctionName(nameof(ScheduledBuildingsUpdate))]
        public static async Task ScheduledBuildingsUpdate([TimerTrigger("0 40 * * * *")]TimerInfo myTimer, 
            [DurableClient] IDurableOrchestrationClient starter)
        {
            string instanceId = await starter.StartNewAsync(nameof(BuildingsUpdateOrchestrator), null);
            Logging.GetLogger(instanceId, nameof(ScheduledBuildingsUpdate), myTimer)
               .Debug("Started scheduled buildings update.");
        }

        [FunctionName(nameof(BuildingsUpdateOrchestrator))]
        public static async Task BuildingsUpdateOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                var buildings = await context.CallActivityWithRetryAsync<IEnumerable<DenodoBuilding>>(
                    nameof(FetchBuildingsFromDenodo), RetryOptions, null);
                
                var tasks = buildings.Select(b =>
                    context.CallActivityWithRetryAsync(
                        nameof(AddOrUpdateBuildingRecords), RetryOptions, b));
                
                await Task.WhenAll(tasks);

                Logging.GetLogger(context).Debug("Finished buildings update.");
            }
            catch (Exception ex)
            {
                Logging.GetLogger(context).Error(ex, "Buildings update failed with exception.");
                throw;
            }
        }

        // Fetch all buildings from the Denodo view maintianed by UITS Facilities
        [FunctionName(nameof(FetchBuildingsFromDenodo))]
        public static async Task<IEnumerable<DenodoBuilding>> FetchBuildingsFromDenodo([ActivityTrigger] IDurableActivityContext context)
        {
            var denodoUrl = Utils.Env("DenodoBuildingsViewUrl", required: true);
            var denodoUser = Utils.Env("DenodoBuildingsViewUser", required: true);
            var denodoPwd = Utils.Env("DenodoBuildingsViewPassword", required: true);
            var basicAuth = Convert.ToBase64String(
                System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(
                    $"{denodoUser}:{denodoPwd}"
                )
            );
            var req = new HttpRequestMessage(HttpMethod.Get, denodoUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);            
            var resp = await Utils.HttpClient.SendAsync(req);
            var body = await Utils.DeserializeResponse<DenodoResponse<DenodoBuilding>>(nameof(FetchBuildingsFromDenodo), resp, "fetch buildings from Denodo view");
            return body.Elements;
        }

        // Add new Builing records to the IT People database.
        // If a building with the same code already exists then update its properties.
        [FunctionName(nameof(AddOrUpdateBuildingRecords))]
        public static async Task AddOrUpdateBuildingRecords([ActivityTrigger] IDurableActivityContext context)
        {
            var bld = context.GetInput<DenodoBuilding>();
            await Utils.DatabaseCommand(nameof(AddOrUpdateBuildingRecords), $"Add/update building with code {bld.BuildingCode}", async db => {
                var record = await db.Buildings.SingleOrDefaultAsync(b => b.Code == bld.BuildingCode);
                if (record == null)
                {
                    Logging.GetLogger(nameof(AddOrUpdateBuildingRecords), bld).Information($"Adding building with code {bld.BuildingCode}");
                    record = new Building();
                    db.Buildings.Add(record);
                }
                bld.MapToBuilding(record);
                await db.SaveChangesAsync();
            });
        }

        private static HttpClient HttpClient = new HttpClient();

        private static RetryOptions RetryOptions = new RetryOptions(
            firstRetryInterval: TimeSpan.FromSeconds(5),
            maxNumberOfAttempts: 3);

        
    }
}
