using System;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
    public class AuthorizationRepository : DataRepository
    {
        public static Result<bool, Error> AuthorizeModification(EntityPermissions permissions) 
            => permissions.HasFlag(EntityPermissions.Put)
                ? Pipeline.Success(true)
                : Pipeline.Unauthorized();


        internal static Task<Result<EntityPermissions, Error>> DeterminePersonPermissions(HttpRequest req, string requestorNetid, int personId)
            => ExecuteDbPipeline("resolve person permissions", db =>
                FetchPeople(db, requestorNetid, personId)
                .Bind(tup => ResolvePersonPermissions(tup.requestor, tup.target))
                .Tap(perms => AddResponseHeaders(req, perms)));
        
        private static async Task<Result<(Person requestor, Person target),Error>> FetchPeople(PeopleContext db, string requestorNetid, int personId)
        {
            var requestor = await FindRequestorOrDefault(db, requestorNetid);
            var target = await db.People.SingleOrDefaultAsync(p => p.Id == personId);
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
                    result = EntityPermissions.GetPut;
                }   
            }

            // return the entity permissions to the next step of the pipeline.
            return Pipeline.Success(result);
        }

        internal static Task<Result<EntityPermissions, Error>> DetermineUnitPermissions(HttpRequest req, string requestorNetId) 
            => ExecuteDbPipeline("resolve unit permissions", db =>
                FetchPersonAndMembership(db, requestorNetId)
                .Bind(person => ResolveUnitPermissions(person))
                .Tap(perms => AddResponseHeaders(req, perms)));

        internal static Task<Result<EntityPermissions, Error>> DetermineUnitPermissions(HttpRequest req, string requestorNetId, int unitId) 
            => ExecuteDbPipeline("resolve unit permissions", db =>
                FetchPersonAndMembership(db, requestorNetId, unitId)
                .Bind(person => ResolveUnitPermissions(person, unitId))
                .Tap(perms => AddResponseHeaders(req, perms)));

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
                ? Pipeline.Success(EntityPermissions.All)
                : Pipeline.Success(EntityPermissions.Get);
                
        public static Result<EntityPermissions,Error> ResolveUnitPermissions(Person requestor, int unitId)
        {
            var result = EntityPermissions.Get;
            // service admins: get post put delete
            if (requestor.IsServiceAdmin)
            {
                result = EntityPermissions.All;
            }   
            // Requestor owner/manage roles can get put
            if(requestor != null && requestor.UnitMemberships.Any(um => um.UnitId == unitId && (um.Permissions == UnitPermissions.Owner || um.Permissions == UnitPermissions.ManageMembers)))
            {
                result = EntityPermissions.GetPut;
            }
            
            return Pipeline.Success(result);
        }

        private static Task<Person> FindRequestorOrDefault(PeopleContext db, string requestorNetid) 
            => db.People
                .Include(p => p.UnitMemberships)
                .SingleOrDefaultAsync(p => p.Netid.ToLower() == requestorNetid.ToLower());

        private static void AddResponseHeaders(HttpRequest req, EntityPermissions permissions)
        {
            req.HttpContext.Response.Headers[Response.Headers.XUserPermissions] = permissions.ToString();
            // TODO: CORS stuff...
            req.HttpContext.Response.Headers[Response.Headers.AccessControlExposeHeaders] = Response.Headers.XUserPermissions;
        }
    }


}