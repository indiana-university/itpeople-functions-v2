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
    public class UnitsRepository : DataRepository
    {
        public const string ParentNotFound = "The specified unit parent does not exist";
        public const string MalformedRequest = "The request body is malformed or missing";

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

        internal static async Task<Result<Unit, Error>> CreateUnit(Unit body)
            => await ExecuteDbPipeline("create a unit", db =>
                TrySetRequestedParent(db, body)
                .Bind(parent => TryCreateUnit(db, body, parent)));

        internal static async Task<Result<Unit, Error>> UpdateUnit(Unit body, int unitId)
        {
            return await ExecuteDbPipeline("update unit", db =>
                TrySetRequestedParent(db, body)
                .Bind(_ => TryFindUnit(db, unitId))//Make sure to use requested unitId, and not trust the provided body.
                .Bind(existing => TryUpdateUnit(db, existing, body))
            );
        }

        private static async Task<Result<Unit,Error>> TryFindUnit (PeopleContext db, int id)
        {
            var unit = await db.Units
                .Include(u => u.Parent)
                .SingleOrDefaultAsync(p => p.Id == id);
            return unit == null
                ? Pipeline.NotFound("No unit found with that ID.")
                : Pipeline.Success(unit);
        }

        private static async Task<Result<Unit,Error>> TrySetRequestedParent (PeopleContext db, Unit body)
        {
            if(body.ParentId.HasValue && body.ParentId != 0)
            {
                return await TryFindUnit(db, (int)body.ParentId)
                    .Tap(p => body.Parent = p);
            }
            else
            {
                body.Parent = null;
                return Pipeline.Success(body);
            }
        }

        private static async Task<Result<Unit,Error>> TryCreateUnit (PeopleContext db, Unit unit, Unit parent)
        {
            // Setup the parent relationship on the new Unit.
            unit.Parent = parent;

            // add the unit
            db.Units.Add(unit);
            
            // save changes
            await db.SaveChangesAsync();
            return Pipeline.Success(unit);
        }

        private static async Task<Result<Unit, Error>> TryUpdateUnit(PeopleContext db, Unit existing, Unit body)
		{
			existing.Name = body.Name;
            existing.Description = body.Description;
            existing.Url = body.Url;
            existing.Email = body.Email;
            existing.Parent = body.Parent;

            // TODO: Do we need to manually validate the model?  EF doesn't do that for free any more.
            await db.SaveChangesAsync();
            return Pipeline.Success(existing);
		}        
    }
}