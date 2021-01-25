using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;
using API.Functions;

namespace API.Data
{

    public class PeopleRepository
    {
        public PeopleRepository() 
        {
        }

        internal static async Task<Result<List<Person>, Error>> GetAllAsync(PeopleSearchParameters query)
        {
            try
            {
                using (var db = PeopleContext.Create())
                {                    
                    var result = await db.People
                        .Where(p=> // partial match netid and/or name
                            string.IsNullOrWhiteSpace(query.Q) 
                                || EF.Functions.ILike(p.Netid, $"%{query.Q}%")
                                || EF.Functions.ILike(p.Name, $"%{query.Q}%"))
                        .Where(p=> // check for overlapping responsibilities / job classes
                            query.Responsibilities == Responsibilities.None
                                || ((int)p.Responsibilities & (int)query.Responsibilities) != 0)
                                // That & is a bitwise operator - go read-up!
                                // https://stackoverflow.com/questions/12988260/how-do-i-test-if-a-bitwise-enum-contains-any-values-from-another-bitwise-enum-in
                        .Where(p=> // partial match any supplied interest against any self-described expertise
                            query.Expertise.Length == 0
                                || query.Expertise.Select(s=>$"%{s}%").ToArray().Any(s => EF.Functions.ILike(p.Expertise, s)))
                        .Where(p=> // partial match campus
                            query.Campus.Length == 0
                                || query.Campus.Select(s=>$"%{s}%").ToArray().Any(s => EF.Functions.ILike(p.Campus, s)))
                        .AsNoTracking()
                        .ToListAsync();

                        // Fetch memberships that satisfy our role and our existing results.
                        // NB: We're only doing this because the existing Person model doesn't have a relationship to it's UnitMember(s)
                        if(query.Role != null)
                        {
                            var peopleIds = result.Select(r => (int?)r.Id).ToList();
                            var peopleIdsWithRole = db.UnitMembers.Include(m => m.Person)
                                .Where(m =>  peopleIds.Contains(m.PersonId)  && m.Role == query.Role)
                                .Select(m => m.PersonId)
                                .ToList();
                            
                            result = result
                                .Where(p => peopleIdsWithRole.Contains(p.Id))
                                .ToList();
                        }
                        if(query.Permissions != null)
                        {
                            var peopleIds = result.Select(r => (int?)r.Id).ToList();
                            var peopleIdsWithPermissions = db.UnitMembers.Include(m => m.Person)
                                .Where(m =>  peopleIds.Contains(m.PersonId)  && m.Permissions == query.Permissions)
                                .Select(m => m.PersonId)
                                .ToList();
                            
                            result = result
                                .Where(p => peopleIdsWithPermissions.Contains(p.Id))
                                .ToList();
                        }
                    return Pipeline.Success(result);
                }
            }
            catch (System.Exception ex)
            {
                return Pipeline.InternalServerError(ex.Message);
            }        
        }



        // /// Get a user class for a given net ID (e.g. 'jhoerr')
        // //TryGetId: NetId -> Async<Result<NetId * Id option,Error>>
        // public async Task<Result<(NetId, Id), option, Error>> TryGetId(string NetId) {
        //     throw new NotImplementedException();
        // }
        // /// Get a list of all people
        // public async Task<Result<List<Person>,Error>> GetAll(PeopleQuery peopleQuery) => throw new NotImplementedException();
        // /// Get a unioned list of IT and HR people, filtered by name/netid
        // public async Task<Result<List<Person>,Error>> GetAllWithHr(string NetId) => throw new NotImplementedException();
        // /// Get a single HR person by NetId
        // public async Task<Result<Person,Error>> GetHr(string NetId) => throw new NotImplementedException();
        // /// Get a single person by ID
        // public async Task<Result<Person,Error>> GetById(int Id) => throw new NotImplementedException();
        /// Get a single person by NetId


        public async Task<Result<Person,Error>> GetByNetId(string netid)
        {
            try
            {
                using (var db = PeopleContext.Create())
                {
                    var person = await db.People.SingleOrDefaultAsync(p => EF.Functions.Like(p.Netid, netid));
                    return person == null
                        ? Pipeline.NotFound("No person found with that netid.")
                        : Pipeline.Success(person);
                }
            }
            catch (System.Exception ex)
            {
                return Pipeline.InternalServerError(ex.Message);
            }
        }
        // /// Create a person from canonical HR data
        // public  async Task<Result<Person,Error>> Create(Person person) => throw new NotImplementedException();
        // /// Get a list of a person's unit memberships, by the person's ID
        // public async Task<Result<List<UnitMember>,Error>> GetMemberships(Id id) => throw new NotImplementedException();
        
        // /// Update a person
        // public async Task<Result<Person,Error>> Update(PersonRequest personRequest) => throw new NotImplementedException();
    }
}