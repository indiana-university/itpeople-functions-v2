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

                    var requestor = persons.FirstOrDefault(p => p.Netid.ToLower() == requestorNetid.ToLower());
                    var target = persons.FirstOrDefault(p => p.Id == personId);

                    // If we didn't find the target person return a 404
                    if(target == null)
                    {
                        return Pipeline.NotFound("No person was found with the ID provided.");
                    }

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