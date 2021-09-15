using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;
using API.Functions;
using System;
using Microsoft.AspNetCore.Http;

namespace API.Data
{

    public class PeopleRepository : DataRepository
    {

        internal static Task<Result<List<Person>, Error>> GetAll(PeopleSearchParameters query)
            => ExecuteDbPipeline("search all people", async db => {
                    var parsedName = GetParsedName(query.Q);
                    var result = await GetPeopleFilteredByArea(db, query)
                        .Where(p => // partial match netid and/or name
                            string.IsNullOrWhiteSpace(query.Q)
                                || EF.Functions.ILike(p.Netid, $"%{query.Q}%")
                                || EF.Functions.ILike(p.Name, $"%{query.Q}%")
                                || ((string.IsNullOrWhiteSpace(parsedName.firstName) == false
                                    || string.IsNullOrWhiteSpace(parsedName.lastName) == false)
                                    && EF.Functions.ILike(p.Name, $"{parsedName.firstName}%{parsedName.lastName}%")))
                        .Where(p => // check for overlapping responsibilities / job classes
                            query.Responsibilities == Responsibilities.None
                                || ((int)p.Responsibilities & (int)query.Responsibilities) != 0)
                                // That & is a bitwise operator - go read-up!
                                // https://stackoverflow.com/questions/12988260/how-do-i-test-if-a-bitwise-enum-contains-any-values-from-another-bitwise-enum-in
                        .Where(p => // partial match any supplied interest against any self-described expertise
                            query.Expertise.Length == 0
                                || query.Expertise.Select(s => $"%{s}%").ToArray().Any(s => EF.Functions.ILike(p.Expertise, s)))
                        .Where(p => // partial match campus
                            query.Campus.Length == 0
                                || query.Campus.Select(s => $"%{s}%").ToArray().Any(s => EF.Functions.ILike(p.Campus, s)))
                        .Where(p => query.Roles.Length == 0
                            || p.UnitMemberships.Any(m => query.Roles.Contains(m.Role) && m.Unit.Active))
                        .Where(p => query.Permissions.Length == 0
                            || p.UnitMemberships.Any(m => query.Permissions.Contains(m.Permissions) && m.Unit.Active))
                        .Include(p => p.Department)
                        .AsNoTracking()
                        .ToListAsync();
                    return Pipeline.Success(result);
                });

        private static (string firstName, string lastName) GetParsedName(string nameQuery)
        {
            if (string.IsNullOrWhiteSpace(nameQuery) == false && nameQuery.Count(q => q == ',') == 1)
            {
                //take something like Drake, Jared and make it Jared Drake, but return as (firstName, LastName) tuple
                var parts = nameQuery.Split(',').Select(q => q.Trim()).ToArray();
                return (parts[1], parts[0]);
            }
            return ("", "");
        }

