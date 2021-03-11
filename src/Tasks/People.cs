using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using Models;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace Tasks
{
    public static class People
    {
         // Runs at the top of the hour (00:00 AM, 01:00 AM, 02:00 AM, ...)
        [FunctionName(nameof(ScheduledPeopleUpdate))]
        public static async Task ScheduledPeopleUpdate([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, 
            [DurableClient] IDurableOrchestrationClient starter)
        {
            string instanceId = await starter.StartNewAsync(nameof(PeopleUpdateOrchestrator), null);
            Logging.GetLogger(instanceId, nameof(ScheduledPeopleUpdate), myTimer)
                .Information("Started scheduled people update.");
        }

        [FunctionName(nameof(PeopleUpdateOrchestrator))]
        public static async Task PeopleUpdateOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                // Get a UAA JWT to authenticate calls to the IMS Profile API
                var uaaJwt = await context.CallActivityWithRetryAsync<string>(
                    nameof(FetchUAAToken), RetryOptions, null);

                // Aggregate all the HR records of various different types
                var hrRecords = new List<ProfileEmployee>();
                foreach(var type in new[]{"employee", "affiliate", "foundation"})
                {
                    var employees = await context.CallSubOrchestratorAsync<IEnumerable<ProfileEmployee>>(
                        nameof(FetchPeopleFromProfileApi), (uaaJwt, type));
                    hrRecords.AddRange(employees);
                }

                // Update hr_people/people/departments database records.
                await context.CallSubOrchestratorAsync(
                    nameof(UpdateDatabaseRecords), hrRecords);
            }
            catch (Exception ex)
            {
                Logging.GetLogger(context).Error(ex, "People update orchestration failed with exception.");
                throw;
            }
        }       

        // Fetch a UAA Jwt using the client credentials (username/password) grant type.
        [FunctionName(nameof(FetchUAAToken))]
        public static async Task<string> FetchUAAToken([ActivityTrigger] IDurableActivityContext context)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string,string>{
                {"grant_type", "client_credentials"},
                {"client_id", Utils.Env("UaaClientCredentialId", required: true)},
                {"client_secret", Utils.Env("UaaClientCredentialPassword", required: true)},
            });
            var url = Utils.Env("UaaClientCredentialUrl", required: true);
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            var resp = await HttpClient.SendAsync(req);
            var body = await Utils.DeserializeResponse<UaaJwtResponse>(context, resp, "fetch JWT from UAA");
            return body.access_token;
        }


        // Aggregate all HR records of a certain type from the IMS Profile API
        [FunctionName(nameof(FetchPeopleFromProfileApi))]
        public static async Task<IEnumerable<ProfileEmployee>> FetchPeopleFromProfileApi([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var args = context.GetInput<(string uaaJwt, string type)>();
            var page = 0;
            var hasMore = true;
            var hrRecords = new List<ProfileEmployee>();

            do 
            {
                var resp = await context.CallActivityWithRetryAsync<ProfileResponse>(                    
                    nameof(FetchPeoplePageFromProfileApi), RetryOptions, (args.uaaJwt, args.type, page++));
                hrRecords.AddRange(resp.affiliates == null ? new List<ProfileEmployee>() : resp.affiliates);
                hrRecords.AddRange(resp.employees == null ? new List<ProfileEmployee>() : resp.employees);
                hrRecords.AddRange(resp.foundations == null ? new List<ProfileEmployee>() : resp.foundations);
                page += 1;
                hasMore = (resp.page.CurrentPage == resp.page.LastPage);
            } while (hasMore);

            return hrRecords;
        }

        // Fetch a page of HR records of a certain type from the IMS Profile API
        [FunctionName(nameof(FetchPeoplePageFromProfileApi))]
        public static async Task<ProfileResponse> FetchPeoplePageFromProfileApi([ActivityTrigger] IDurableActivityContext context)
        {
            var args = context.GetInput<(string uaaJwt, string type, int page)>();
            var url = Utils.Env("ImsProfileApiUrl", required: true);
            var req = new HttpRequestMessage(HttpMethod.Get, $"{url}?affiliationType={args.type}&page={args.page}&pageSize=7500");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", args.uaaJwt);
            var resp = await HttpClient.SendAsync(req);            
            return await Utils.DeserializeResponse<ProfileResponse>(context, resp, "fetch page from IMS Profile API");
        }

        // Aggregate all HR records of a certain type from the IMS Profile API
        [FunctionName(nameof(UpdateDatabaseRecords))]
        public static async Task UpdateDatabaseRecords([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var hrRecords = context.GetInput<IEnumerable<ProfileEmployee>>();
            // Refresh the HrPeople table with the Profile API data
            await context.CallActivityWithRetryAsync(
                    nameof(RefreshHrPeople), RetryOptions, hrRecords);
            // Add/update Departments from new HR data
            await context.CallActivityWithRetryAsync(
                    nameof(UpdateDepartmentRecords), RetryOptions, null);
            // Update People name/position/contact info from new HR data
            await context.CallActivityWithRetryAsync(
                    nameof(UpdatePeopleRecords), RetryOptions, null);
        }

        // Reset the hr_people table with the lastest HR data from the Profile API 
        [FunctionName(nameof(RefreshHrPeople))]
        public static async Task RefreshHrPeople([ActivityTrigger] IDurableActivityContext context)
        {
            try
            {
                var emps = context.GetInput<IEnumerable<ProfileEmployee>>();
                var connStr = Utils.Env("DatabaseConnectionString", required: true);
                using (var db = new Npgsql.NpgsqlConnection(connStr))
                {
                    await db.OpenAsync();
                    // Clear all records in hr_people
                    var cmd = db.CreateCommand();
                    cmd.CommandText = "TRUNCATE hr_people;";
                    await cmd.ExecuteNonQueryAsync();
                    // Quickly import all HR records from the IMS Profile API
                    using (var writer = db.BeginTextImport("COPY hr_people (name, name_first, name_last, netid, position, campus, campus_phone, campus_email, hr_department, hr_department_desc) FROM STDIN"))
                    {
                        foreach(var emp in emps)
                        {
                            var p = new HrPerson();
                            emp.MapToHrPerson(p);
                            await writer.WriteLineAsync($"{p.Name}\t{p.NameFirst}\t{p.NameLast}\t{p.Netid}\t{p.Position}\t{p.Campus}\t{p.CampusPhone}\t{p.CampusEmail}\t{p.HrDepartment}\t{p.HrDepartmentDescription}");
                        }
                        await writer.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.GetLogger(context).Error(ex, "Failed to bulk-insert HR records to hr_people");
                throw;
            }
        }

        // Add new Departmnet records to the IT People database.
        // If a departments with the same name already exists then update its description..
        [FunctionName(nameof(UpdateDepartmentRecords))]
        public static Task UpdateDepartmentRecords([ActivityTrigger] IDurableActivityContext context) 
            => Utils.DatabaseCommand(context, "Upsert department records from new HR data", db =>
                db.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO departments (name, description)
                    SELECT DISTINCT hr_department, hr_department_desc
                    FROM hr_people
                    WHERE hr_department IS NOT NULL
                    ON CONFLICT (name) DO UPDATE SET
                        description = EXCLUDED.description;"));

        /// Update name, location, position of people in directory using new HR data
        /// This should be one *after* departments are updated.
        [FunctionName(nameof(UpdatePeopleRecords))]
        public static Task UpdatePeopleRecords([ActivityTrigger] IDurableActivityContext context)
            => Utils.DatabaseCommand(context, "Upsert department records from new HR data", db =>
                db.Database.ExecuteSqlRawAsync(@"
                    UPDATE people p
                    SET p.name = hr.name,
                        p.name_first = hr.name_first,
                        p.name_last = hr.name_last,
                        p.position = hr.position,
                        p.location = hr.location,
                        p.campus = hr.campus,
                        p.campus_phone = hr.campus_phone,
                        p.campus_email = hr.campus_email,
                        p.department_id = (SELECT id FROM departments WHERE name=hr.hr_department)
                    FROM hr_people hr
                    WHERE p.netid = hr.netid"));

        private static HttpClient HttpClient = new HttpClient();

        private static RetryOptions RetryOptions = new RetryOptions(
            firstRetryInterval: TimeSpan.FromSeconds(5),
            maxNumberOfAttempts: 3);
    }
}
