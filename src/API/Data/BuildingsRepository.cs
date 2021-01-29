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

namespace API.Data
{
    public class BuildingsRepository : DataRepository
    {
		internal static Task<Result<List<Building>, Error>> GetAll(BuildingSearchParameters query)
            => ExecuteDbPipeline("search all buildings", async db => {
                    var queryNoDash = query?.Q?.Replace("-","");
                    var result = await db.Buildings.Where(b =>
                        EF.Functions.ILike(b.Address, $"%{query.Q}%")
                        || EF.Functions.ILike(b.Code, $"%{query.Q}%")
                        || EF.Functions.ILike(b.Code, $"%{queryNoDash}%")
                        || EF.Functions.ILike(b.Name, $"%{query.Q}%"))
                        .OrderBy(b => b.Name)
                        .Take(25)
                        .AsNoTracking()
                        .ToListAsync();
                    return Pipeline.Success(result);
                });

        public static Task<Result<Building, Error>> GetOne(int id) 
            => ExecuteDbPipeline("get a building by ID", db => 
                TryFindBuilding(db, id));

        
        public static Task<Result<List<BuildingRelationship>, Error>> GetSupportingUnits(int buildingId) 
            => ExecuteDbPipeline("fetch supporting units", db =>
                TryFindBuildingRelationships(db, buildingId)
                .Bind(buildingRelationship => Pipeline.Success(buildingRelationship)));

        private static async Task<Result<Building,Error>> TryFindBuilding (PeopleContext db, int id)
        {
            var result = await db.Buildings
                .SingleOrDefaultAsync(b => b.Id == id);
            return result == null
                ? Pipeline.NotFound("No building was found with the ID provided.")
                : Pipeline.Success(result);
        }

        private static async Task<Result<List<BuildingRelationship>,Error>> TryFindBuildingRelationships (PeopleContext db, int buildingId)
        {
            var result = await db.BuildingRelationships
                .Include(b => b.Building)
                .Include(b => b.Unit)
                .Where(b => b.BuildingId == buildingId)
                .AsNoTracking().ToListAsync();
            return result.Count == 0
                ? Pipeline.NotFound("No building relationships were found with the buildingId provided.")
                : Pipeline.Success(result);
        }
    }
}