using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
    public class AuthorizationRepository
    {
        public static async Task<Result<bool, Error>> CanModifyPerson(string requestorNetid, int personId)
        {
            try
            {
                using (var db = PeopleContext.Create())
                {
                    // Fetch the person entity for the requestor and target person.
                    var persons = await db.People
                        .Include(p => p.UnitMemberships)
                        .Where(p => p.Netid.ToLower() == requestorNetid.ToLower() || p.Id == personId)
                        .ToListAsync();

                    // If we didn't find an entity for the requestor they aren't authorized
                    if(persons.Any(p => p.Netid.ToLower() == requestorNetid.ToLower()) == false)
                    {
                        return Pipeline.Unauthorized();
                    }

                    // If we didn't find the target person return a 404
                    if(persons.Any(p => p.Id == personId) == false)
                    {
                        return Pipeline.NotFound("No person was found with the ID provided.");
                    }

                    // If requestor and and target person are the same they are authorized.
                    if(persons.All(p => p.Id == personId))
                    {
                        return Pipeline.Success(true);
                    }

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
                        return Pipeline.Success(true);
                    }

                    // If the requestor and target person combo didn't satisfy any of the above conditions they are not authorized.
                    return Pipeline.Unauthorized();
                }
            }
            catch (System.Exception ex)
            {
                return Pipeline.InternalServerError(ex.Message);
            }
        }
    }

}