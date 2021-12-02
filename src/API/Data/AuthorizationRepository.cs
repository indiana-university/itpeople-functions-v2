using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

namespace API.Data
{
	public class AuthorizationRepository : DataRepository
    {
        public static Result<bool, Error> AuthorizeModification(EntityPermissions permissions) 
            => permissions.HasFlag(EntityPermissions.Put)
                ? Pipeline.Success(true)
                : Pipeline.Forbidden();

        public static Result<bool, Error> AuthorizeCreation(EntityPermissions permissions) 
            => permissions.HasFlag(EntityPermissions.Post)
                ? Pipeline.Success(true)
                : Pipeline.Forbidden();
        
        public static Result<bool, Error> AuthorizeDeletion(EntityPermissions permissions) 
            => permissions.HasFlag(EntityPermissions.Delete)
                ? Pipeline.Success(true)
                : Pipeline.Forbidden();

        internal static Task<Result<EntityPermissions, Error>> DeterminePersonPermissions(HttpRequest req, string requestorNetid, int personId)
            => ExecuteDbPipeline("resolve person permissions", db =>
                FetchPeople(db, requestorNetid, personId)
                .Bind(tup => ResolvePersonPermissions(tup.requestor, tup.target))
                .Tap(perms => req.SetEntityPermissions(perms)));
        internal static Task<Result<EntityPermissions, Error>> DeterminePersonPermissions(HttpRequest req, string requestorNetid, string netId)
            => ExecuteDbPipeline("resolve person permissions", db =>
                FetchPeople(db, requestorNetid, netId)
                .Bind(tup => ResolvePersonPermissions(tup.requestor, tup.target))
                .Tap(perms => req.SetEntityPermissions(perms)));
        
        private static async Task<Result<(Person requestor, Person target),Error>> FetchPeople(PeopleContext db, string requestorNetid, int personId)
        {
            var requestor = await FindRequestorOrDefault(db, requestorNetid);
            var target = await db.People.Include(p => p.UnitMemberships).SingleOrDefaultAsync(p => p.Id == personId);
            return target == null
                ? Pipeline.NotFound("No person was found with the ID provided.")
                : Pipeline.Success((requestor, target));
        }
        private static async Task<Result<(Person requestor, Person target),Error>> FetchPeople(PeopleContext db, string requestorNetid, string netId)
        {
            var requestor = await FindRequestorOrDefault(db, requestorNetid);
            var target = await db.People.Include(p => p.UnitMemberships).SingleOrDefaultAsync(p => p.Netid == netId);
            return target == null
                ? Pipeline.NotFound("No person was found with the ID provided.")
                : Pipeline.Success((requestor, target));
        }

        public static Result<EntityPermissions,Error> ResolvePersonPermissions(Person requestor, Person target)
        {
            //By default a user can only "get"
            var result = EntityPermissions.Get;

            if (requestor != null)
            {
                var requestorManagedUnits = requestor
                    .UnitMemberships
                    .Where(m => m.Permissions == UnitPermissions.Owner || m.Permissions == UnitPermissions.ManageMembers)
                    .Select(m => m.UnitId)
                    .ToList();

                var personMemberOfUnits = target
                    .UnitMemberships
                    .Select(m => m.UnitId)
                    .ToList();
                
                var matches = requestorManagedUnits.Intersect(personMemberOfUnits);

                if (requestor.IsServiceAdmin // requestor is a service admin
                    || requestor.Id == target.Id // requestor and and target person are the same
                    || matches.Count() > 0)  // requestor manages a unit that the target person is a member of
                {
                    result = PermsGroups.GetPut;
                }   
            }

            // return the entity permissions to the next step of the pipeline.
            return Pipeline.Success(result);
        }

        internal static Task<Result<EntityPermissions, Error>> DetermineUnitPermissions(HttpRequest req, string requestorNetId) 
            => ExecuteDbPipeline("resolve unit permissions", db =>
                FetchPersonAndMembership(db, requestorNetId)
                .Bind(person => ResolveUnitPermissions(person))
                .Tap(perms => req.SetEntityPermissions(perms)));

        internal static Task<Result<EntityPermissions, Error>> DetermineUnitManagementPermissions(HttpRequest req, string requestorNetId, int unitId, UnitPermissions permissions = UnitPermissions.ManageMembers) 
            => ExecuteDbPipeline($"resolve unit {unitId} and unit member management permissions", db =>
                FetchPersonAndMembership(db, requestorNetId, unitId)
                .Bind(person => ResolveUnitManagmentPermissions(person, unitId, permissions, db))
                .Tap(perms => req.SetEntityPermissions(perms)));

