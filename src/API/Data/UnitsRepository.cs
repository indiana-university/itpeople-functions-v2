using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;
using API.Functions;
using Microsoft.AspNetCore.Http;

namespace API.Data
{
    public class UnitsRepository : DataRepository
    {
        internal static Task<Result<List<Unit>, Error>> GetAll(UnitSearchParameters query)
            => ExecuteDbPipeline("search all units", async db => {
                    IQueryable<Unit> queryable = db.Units.Include(u => u.Parent);
                    if(string.IsNullOrWhiteSpace(query.Q))
                    {   // return all top-level units if there's no query string
                        queryable = queryable.Where(u => u.Parent == null);
                    }
                    else
                    {   // otherwise search on name/description
                        queryable = queryable.Where(u=>
                            EF.Functions.ILike(u.Description, $"%{query.Q}%")
                            || EF.Functions.ILike(u.Name, $"%{query.Q}%"));
                    }                    
                    var result = await queryable.AsNoTracking().ToListAsync();
                    return Pipeline.Success(result);
                });

        public static Task<Result<Unit, Error>> GetOne(int id) 
            => ExecuteDbPipeline("get a unit by ID", db => 
                TryFindUnit(db, id));

        internal static async Task<Result<Unit, Error>> CreateUnit(UnitRequest body)
            => await ExecuteDbPipeline("create a unit", db =>
                TryValidateParentExists(db, body)
                .Bind(_ => TryCreateUnit(db, body))
                .Bind(created => TryFindUnit(db, created.Id))
            );

        internal static async Task<Result<Unit, Error>> UpdateUnit(HttpRequest req, UnitRequest body, int unitId)
        {
            return await ExecuteDbPipeline($"update unit {unitId}", db =>
                TryValidateParentExists(db, body)
                .Bind(_ => TryFindUnit(db, unitId))
                .Tap(existing => LogPrevious(req, existing))
                .Bind(existing => TryUpdateUnit(db, existing, body))
                .Bind(_ => TryFindUnit(db, unitId))
            );
        }

        internal static async Task<Result<bool, Error>> DeleteUnit(HttpRequest req, int unitId)
        {
            return await ExecuteDbPipeline($"delete unit {unitId}", db =>
                TryFindUnit(db, unitId)
                .Bind(unit => TryDeleteUnit(db, req, unit)));
        }

        private static async Task<Result<Unit,Error>> TryFindUnit (PeopleContext db, int id, bool findingParent = false)
        {
            var unit = await db.Units
                .Include(u => u.Parent)
                .SingleOrDefaultAsync(p => p.Id == id);
            return unit == null
                ? Pipeline.NotFound($"No {(findingParent ? "parent" : "")} unit found with ID ({id}).")
                : Pipeline.Success(unit);
        }

        private static async Task<Result<UnitRequest,Error>> TryValidateParentExists (PeopleContext db, UnitRequest body)
        {
            if(body.ParentId.HasValue && body.ParentId != 0)
            {
                return await TryFindUnit(db, (int)body.ParentId, true)
                    .Bind(_ => Pipeline.Success(body));
            }
            else
            {
                return Pipeline.Success(body);
            }
        }

        private static async Task<Result<Unit,Error>> TryCreateUnit (PeopleContext db, UnitRequest body)
        {
            var unit = new Unit(body.Name, body.Description, body.Url, body.Email, body.ParentId);

            // add the unit
            db.Units.Add(unit);
            // TODO: We might need to force the validation of unit before EF saves it to the DB.
            // save changes
            await db.SaveChangesAsync();
            return Pipeline.Success(unit);
        }

        private static async Task<Result<Unit, Error>> TryUpdateUnit(PeopleContext db, Unit existing, UnitRequest body)
        {
            existing.Name = body.Name;
            existing.Description = body.Description;
            existing.Url = body.Url;
            existing.Email = body.Email;
            existing.ParentId = body.ParentId;

            // TODO: Do we need to manually validate the model?  EF doesn't do that for free any more.
            await db.SaveChangesAsync();
            return Pipeline.Success(existing);
        }

        private static async Task<Result<bool,Error>> TryDeleteUnit (PeopleContext db, HttpRequest req, Unit unit)
        {
            // Check for child Units
            var childUnitIds = db.Units
                .Include(c => c.Parent)
                .Where(c => c.ParentId == unit.Id)
                .Select(c => c.Id)
                .ToList();
            if(childUnitIds.Count > 0)
            {
                return Pipeline.Conflict($"Unit {unit.Id} has child units, with ids: {string.Join(", ", childUnitIds)}. These must be reassigned prior to deletion.");
            }
            else
            {
                //Remove, and log, UnitMembers and SupportRelationships for this unit.
                var unitAndRelated = db.Units
                    .Include(u => u.Parent)
                    .Include(u => u.UnitMembers).ThenInclude(um => um.Person)
                    .Include(u => u.UnitMembers).ThenInclude(um => um.MemberTools)
                    .Include(u => u.SupportRelationships).ThenInclude(sr => sr.Department)
                    .Single(u => u.Id == unit.Id);

                //Write the logs with the whole enchilada 
                LogPrevious(req, unitAndRelated);
                //Remove MemberTools for unit
                db.MemberTools.RemoveRange(unitAndRelated.UnitMembers.SelectMany(um => um.MemberTools));
                //Remove UnitMembers for unit
                db.UnitMembers.RemoveRange(unitAndRelated.UnitMembers);
                //Remove SupportRelationships for unit
                db.SupportRelationships.RemoveRange(unitAndRelated.SupportRelationships);

                // Remove unit from the database.
                db.Units.Remove(unit);
                // save changes
                await db.SaveChangesAsync();
                return Pipeline.Success(true);
            }
        }
    }
}