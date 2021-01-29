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

        internal static async Task<Result<Unit, Error>> CreateUnit(UnitCreateRequest body)
		    => await ExecuteDbPipeline("create a unit", db =>
                (body.ParendId > 0 ? TryFindUnit(db, body.ParendId) : Task.FromResult(Pipeline.Success((Unit) null)))// 😬If body has parent try to fetch it, otherwise return a null parent.
                .Bind(parent => TryCreateUnit(db, body, parent)));

        private static async Task<Result<Unit,Error>> TryFindUnit (PeopleContext db, int id)
        {
            var unit = await db.Units
                .Include(u => u.Parent)
                .SingleOrDefaultAsync(p => p.Id == id);
            return unit == null
                ? Pipeline.NotFound("No unit found with that ID.")
                : Pipeline.Success(unit);
        }

        private static async Task<Result<Unit,Error>> TryCreateUnit (PeopleContext db, UnitCreateRequest body, Unit parent)
        {
            var unit = new Unit
            {
                Name = body.Name,
                Description = body.Description,
                Url = body.Url,
                Email = body.Email,
                Parent = parent
            };

            // add the unit
            db.Units.Add(unit);
            
            // save changes
            await db.SaveChangesAsync();
            return Pipeline.Success(unit);
        }
	}
}