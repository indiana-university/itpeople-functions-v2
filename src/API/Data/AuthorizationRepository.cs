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
    public class AuthorizationRepository
    {
        public static Result<bool, Error> AuthorizeModification(EntityPermissions permissions) 
            => permissions.HasFlag(EntityPermissions.Put)
                ? Pipeline.Success(true)
                : Pipeline.Unauthorized();


        internal async static Task<Result<EntityPermissions, Error>> DeterminePersonPermissions(HttpRequest req, string requestorNetid, int personId)
        {
            // if requestor is a service admin, they can get/put
            // if requestor manages a person's unit, they can get/put
            // if requestor is person, they can get/put
            // otherwise, get only

            try
            {
                using (var db = PeopleContext.Create())
                {
                    //By default a user can only "get"
                    var result = EntityPermissions.Get;
                    
                    // Fetch the person entity for the requestor and target person.
                    var persons = await db.People
                        .Include(p => p.UnitMemberships)
                        .Where(p => p.Netid.ToLower() == requestorNetid.ToLower() || p.Id == personId)
                        .ToListAsync();

                    // If we didn't find an entity for the requestor they aren't authorized
                    // if(persons.Any(p => p.Netid.ToLower() == requestorNetid.ToLower()) == false)
                    // {
                    //     result = Pipeline.Success(EntityPermissions.Get);
                    // }

                    // If we didn't find the target person return a 404
                    if(persons.Any(p => p.Id == personId) == false)
                    {
                        return Pipeline.NotFound("No person was found with the ID provided.");
                    }

                    // If requestor and and target person are the same they are authorized.
                    if(persons.All(p => p.Id == personId))
                    {
                        result = EntityPermissions.Get | EntityPermissions.Put;
                    }

                    // TODO: Is requestor a service admin? If so they can get/put

                    // Does the requestor manage a unit that the target person is a member of?
                    var requestorManagedUnits = persons.First(p => p.Netid.ToLower() == requestorNetid.ToLower())
                        .UnitMemberships
                        .Where(m => m.Permissions == UnitPermissions.Owner || m.Permissions == UnitPermissions.ManageMembers)
                        .Select(m => m.UnitId)
                        .ToList();

                    var personMemberOfUnits = persons.First(p => p.Id == personId)
                        .UnitMemberships
                        .Select(m => m.UnitId)
                        .ToList();
                    
                    var matches = requestorManagedUnits.Intersect(personMemberOfUnits);
                    if(matches.Count() > 0)
                    {
                        result = EntityPermissions.Get | EntityPermissions.Put;
                    }

                    // attach those permission to the response headers.
                    req.HttpContext.Response.Headers[Response.Headers.XUserPermissions] = result.ToString();
                    
                    // return the entity permissions to the next step of the pipeline.
                    return Pipeline.Success(result);
                }
            }
            catch (System.Exception ex)
            {
                return Pipeline.InternalServerError(ex.Message);
            }
        }
    }
}