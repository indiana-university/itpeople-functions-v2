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

        internal static async Task<Result<List<Person>, Error>> GetAll(PeopleSearchParameters query)
        {
            try
            {
                using (var db = PeopleContext.Create())
                {                    
                    var result = await GetPeopleFilteredByArea(db, query)
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
                        .Where(p => query.Roles.Length == 0
                                || p.UnitMemberships.Any(m => query.Roles.Contains(m.Role)))
                        .Where(p => query.Permissions.Length == 0
                                || p.UnitMemberships.Any(m => query.Permissions.Contains(m.Permissions)))
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

        private static IQueryable<Person> GetPeopleFilteredByArea(PeopleContext db, PeopleSearchParameters query)
        {
            return db.People
                .FromSqlInterpolated<Person>($@"
                    SELECT p.* from public.people p
                        JOIN public.unit_members um ON um.person_id = p.id
                    WHERE CARDINALITY({query.Areas}) = 0 -- no area specified
                        OR {query.Areas} = ARRAY[1,2]    -- both uits and edge requested
                        -- Area=1: UITS unit members only
                        OR ({query.Areas} = ARRAY[1] AND um.unit_id IN (
                            WITH RECURSIVE parentage AS (
                                SELECT id, id as root_id FROM units WHERE parent_id IS NULL
                            UNION
                                SELECT u.id, p.root_id as root_id FROM units u INNER JOIN parentage p ON u.parent_id = p.id
                            )
                            SELECT id FROM parentage
                            WHERE root_id = 1))
                        -- Area=2: Edge unit members only
                        OR ({query.Areas} = ARRAY[2] AND um.unit_id IN (
                            WITH RECURSIVE parentage AS (
                                SELECT id, id as root_id FROM units WHERE parent_id IS NULL
                            UNION
                                SELECT u.id, p.root_id as root_id FROM units u INNER JOIN parentage p ON u.parent_id = p.id
                            )
                            SELECT id FROM parentage
                            WHERE root_id <> 1))");
        }


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

        // /// Get a user class for a given net ID (e.g. 'jhoerr')
        // //TryGetId: NetId -> Async<Result<NetId * Id option,Error>>
        public static async Task<Result<Person,Error>> GetOne(int id)
        {
            try
            {
                using (var db = PeopleContext.Create())
                {
                    var person = await db.People.Include(p => p.Department).SingleOrDefaultAsync(p => p.Id == id);
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