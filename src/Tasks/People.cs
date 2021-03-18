using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using Models;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;

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
                var hrRecords = await context.CallActivityWithRetryAsync<IEnumerable<ProfileEmployee>>(
                    nameof(FetchPeopleFromProfileApi), RetryOptions, uaaJwt);

                // Refresh the HrPeople table with the Profile API data
                await context.CallActivityWithRetryAsync(
                        nameof(BulkInsertHrPeople), RetryOptions, hrRecords);
                // Add/update Departments from new HR data
                await context.CallActivityWithRetryAsync(
                        nameof(UpdateDepartmentRecords), RetryOptions, null);
                // Update People name/position/contact info from new HR data
                await context.CallActivityWithRetryAsync(
                        nameof(UpdatePeopleRecords), RetryOptions, null);
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
                {"client_secret", Utils.Env("UaaClientCredentialSecret", required: true)},
            });
            var url = Utils.Env("UaaClientCredentialUrl", required: true);
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            var resp = await Utils.HttpClient.SendAsync(req);
            var body = await Utils.DeserializeResponse<UaaJwtResponse>(nameof(FetchUAAToken), resp, "fetch JWT from UAA");
            return body.access_token;
        }


        // Aggregate all HR records of a certain type from the IMS Profile API
        [FunctionName(nameof(FetchPeopleFromProfileApi))]
        public static async Task<IEnumerable<ProfileEmployee>> FetchPeopleFromProfileApi([ActivityTrigger] IDurableActivityContext context)
        {
            var jwt = context.GetInput<string>();
            var hrRecords = new List<ProfileEmployee>();

            foreach(var type in new[]{"employee", "affiliate", "foundation"})
            {
                var page = 0;
                var hasMore = true;
                do 
                {
                    var body = await FetchProfileApiPage(context, jwt, type, page);
                    hrRecords.AddRange(body.affiliates == null ? new List<ProfileEmployee>() : body.affiliates);
                    hrRecords.AddRange(body.employees == null ? new List<ProfileEmployee>() : body.employees);
                    hrRecords.AddRange(body.foundations == null ? new List<ProfileEmployee>() : body.foundations);
                    page += 1;
                    hasMore = (body.page.CurrentPage != body.page.LastPage);
                } while (hasMore);
            }

            return Clean(hrRecords);
        }

        private static async Task<ProfileResponse> FetchProfileApiPage(IDurableActivityContext context, string jwt, string type, int page)
        {
            Console.WriteLine($"Fetching {type} page {page}");
            var url = Utils.Env("ImsProfileApiUrl", required: true);
            var authHeader = new AuthenticationHeaderValue("Bearer", jwt);
            var req = new HttpRequestMessage(HttpMethod.Get, $"{url}?affiliationType={type}&page={page}&pageSize=7500");
            req.Headers.Authorization = authHeader;
            var resp = await Utils.HttpClient.SendAsync(req);
            return await Utils.DeserializeResponse<ProfileResponse>(nameof(FetchProfileApiPage), resp, "fetch page from IMS Profile API");
        }

        private static IEnumerable<ProfileEmployee> Clean(List<ProfileEmployee> hrRecords) 
            => hrRecords.GroupBy(r => r.Username)
                .Select(grp => new ProfileEmployee()
                {
                    Username = grp.Key,
                    FirstName = grp.First().FirstName,
                    LastName = grp.First().LastName,
                    Email = grp.First().Email,
                    Jobs = grp.SelectMany(grpx => grpx.Jobs),
                    Contacts = grp.SelectMany(grpx => grpx.Contacts)
                })
                .Where(r =>
                    !string.IsNullOrWhiteSpace(r.Email)
                    && r.Jobs.Any(j => j.JobStatus == "P" && !string.IsNullOrWhiteSpace(j.JobDepartmentId)));

        // Reset the hr_people table with the lastest HR data from the Profile API 
        [FunctionName(nameof(BulkInsertHrPeople))]
        public static async Task BulkInsertHrPeople([ActivityTrigger] IDurableActivityContext context)
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
                            await writer.WriteLineAsync($"{p.Name}\t{p.NameFirst}\t{p.NameLast}\t{p.Netid.ToLowerInvariant()}\t{p.Position}\t{p.Campus}\t{p.CampusPhone}\t{p.CampusEmail}\t{p.HrDepartment}\t{p.HrDepartmentDescription}");
                        }
                        await writer.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "Failed to bulk-insert HR records to hr_people";
                Logging.GetLogger(nameof(BulkInsertHrPeople)).Error(ex, msg);
                throw new Exception(msg, ex);
            }
        }

        // Add new Departmnet records to the IT People database.
        // If a departments with the same name already exists then update its description..
        [FunctionName(nameof(UpdateDepartmentRecords))]
        public static Task UpdateDepartmentRecords([ActivityTrigger] IDurableActivityContext context) 
            => Utils.DatabaseCommand(nameof(UpdateDepartmentRecords), "Upsert department records from new HR data", db =>
                db.Database.ExecuteSqlRawAsync(@"
                    -- 1. Add any new hr departments
                    INSERT INTO departments (name, description)
                    SELECT DISTINCT hr_department, hr_department_desc
                    FROM hr_people
                    WHERE hr_department IS NOT NULL
                    ON CONFLICT (name)
                    DO NOTHING;
                    -- 2. Update department descriptions 
                    UPDATE departments d
                    SET description = hr_department_desc
                    FROM hr_people hr
                    WHERE d.name = hr.hr_department"));

        /// Update name, location, position of people in directory using new HR data
        /// This should be one *after* departments are updated.
        [FunctionName(nameof(UpdatePeopleRecords))]
        public static Task UpdatePeopleRecords([ActivityTrigger] IDurableActivityContext context)
            => Utils.DatabaseCommand(nameof(UpdatePeopleRecords), "Upsert department records from new HR data", db =>
                db.Database.ExecuteSqlRawAsync(@"
                    UPDATE people p
                    SET name = hr.name,
                        name_first = hr.name_first,
                        name_last = hr.name_last,
                        position = hr.position,
                        campus = hr.campus,
                        campus_phone = hr.campus_phone,
                        campus_email = hr.campus_email,
                        department_id = (SELECT id FROM departments WHERE name=hr.hr_department)
                    FROM hr_people hr
                    WHERE p.netid = hr.netid"));

        private static RetryOptions RetryOptions = new RetryOptions(
            firstRetryInterval: TimeSpan.FromSeconds(5),
            maxNumberOfAttempts: 3);
    }
}
