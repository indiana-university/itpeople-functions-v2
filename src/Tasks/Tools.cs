using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Models;
using System.Linq;
using Novell.Directory.Ldap.Controls;
using Novell.Directory.Ldap;

namespace Tasks
{
    public class Tools
    {
        // Runs at 20 minutes past the hour (00:20 AM, 01:20 AM, 02:20 AM, ...)
        [FunctionName(nameof(ScheduledToolsUpdate))]
        public static async Task ScheduledToolsUpdate([TimerTrigger("0 20 * * * *")]TimerInfo myTimer, 
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync(nameof(ToolsUpdateOrchestrator), null);
            log.LogInformation($"Started tools update orchestration with ID = '{instanceId}'.");
        }

        [FunctionName(nameof(ToolsUpdateOrchestrator))]
        public static async Task ToolsUpdateOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // Fetch all tools.
            var tools = await context.CallActivityWithRetryAsync<string>(
                nameof(FetchTools), RetryOptions, null);
            
            // Compare current tool grants with IT People grants and add/remove grants as necessary.
            var updates = new List<ToolUpdate>();
            foreach(var tool in tools)
            {
                await context.CallSubOrchestratorAsync(
                    nameof(SynchronizeToolGroupMembership), tool);
            }
        }

        [FunctionName(nameof(FetchTools))]
        public static async Task<List<Tool>> FetchTools([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            var connStr = Utils.Env("DatabaseConnectionString", required: true);
            using (var db = PeopleContext.Create(connStr))
            {
                return await db.Tools.ToListAsync();
            }
        }

        // Aggregate all HR records of a certain type from the IMS Profile API
        [FunctionName(nameof(SynchronizeToolGroupMembership))]
        public static async Task SynchronizeToolGroupMembership(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log, Tool tool)
        {
            // get grantee netids from IT People DB
            var grantees = await context.CallActivityWithRetryAsync<IEnumerable<string>>(
                nameof(GetToolGrantees), RetryOptions, tool);
            // get current group members
            var members = await context.CallActivityWithRetryAsync<IEnumerable<string>>(
                nameof(GetGroupMembers), RetryOptions, tool);
            // add to the group any grantee who is not a member.
            foreach (var netid in grantees.Except(members))
            {
                await context.CallActivityWithRetryAsync(
                    nameof(AddGroupMember), RetryOptions, (tool, netid));
            }
            // remove from the group any member who is not a grantee.
            foreach (var netid in members.Except(grantees))
            {
                await context.CallActivityWithRetryAsync(
                    nameof(RemoveGroupMember), RetryOptions, (tool, netid));
            }
        }

        [FunctionName(nameof(GetToolGrantees))]
        public static async Task<List<string>> GetToolGrantees([ActivityTrigger] IDurableActivityContext context, 
            ILogger log, Tool tool)
        {
            var connStr = Utils.Env("DatabaseConnectionString", required: true);
            using (var db = PeopleContext.Create(connStr))
            {                
                return await db.Tools
                    .Where(t => t.Id == tool.Id)
                    .SelectMany(t => t.MemberTools)
                    .Select(mt => mt.UnitMember.Person.Netid)
                    .Distinct()
                    .ToListAsync();
            }
        }

        private const bool AddMember = true;
        private const bool DeleteMember = false;
        private const string LdapSortKey = "cn";
        private const string LdapAttributeName = "sAMAccountName";
        private const string LdapSearchBase = "ou=Accounts,dc=ads,dc=iu,dc=edu";
        private const int LdapPageSize = 500;

        [FunctionName(nameof(GetGroupMembers))]
        public static List<string> GetGroupMembers([ActivityTrigger] IDurableActivityContext context, 
            ILogger log, Tool tool)
        {
            var members = new List<string>();
            using (var ldap = GetLdapConnection())
            {
                var page = 0;
                var groupHasMoreMembers = true;
                do 
                {
                    // set up a pager and sorter for the group member search results 
                    var controls = new LdapControl[]{
                        new LdapVirtualListControl (page*LdapPageSize+1, 0, LdapPageSize-1, 0), // pager
                        new LdapSortControl(new LdapSortKey(LdapSortKey), true) // sorter
                    };
                    var constraints = new LdapSearchConstraints();
                    constraints.setControls(controls);
                    // query the LDAP group membrship
                    var search = ldap.Search(LdapSearchBase, LdapConnection.SCOPE_SUB, $"(memberOf={tool.ADPath}", new[]{LdapAttributeName}, false, constraints);
                    // Pull member netids from the results list. But LDAP is weird: it sends
                    // a linked list of results that wraps around on itself. We know we've
                    // reached the end of the list when we encounter a netid we've already seen.
                    while (search.hasMore())
                    {
                        var netid = search.next().getAttribute(LdapAttributeName).StringValue;
                        if (members.Contains(netid))
                            groupHasMoreMembers = false;
                        else 
                            members.Add(netid);
                    } 
                    // advance the page
                    page = page + 1;
                } while (groupHasMoreMembers);
            }
            return members;
        }

        [FunctionName(nameof(AddGroupMember))]
        public static void AddGroupMember([ActivityTrigger] IDurableActivityContext context, 
            ILogger log, (Tool tool, string netid) args)
            => ModifyGroupMembership(args.netid, args.tool.ADPath, LdapModification.ADD);

        [FunctionName(nameof(RemoveGroupMember))]
        public static void RemoveGroupMember([ActivityTrigger] IDurableActivityContext context, 
            ILogger log, (Tool tool, string netid) args)
            => ModifyGroupMembership(args.netid, args.tool.ADPath, LdapModification.DELETE);

        private static void ModifyGroupMembership(string netid, string adPath, int action)
        {
            using (var ldap = GetLdapConnection())
            {
                var memberAttribute = $"cn={netid},{LdapSearchBase}";
                var ldapAttribute = new LdapAttribute("member", memberAttribute);
                var groupMod = new LdapModification(action, ldapAttribute);
                ldap.Modify(adPath, groupMod);
            }
        }

        private static RetryOptions RetryOptions = new RetryOptions(
            firstRetryInterval: TimeSpan.FromSeconds(5),
            maxNumberOfAttempts: 3);

        private static LdapConnection GetLdapConnection()
        {
            var adsUser = $"ads\\{Utils.Env("AdsUser")}";
            var adsPassword = Utils.Env("AdsPassword");
            var ldap = new LdapConnection() {SecureSocketLayer = true};
            ldap.Connect("ads.iu.edu", 636);
            ldap.Bind(adsUser, adsPassword);    
            return ldap;     
        }
    }
}
