using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Models;
using System.Linq;
using Novell.Directory.Ldap.Controls;
using Novell.Directory.Ldap;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;

namespace Tasks
{
    public class Tools
    {
        // Runs every 5 minutes)
        [Function(nameof(ScheduledToolsUpdate))]
        public static Task ScheduledToolsUpdate([TimerTrigger("0 */5 * * * *")]TimerInfo timer, 
            [DurableClient] DurableTaskClient starter)
            => Utils.StartOrchestratorAsSingleton(timer, starter, nameof(ToolsUpdateOrchestrator));

        [Function(nameof(ToolsUpdateOrchestrator))]
        public static async Task ToolsUpdateOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            try
            {
                Logging.GetLogger(context).Information("Starting tools update.");
                // Fetch all tools.
                var tools = await context.CallActivityAsync<IEnumerable<Tool>>(nameof(FetchAllTools), null, RetryOptions);
                
                // Compare current tool grants with IT People grants and add/remove grants as necessary.
                var toolTasks = tools.Select(t => 
                    context.CallActivityAsync(nameof(SynchronizeToolGroupMembership), t, RetryOptions));
                await Task.WhenAll(toolTasks);

                Logging.GetLogger(context).Debug("Finished tools update.");
            } 
            catch (Exception ex)
            {
                Logging.GetLogger(context).Error(ex, "Tools update failed with exception.");
                throw;
            }
        }

        [Function(nameof(FetchAllTools))]
        public static Task<List<Tool>> FetchAllTools([ActivityTrigger] TaskOrchestrationContext context) 
            => Utils.DatabaseQuery(nameof(FetchAllTools), "fetch all tools", db => db.Tools.ToListAsync());

        // Aggregate all HR records of a certain type from the IMS Profile API
        [Function(nameof(SynchronizeToolGroupMembership))]
        public static async Task SynchronizeToolGroupMembership([ActivityTrigger] Tool tool, TaskOrchestrationContext context)
        {
            // get grantee netids from IT People DB
            var grantees = await GetToolGrantees(context, tool);
            // get current group members
            var members = GetToolGroupMembers(context, tool);
            // add to the group any grantee who is not a member.
            var addTasks = grantees.Except(members).Select(m => AddToolGroupMember(context, tool, m));
            // remove from the group any member who is not a grantee
            var removeTasks = members.Except(grantees).Select(m => RemoveToolGroupMember(context, tool, m));
            await Task.WhenAll(addTasks.Concat(removeTasks));
        }

        public static Task<List<string>> GetToolGrantees(TaskOrchestrationContext context, Tool tool) 
            => Utils.DatabaseQuery<List<string>>(nameof(GetToolGrantees), $"fetch grantees for tool '{tool.Name}'", db =>
                db.Tools
                    .Where(t => t.Id == tool.Id)
                    .SelectMany(t => t.MemberTools)
                    .Where(mt => mt.UnitMember.Unit.Active)
                    .Select(mt => mt.UnitMember.Person.Netid)
                    .Distinct()
                    .OrderBy(m=>m)
                    .ToListAsync());

        private const bool AddMember = true;
        private const bool DeleteMember = false;
        private const string LdapSortKey = "cn";
        private const string LdapAttributeName = "sAMAccountName";
        private const string LdapSearchBase = "ou=Accounts,dc=ads,dc=iu,dc=edu";
        private const int LdapPageSize = 500;

        public static List<string> GetToolGroupMembers(TaskOrchestrationContext context, Tool tool)
        {
            var members = new List<string>();
            try
            {
                using (var ldap = GetLdapConnection())
                {
                    var page = 0;
                    var groupHasMoreMembers = false;
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
                        var search = ldap.Search(LdapSearchBase, LdapConnection.SCOPE_SUB, $"(memberOf={tool.ADPath})", new[]{LdapAttributeName}, false, constraints);
                        // Pull member netids from the results list. But LDAP is weird: it sends
                        // a linked list of results that wraps around on itself. We know we've
                        // reached the end of the list when we encounter a netid we've already seen.
                        while (search.hasMore())
                        {
                            var netid = search.next().getAttribute(LdapAttributeName).StringValue;
                            if (members.Contains(netid))
                            {
                                groupHasMoreMembers = false;
                                break;
                            }
                            else 
                            {                                
                                members.Add(netid);
                                groupHasMoreMembers = true;
                            }
                        } 
                        // advance the page
                        page = page + 1;
                    } while (groupHasMoreMembers);
                }
                return members.OrderBy(m => m).ToList();
            }
            catch (Exception ex)
            {
                string msg = $"Failed to fetch members of tool {tool.Name} group at path {tool.ADPath}";
                Logging.GetLogger(nameof(GetToolGroupMembers), tool).Error(ex, msg);
                throw new Exception(msg, ex);
            }
        }

        public static Task AddToolGroupMember(TaskOrchestrationContext context, Tool tool, string netid)
        {
            Logging.GetLogger(nameof(AddToolGroupMember),new {tool=tool, netid=netid}).Information($"Add {netid} to group {tool.Name}.");
            return Task.Run(()=>ModifyToolGroupMembership(context, netid, tool.Name, tool.ADPath, LdapModification.ADD));
        }

        public static Task RemoveToolGroupMember(TaskOrchestrationContext context, Tool tool, string netid)
        {
            Logging.GetLogger(nameof(RemoveToolGroupMember),new {tool=tool, netid=netid}).Information($"Remove {netid} from group {tool.Name}.");
            return Task.Run(()=>ModifyToolGroupMembership(context, netid, tool.Name, tool.ADPath, LdapModification.DELETE));
        }

        private static void ModifyToolGroupMembership(TaskOrchestrationContext context, string netid, string name, string adPath, int action)
        {
            try
            {
                using (var ldap = GetLdapConnection())
                {
                    var memberAttribute = $"cn={netid},{LdapSearchBase}";
                    var ldapAttribute = new LdapAttribute("member", memberAttribute);
                    var groupMod = new LdapModification(action, ldapAttribute);
                    ldap.Modify(adPath, groupMod);
                }
            } 
            catch (Exception ex)
            {
                string actionDesc = action == 0 ? "ADD" : "REMOVE";
                string msg = $"Failed to {actionDesc} {netid} from {name} group";
                Logging.GetLogger(nameof(ModifyToolGroupMembership), new{action= actionDesc, tool= name, path= adPath }).Error(ex, msg);
                throw new Exception(msg, ex);
            }    
        }

        private static TaskOptions RetryOptions = new TaskOptions(
            new TaskRetryOptions(
                new RetryPolicy(3, TimeSpan.FromSeconds(5))
            )
        );

        public static LdapConnection GetLdapConnection()
        {
            var adsUser = $"ads\\{Utils.Env("AdToolsGroupManagerUser", required:true)}";
            var adsPassword = Utils.Env("AdToolsGroupManagerPassword", required:true);
            var ldap = new LdapConnection() {SecureSocketLayer = true};
            ldap.Connect("ads.iu.edu", 636);
            ldap.Bind(adsUser, adsPassword);    
            return ldap;     
        }
    }
}
