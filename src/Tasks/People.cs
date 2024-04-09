using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;
using System.Collections.Generic;
using Models;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;

namespace Tasks
{
    public static class People
    {
        // Runs at the top of the hour (00:00 AM, 01:00 AM, 02:00 AM, ...)
        [Function(nameof(ScheduledPeopleUpdate))]
        public static Task ScheduledPeopleUpdate([TimerTrigger("0 0 * * * *")] TimerInfo timer,
            [DurableClient] DurableTaskClient starter) 
            => Utils.StartOrchestratorAsSingleton(timer, starter, nameof(PeopleUpdateOrchestrator));


        [Function(nameof(PeopleUpdateOrchestrator))]
        public static async Task PeopleUpdateOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            try
            {
                // Get a UAA JWT to authenticate calls to the IMS Profile API
                var uaaJwt = await context.CallActivityAsync<string>(nameof(FetchUAAToken), null, RetryOptions);

                // Add/update HR records of various different types
                await context.CallSubOrchestratorAsync(nameof(UpdateHrPeopleRecords), uaaJwt);
                        
                // Add/update Departments from new HR data
                await context.CallActivityAsync(nameof(UpdateDepartmentRecords), null, RetryOptions);
                // Update People name/position/contact info from new HR data
                await context.CallActivityAsync(nameof(UpdatePeopleRecords), null, RetryOptions);

                Logging.GetLogger(context).Debug("Finished people update.");
            }
            catch (Exception ex)
            {
                Logging.GetLogger(context).Error(ex, "People update failed with exception.");
                throw;
            }
        }       

        // Fetch a UAA Jwt using the client credentials (username/password) grant type.
        [Function(nameof(FetchUAAToken))]
        public static async Task<string> FetchUAAToken([ActivityTrigger] TaskOrchestrationContext context)
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
        [Function(nameof(UpdateHrPeopleRecords))]
        public static async Task UpdateHrPeopleRecords([OrchestrationTrigger] TaskOrchestrationContext context)
        {            
            var jwt = context.GetInput<string>();

            // Mark all HrPeople records for deletion
            await context.CallActivityAsync(nameof(MarkHrPeopleForDeletion), null, RetryOptions);

            foreach(var type in new[]{"employee", "affiliate", "foundation"})
            {
                var page = 0;
                var hasMore = true;
                do 
                {
                    // hasMore = await context.CallActivityWithRetryAsync<bool>(
                    //     nameof(UpdateHrPeoplePage), RetryOptions, (jwt, type, page));
                    hasMore = await context.CallActivityAsync<bool>(nameof(UpdateHrPeoplePage), (jwt, type, page), RetryOptions);
                    page += 1;
                } while (hasMore);
            }
            // Delete HRpeople still marked for deletion
            await context.CallActivityAsync(nameof(DeleteMarkedHrPeople), null, RetryOptions);
        }
        
