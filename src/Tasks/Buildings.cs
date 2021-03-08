using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using Models;
using Database;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Newtonsoft.Json;

namespace Tasks
{
    public static class Buildings
    {
        // Runs at the bottom of the hour (00:30 AM, 01:30 AM, 02:30 AM, ...)
        [FunctionName(nameof(ScheduledBuildingsUpdate))]
        public static async Task ScheduledBuildingsUpdate([TimerTrigger("0 30 * * * *")]TimerInfo myTimer, 
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync(nameof(BuildingsUpdateOrchestrator), null);
            log.LogInformation($"Started buildings update orchestration with ID = '{instanceId}'.");
        }

        [FunctionName(nameof(BuildingsUpdateOrchestrator))]
        public static async Task BuildingsUpdateOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var buildings = await context.CallActivityWithRetryAsync<IEnumerable<DenodoBuilding>>(
                nameof(FetchBuildingsFromDenodo), RetryOptions, null);
            foreach (var building in buildings)
            {
                await context.CallActivityWithRetryAsync(
                    nameof(AddOrUpdateBuildingRecords), RetryOptions, building);
            }
        }

        // Fetch all buildings from the Denodo view maintianed by UITS Facilities
        [FunctionName(nameof(FetchBuildingsFromDenodo))]
        public static async Task<IEnumerable<DenodoBuilding>> FetchBuildingsFromDenodo([ActivityTrigger] ILogger log)
        {
            var req = CreateDenodoBuildingsRequest();
            var resp = await HttpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsAsync<DenodoResponse<DenodoBuilding>>();
            return body.Elements;
        }

        // Add new Builing records to the IT People database.
        // If a building with the same code already exists then update its properties.
        [FunctionName(nameof(AddOrUpdateBuildingRecords))]
        public static async Task AddOrUpdateBuildingRecords([ActivityTrigger] DenodoBuilding bld, ILogger log)
        {
            var connStr = Utils.Env("DatabaseConnectionString", required: true);
            using (var db = PeopleContext.Create(connStr))
            {                
                var record = await db.Buildings.SingleOrDefaultAsync(b => b.Code == bld.BuildingCode);
                if (record == null)
                {
                    record = new Building();
                    db.Buildings.Add(record);
                }
                bld.MapToBuilding(record);
                await db.SaveChangesAsync();
            }
        }

        private static HttpClient HttpClient = new HttpClient();

        private static RetryOptions RetryOptions = new RetryOptions(
            firstRetryInterval: TimeSpan.FromSeconds(5),
            maxNumberOfAttempts: 3);

        private static HttpRequestMessage CreateDenodoBuildingsRequest()
        {
            var denodoUrl = Utils.Env("DenodoBuildingsUrl", required: true);
            var denodoUser = Utils.Env("DenodoBuildingsUsername", required: true);
            var denodoPwd = Utils.Env("DenodoBuildingsPassword", required: true);
            var basicAuth = Convert.ToBase64String(
                System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(
                    $"{denodoUser}:{denodoPwd}"
                )
            );
            var req = new HttpRequestMessage(HttpMethod.Get, denodoUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            return req;
        }
    }

    public class DenodoResponse<T>
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("elements")] public IEnumerable<T> Elements { get; set; }
    }

    public class DenodoBuilding
    {
        [JsonProperty("building_name")] public string Name { get; set; }
        [JsonProperty("building_code")] public string BuildingCode { get; set; }
        [JsonProperty("site_code")] public string SiteCode { get; set; }
        [JsonProperty("building_long_description")] public string Description { get; set; }
        [JsonProperty("street")] public string Street { get; set; }
        [JsonProperty("city")] public string City { get; set; }
        [JsonProperty("state")] public string State { get; set; }
        [JsonProperty("zip")] public string Zip { get; set; }

        public void MapToBuilding(Building record)
        {
            record.Name = Description;
            record.Code = BuildingCode;
            record.Address = Street;
            record.City = City;
            record.State = State;
            record.PostCode = Zip;
            record.Country = "";
        }
    }
}
