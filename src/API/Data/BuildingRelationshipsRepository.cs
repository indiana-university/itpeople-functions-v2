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
    public class BuildingRelationshipsRepository : DataRepository
    {
		internal static Task<Result<List<BuildingRelationship>, Error>> GetAll()
            => ExecuteDbPipeline("search all building support relationships", async db => {
                    var result = await db.BuildingRelationships
                        .AsNoTracking()
                        .ToListAsync();
                    return Pipeline.Success(result);
                });

        public static Task<Result<BuildingRelationship, Error>> GetOne(int id) 
            => ExecuteDbPipeline("get a building support relationship by ID", db => 
                TryFindBuildingRelationship(db, id));

        private static async Task<Result<BuildingRelationship,Error>> TryFindBuildingRelationship (PeopleContext db, int id)
        {
            var result = await db.BuildingRelationships
                .Include(r => r.Unit)
                .Include(r => r.Building)
                .SingleOrDefaultAsync(r => r.Id == id);
            return result == null
                ? Pipeline.NotFound("No building support relationship was found with the ID provided.")
                : Pipeline.Success(result);
        }

        internal static async Task<Result<BuildingRelationship, Error>> CreateBuildingRelationship(BuildingRelationshipRequest body)
            => await ExecuteDbPipeline("create a building relationship", db =>
                TryCreateBuildingRelationship(db, body)
                .Bind(created => TryFindBuildingRelationship(db, created.Id))
            );
        private static async Task<Result<BuildingRelationship,Error>> TryCreateBuildingRelationship (PeopleContext db, BuildingRelationshipRequest body)
        {
            var buildingRelationship = new BuildingRelationship{
                UnitId = body.UnitId,
                BuildingId = body.BuildingId
            };
            db.BuildingRelationships.Add(buildingRelationship);
            await db.SaveChangesAsync();
            return Pipeline.Success(buildingRelationship);
        }

    }
}