        internal static Task<Result<EntityPermissions, Error>> DetermineUnitMemberToolPermissions(HttpRequest req, string requestorNetId, int membershipId) 
            => ExecuteDbPipeline($"resolve unit {membershipId} member management permissions", db =>
                FetchPersonAndMembership(db, requestorNetId)
                .Bind(person => ResolveMembershipToolPermissions(person, membershipId, db))
                .Tap(perms => req.SetEntityPermissions(perms)));

        private static async Task<Result<Person,Error>> FetchPersonAndMembership(PeopleContext db, string requestorNetid)
        {
            var requestor = await FindRequestorOrDefault(db, requestorNetid);
            return Pipeline.Success(requestor);
        }

        private static async Task<Result<Person,Error>> FetchPersonAndMembership(PeopleContext db, string requestorNetid, int unitId)
        {
            var requestor = await FindRequestorOrDefault(db, requestorNetid);
            var unit = await db.Units.SingleOrDefaultAsync(u => u.Id == unitId);
            return unit == null
                ? Pipeline.NotFound("No unit was found with the ID provided.")
                : Pipeline.Success(requestor);
        }

        public static Result<EntityPermissions, Error> ResolveUnitPermissions(Person requestor) 
            => requestor != null && requestor.IsServiceAdmin
                ? Pipeline.Success(PermsGroups.All)
                : Pipeline.Success(EntityPermissions.Get);
                
        public static async Task<Result<EntityPermissions,Error>> ResolveUnitManagmentPermissions(Person requestor, int unitId, List<UnitPermissions> anyGetsAllPermissions, PeopleContext db)
        {
            if (requestor == null)
                return Pipeline.Success(EntityPermissions.Get);

            // Grant all user permissions to Service Admins.
            if (requestor.IsServiceAdmin)
                return Pipeline.Success(PermsGroups.All);

            // Find all units in which  the requestor has the required unit permissions.
            var privilegedUnits = requestor.UnitMemberships
                .Where(um => um.Permissions == UnitPermissions.Owner || anyGetsAllPermissions.Contains(um.Permissions))
                .Select(um => um.UnitId);

            // Grant minimal user permissions if *none* of the requestor's unit 
            //   memberships contain the required unit permissions. 
            if (false == privilegedUnits.Any())
                return Pipeline.Success(EntityPermissions.Get);

            // Grant all user permissions if the requestor has the required unit 
            //  permissions in this unit or any parent unit in the hierarchy. 
            //  Otherwise, grant minimal user permissions.
            var unitsInHierarchy = (await BuildUnitTree(unitId, db)).Select(u => u.Id);
            return (privilegedUnits.Intersect(unitsInHierarchy).Any())
                ? Pipeline.Success(PermsGroups.All)
                : Pipeline.Success(EntityPermissions.Get);
        }

        public static async Task<Result<EntityPermissions,Error>> ResolveUnitManagmentPermissions(Person requestor, int unitId, UnitPermissions getsAllPermissions, PeopleContext db)
            => await ResolveUnitManagmentPermissions(requestor, unitId, new List<UnitPermissions> { getsAllPermissions }, db);

        private static Task<List<Unit>> BuildUnitTree(int unitId, PeopleContext db) 
            => db.Units.FromSqlInterpolated($@"
                WITH RECURSIVE parentage AS (
                -- first row
                SELECT id, active, name, parent_id
                FROM units
                WHERE id = {unitId}
                UNION
                -- recurse
                SELECT u.id, u.active, u.name, u.parent_id
                FROM units u
                INNER JOIN parentage p ON p.parent_id = u.id
                ) 
                SELECT id, active, name, parent_id, '' AS description, '' AS email, '' AS url
                FROM parentage
            ")
            .ToListAsync();

        public static async Task<Result<EntityPermissions,Error>> ResolveMembershipToolPermissions(Person requestor, int membershipId, PeopleContext db)
        {
            // service admins: get post put delete
            if (requestor != null && requestor.IsServiceAdmin)
                return Pipeline.Success(PermsGroups.All);     

            var membership = await db.UnitMembers
                .Include(um => um.Unit)
                .SingleOrDefaultAsync(um => um.Id == membershipId);
            
            if(membership == null)
                return Pipeline.Success(EntityPermissions.Get);
            
            return await ResolveUnitManagmentPermissions(requestor, membership.Unit.Id, new List<UnitPermissions> { UnitPermissions.ManageTools, UnitPermissions.ManageMembers }, db);
        }

        private static Task<Person> FindRequestorOrDefault(PeopleContext db, string requestorNetid) 
            => 
                string.IsNullOrWhiteSpace(requestorNetid)
                ? Task.FromResult<Person>(null)
                : db.People
                    .Include(p => p.UnitMemberships)
                    .SingleOrDefaultAsync(p => p.Netid.ToLower() == requestorNetid.ToLower());
    }
}