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

        internal static async Task<Result<List<Person>, Error>> GetAllAsync(People.PeopleSearchQueryParameters query)
        {
            try
            {
                using (var db = PeopleContext.Create())
                {
                    
                    var result = await db.People
                        .Where(p=> 
                            (string.IsNullOrWhiteSpace(query.Q) 
                                || EF.Functions.ILike(p.Netid, $"%{query.Q}%")
                                || EF.Functions.ILike(p.Name, $"%{query.Q}%"))
                            && (query.Responsibilities == Responsibilities.None
                                || ((int)p.Responsibilities & (int)query.Responsibilities) != 0)
                            && (query.Expertise.Length == 0
                                || query.Expertise.Any(exp => EF.Functions.ILike(p.Expertise, $"%{exp}%")))
                        )
                        .AsNoTracking()
                        .ToListAsync();
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