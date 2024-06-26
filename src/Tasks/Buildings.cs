using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;

namespace Tasks
{
    public static class Buildings
    {
        // Runs at 30 minutes past every hour (00:30 AM, 01:30 AM, 02:30 AM, ...)
        [Function(nameof(ScheduledBuildingsUpdate))]
        public static Task ScheduledBuildingsUpdate([TimerTrigger("0 30 * * * *")]TimerInfo timer, 
            [DurableClient] DurableTaskClient starter)
            => Utils.StartOrchestratorAsSingleton(timer, starter, nameof(BuildingsUpdateOrchestrator));

        [Function(nameof(BuildingsUpdateOrchestrator))]
        public static async Task BuildingsUpdateOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            try
            {
                await context.CallActivityAsync(nameof(BuildingsUpdateActivity), null, RetryOptions);

                Logging.GetLogger(context).Debug("Finished buildings update.");
            }
            catch (Exception ex)
            {
                Logging.GetLogger(context).Error(ex, "Buildings update failed with exception.");
                throw;
            }
        }

        [Function(nameof(BuildingsUpdateActivity))]
        public static async Task BuildingsUpdateActivity([ActivityTrigger] TaskOrchestrationContext context)
        {   
            var buildings = await FetchBuildingsFromDenodo();
            foreach (var batch in buildings.Partition(50))
            {
                await AddOrUpdateBuildingRecords(batch);
            }
        }

        // Fetch all buildings from the Denodo view maintianed by UITS Facilities
        public static async Task<IEnumerable<DenodoBuilding>> FetchBuildingsFromDenodo()
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
        public static async Task AddOrUpdateBuildingRecords(IEnumerable<DenodoBuilding> buildings)
        {
            var buildingCodes = buildings
                .Select(b => b.BuildingCode)
                .ToList();
            
            await Utils.DatabaseCommand(nameof(AddOrUpdateBuildingRecords), "Add/update buildings", async db =>{
                var existingRecords = await db.Buildings
                    .Where(b => buildingCodes.Contains(b.Code))
                    .ToListAsync();
                foreach(var building in buildings)
                {
                    var existing = existingRecords.SingleOrDefault(e => e.Code == building.BuildingCode);
                    if(existing == null)
                    {
                        existing = new Building();
                        await db.Buildings.AddAsync(existing);
                    }

                    building.MapToBuilding(existing);
                }
                await db.SaveChangesAsync();
            });             
        }

        private static HttpClient HttpClient = new HttpClient();

        private static TaskOptions RetryOptions = new TaskOptions(
            new TaskRetryOptions(
                new RetryPolicy(3, TimeSpan.FromSeconds(5))
            )
        );
        
    }
}
