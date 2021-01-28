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
            var persons = await db.People
                .Include(p => p.UnitMemberships)
                .Where(p => p.Netid.ToLower() == requestorNetid.ToLower() || p.Id == personId)
                .ToListAsync();
            var target = persons.FirstOrDefault(p => p.Id == personId);
            var requestor = persons.FirstOrDefault(p => p.Netid.ToLower() == requestorNetid.ToLower());
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
                    result = EntityPermissions.Get | EntityPermissions.Put;
                }   
            }

            // return the entity permissions to the next step of the pipeline.
            return Pipeline.Success(result);
        }

        private static void AddResponseHeaders(HttpRequest req, EntityPermissions permissions)
        {
            req.HttpContext.Response.Headers[Response.Headers.XUserPermissions] = permissions.ToString();
        }
    }


}