        [Function(nameof(MarkHrPeopleForDeletion))]
        public static async Task MarkHrPeopleForDeletion([ActivityTrigger]TaskOrchestrationContext context)
        {
            await Utils.DatabaseCommand(nameof(UpdateHrPeopleRecords), "Mark all HrPeople for deletion", async db => {
                await db.Database.ExecuteSqlRawAsync(@"
                    UPDATE hr_people
                    SET marked_for_delete = true");
            });
        }

        [Function(nameof(DeleteMarkedHrPeople))]
        public static async Task DeleteMarkedHrPeople([ActivityTrigger]TaskOrchestrationContext context)
        {           
            await Utils.DatabaseCommand(nameof(UpdateHrPeopleRecords), "Delete all HrPeople with MarkedForDelete == true", async db => {
                var hrPeopleToDelete = db.HrPeople.Where(h => h.MarkedForDelete);
                db.HrPeople.RemoveRange(hrPeopleToDelete);
                await db.SaveChangesAsync();
            });
        }

        [Function(nameof(UpdateHrPeoplePage))]
        public static async Task<bool> UpdateHrPeoplePage([ActivityTrigger]TaskOrchestrationContext context)
        {
            var (jwt, type, page) = context.GetInput<(string, string, int)>();
            var body = await FetchProfileApiPage(jwt, type, page);
            var hrRecords = new List<ProfileEmployee>();
            hrRecords.AddRange(body.affiliates == null ? new List<ProfileEmployee>() : body.affiliates);
            hrRecords.AddRange(body.employees == null ? new List<ProfileEmployee>() : body.employees);
            hrRecords.AddRange(body.foundations == null ? new List<ProfileEmployee>() : body.foundations);

            foreach (var batch in Clean(hrRecords).Partition(50))
            {
                await UpsertManyHrPersonRecords(batch);
            }

            var hasMore = (body.page.CurrentPage != body.page.LastPage);

            return hasMore;
        }


        public static async Task<ProfileResponse> FetchProfileApiPage(string jwt, string type, int page)
        {
            Console.WriteLine($"Fetching {type} page {page}");
            var url = Utils.Env("ImsProfileApiUrl", required: true);
            var authHeader = new AuthenticationHeaderValue("Bearer", jwt);
            var req = new HttpRequestMessage(HttpMethod.Get, $"{url}?affiliationType={type}&page={page}&pageSize=5000");
            req.Headers.Authorization = authHeader;
            var resp = await Utils.HttpClient.SendAsync(req);
            return await Utils.DeserializeResponse<ProfileResponse>(nameof(FetchProfileApiPage), resp, "fetch page from IMS Profile API");
        }
        
        public static async Task UpsertManyHrPersonRecords(IEnumerable<ProfileEmployee> profiles)
        {
            var profileNetids = profiles
                .Select(p => p.Username.ToLower().Trim())
                .ToList();
            
            await Utils.DatabaseCommand(nameof(UpsertManyHrPersonRecords), "Upsert Hr Person records", async db => {
                var existingRecords = await db.HrPeople
                    .Where(h => profileNetids.Contains(h.Netid.ToLower().Trim()))
                    .ToListAsync();

                foreach(var profile in profiles)
                {
                    var existing = existingRecords.SingleOrDefault(e => e.Netid.ToLower().Trim() == profile.Username.ToLower().Trim());
                    if(existing == null)
                    {
                        existing = new HrPerson();
                        await db.HrPeople.AddAsync(existing);
                    }
                    profile.MapToHrPerson(existing);
                    existing.MarkedForDelete = false;
                }

                await db.SaveChangesAsync();
            });
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

        // Add new Departmnet records to the IT People database.
        // If a departments with the same name already exists then update its description..
        [Function(nameof(UpdateDepartmentRecords))]
        public static Task UpdateDepartmentRecords([ActivityTrigger] TaskOrchestrationContext context) 
            => Utils.DatabaseCommand(nameof(UpdateDepartmentRecords), "Upsert department records from new HR data", db =>
                db.Database.ExecuteSqlRawAsync(@"
                    -- 1. Add any new hr departments
                    INSERT INTO departments (name, description)
                    SELECT DISTINCT hr_department, hr_department_desc
                    FROM hr_people
                    WHERE hr_department IS NOT NULL
                    AND hr_department_desc IS NOT NULL
                    ON CONFLICT (name)
                    DO NOTHING;
                    -- 2. Update department descriptions 
                    UPDATE departments d
                    SET description = hr_department_desc
                    FROM hr_people hr
                    WHERE d.name = hr.hr_department
                    AND hr_department_desc IS NOT NULL"));

        /// Update name, location, position of people in directory using new HR data
        /// This should be one *after* departments are updated.
        [Function(nameof(UpdatePeopleRecords))]
        public static Task UpdatePeopleRecords([ActivityTrigger] TaskOrchestrationContext context)
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

        private static TaskOptions RetryOptions = new TaskOptions(
            new TaskRetryOptions(
                new RetryPolicy(3, TimeSpan.FromSeconds(5))
            )
        );
    }
}
