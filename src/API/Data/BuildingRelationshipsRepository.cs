using System.Collections.Generic;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;

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


        internal static async Task<Result<BuildingRelationship, Error>> CreateBuildingRelationship(BuildingRelationshipRequest body)
            => await ExecuteDbPipeline("create a building relationship", db =>
                TryCreateBuildingRelationship(db, body)
                .Bind(created => TryFindBuildingRelationship(db, created.Id))
            );

		internal static async Task<Result<BuildingRelationship, Error>> UpdateBuildingRelationship(BuildingRelationshipRequest body, int relationshipId)
        {
            return await ExecuteDbPipeline($"update building relationship {relationshipId}", db =>
                TryFindBuildingRelationship(db, relationshipId)
                .Bind(existing => TryUpdateBuildingRelationship(db, existing, body))
                .Bind(_ => TryFindBuildingRelationship(db, relationshipId))
            );
        }

		internal static async Task<Result<bool, Error>> DeleteBuildingRelationship(int relationshipId)
        {
            return await ExecuteDbPipeline($"delete building relationship {relationshipId}", db =>
                TryFindBuildingRelationship(db, relationshipId)
                .Bind(existing => TryDeleteBuildingRelationship(db, existing))
            );
        }

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
        
        private static async Task<Result<BuildingRelationship,Error>> TryCreateBuildingRelationship (PeopleContext db, BuildingRelationshipRequest body)
        {
            var buildingRelationship = new BuildingRelationship{
                UnitId = body.UnitId,
                BuildingId = body.BuildingId
            };

            var createValidationResult = await GetCreateValidationResult(db,buildingRelationship);
            if(createValidationResult.IsFailure) {
                return createValidationResult;
            }

            db.BuildingRelationships.Add(buildingRelationship);
            await db.SaveChangesAsync();
            return Pipeline.Success(buildingRelationship);
        }

		private static async Task<Result<BuildingRelationship, Error>> GetCreateValidationResult(PeopleContext db, BuildingRelationship body)
		{
            if(body.UnitId == 0 || body.BuildingId == 0) 
            {
                return Pipeline.BadRequest("The request body was malformed, the unitId and/or buildingId field was missing.");
            }
            //Unit is already being checked/found in the Authorization step
			if (await db.Buildings.AnyAsync(b => b.Id == body.BuildingId) == false)
            {
                return Pipeline.NotFound("No building was found with the buildingId provided.");
            }
            if (await db.BuildingRelationships.AnyAsync(r => r.BuildingId == body.BuildingId && r.UnitId == body.UnitId))
            {
                return Pipeline.Conflict("The provided unit already has a support relationship with the provided building.");
            }
            return Pipeline.Success(body);
		}


        private static async Task<Result<BuildingRelationship, Error>> TryUpdateBuildingRelationship(PeopleContext db, BuildingRelationship existing, BuildingRelationshipRequest body)
        {
            existing.UnitId = body.UnitId;
            existing.BuildingId = body.BuildingId;

            var createValidationResult = await GetCreateValidationResult(db, existing);
            if(createValidationResult.IsFailure) {
                return createValidationResult;
            }

            await db.SaveChangesAsync();
            return Pipeline.Success(existing);
        }

        private static async Task<Result<bool, Error>> TryDeleteBuildingRelationship(PeopleContext db, BuildingRelationship buildingRelationship)
        {
            db.BuildingRelationships.Remove(buildingRelationship);
            await db.SaveChangesAsync();
            return Pipeline.Success(true);
        }

    }
}