        private static IQueryable<Person> GetPeopleFilteredByArea(PeopleContext db, PeopleSearchParameters query)
            => db.People .FromSqlInterpolated<Person>($@"
                    SELECT DISTINCT p.*
                    FROM public.people p
                    JOIN public.unit_members um ON um.person_id = p.id
                    JOIN public.units u
                        ON u.id = um.unit_id
                        AND u.Active = True
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

        private static IQueryable<PeopleLookupItem> SearchHrPeopleByNameOrNetId(PeopleContext db, HrPeopleSearchParameters query)
        {
            var parsedName = GetParsedName(query.Q);
            return db.HrPeople
                .Where(h=>  EF.Functions.ILike(h.Netid, $"%{query.Q}%")
                            || EF.Functions.ILike(h.Name, $"%{query.Q}%")
                            || ((string.IsNullOrWhiteSpace(parsedName.firstName) == false
                                || string.IsNullOrWhiteSpace(parsedName.lastName) == false)
                                && EF.Functions.ILike(h.Name, $"{parsedName.firstName}%{parsedName.lastName}%")))
                .Select(h => new PeopleLookupItem { Id = 0, NetId = h.Netid, Name = h.Name })
                .AsNoTracking();
        }

        private static IQueryable<PeopleLookupItem> SearchBothByNameOrNetId(PeopleContext db, HrPeopleSearchParameters query)
        {
            var parsedName = GetParsedName(query.Q);
            //Get existing people matches
            var peopleMatches = db.People
                .Where(p=> EF.Functions.ILike(p.Netid, $"%{query.Q}%")
                            || EF.Functions.ILike(p.Name, $"%{query.Q}%")
                            || ((string.IsNullOrWhiteSpace(parsedName.firstName) == false
                                || string.IsNullOrWhiteSpace(parsedName.lastName) == false)
                                && EF.Functions.ILike(p.Name, $"{parsedName.firstName}%{parsedName.lastName}%")))
                .Select(p => new PeopleLookupItem { Id = p.Id, NetId = p.Netid, Name = p.Name })
                .Take(query.Limit)
                .AsNoTracking();
            var existingNetIds = peopleMatches.Select(p => p.NetId.ToLower()).ToList();

            //Get possible matches from the HrPeople table, and exclude any existing users.
            var hrPeopleMatches = SearchHrPeopleByNameOrNetId(db, query)
                .Where(h => existingNetIds.Contains(h.NetId.ToLower()) == false)
                .Take(query.Limit);

            return peopleMatches
                .Union(hrPeopleMatches)
                .Take(query.Limit);
        }

        public static Task<Result<Person, Error>> GetOne(int id)
            => ExecuteDbPipeline("get a person by ID", db =>
                TryFindPerson(db, id));
        public static Task<Result<Person, Error>> GetOne(string id)
            => ExecuteDbPipeline("get a person by Netid", db =>
                TryFindPerson(db, id));

        private static Result<List<UnitMember>, Error> GetActiveMemberships(Person person)
            => Pipeline.Success(person.UnitMemberships.Where(um => um.Unit.Active).ToList());

        public static Task<Result<List<UnitMember>, Error>> GetMemberships(int id)
            => ExecuteDbPipeline("fetch unit memberships", db =>
                TryFindPerson(db, id)
                .Bind(person => GetActiveMemberships(person)));
        public static Task<Result<List<UnitMember>, Error>> GetMemberships(string username)
            => ExecuteDbPipeline("fetch unit memberships", db =>
                TryFindPerson(db, username)
                .Bind(person => GetActiveMemberships(person)));
        public static Task<Result<Person, Error>> Update(HttpRequest req, int id, PersonUpdateRequest body)
            => ExecuteDbPipeline("update person", db =>
                TryFindPerson(db, id)
                .Tap(person => LogPrevious(req, person))
                .Bind(person => TryUpdatePerson(db, body, person)));

        private static async Task<Result<Person,Error>> TryFindPerson (PeopleContext db, int id)
        {
            var person = await db.People
                .Include(p => p.Department)
                .Include(p => p.UnitMemberships).ThenInclude(um => um.Unit)
                .SingleOrDefaultAsync(p => p.Id == id);
            return person == null
                ? Pipeline.NotFound("No person found with that Id.")
                : Pipeline.Success(person);
        }
        private static async Task<Result<Person,Error>> TryFindPerson (PeopleContext db, string username)
        {
            var person = await db.People
                .Include(p => p.Department)
                .Include(p => p.UnitMemberships).ThenInclude(um => um.Unit)
                .SingleOrDefaultAsync(p => EF.Functions.ILike(p.Netid, username));
            return person == null
                ? Pipeline.NotFound("No person found with that netid.")
                : Pipeline.Success(person);
        }

        private static async Task<Result<Person,Error>> TryUpdatePerson (PeopleContext db, PersonUpdateRequest body, Person record)
        {
            // update the props
            record.Location = body.Location;
            record.Expertise = body.Expertise;
            record.PhotoUrl = body.PhotoUrl;
            record.Responsibilities = body.Responsibilities;
            // save changes
            await db.SaveChangesAsync();
            return Pipeline.Success(record);
        }

        internal static Task<Result<List<PeopleLookupItem>, Error>> GetAllWithHr(HrPeopleSearchParameters query)
            => ExecuteDbPipeline("search all people by netId", async db => {
                    var result = await SearchBothByNameOrNetId(db, query).ToListAsync();
                    return Pipeline.Success(result);
                });
    }
}
