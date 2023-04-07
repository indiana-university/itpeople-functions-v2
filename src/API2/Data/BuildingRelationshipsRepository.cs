using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
	public class BuildingRelationshipsRepository : DataRepository
	{
		internal static Task<Result<List<BuildingRelationship>, Error>> GetAll()
			=> ExecuteDbPipeline("search all building support relationships", async db =>
			{
				var result = await db.BuildingRelationships
					.Include(br => br.Unit)
					.Include(br => br.Building)
					.AsNoTracking()
					.ToListAsync();
				return Pipeline.Success(result);
			});

		public static Task<Result<BuildingRelationship, Error>> GetOne(int id)
			=> ExecuteDbPipeline("get a building support relationship by ID", db =>
				TryFindBuildingRelationship(db, id));


		internal static async Task<Result<BuildingRelationship, Error>> CreateBuildingRelationship(BuildingRelationshipRequest body)
			=> await ExecuteDbPipeline("create a building relationship", db =>
				ValidateRequest(db, body)
				.Bind(_ => TryCreateBuildingRelationship(db, body))
				.Bind(created => TryFindBuildingRelationship(db, created.Id))
			);

		internal static async Task<Result<bool, Error>> DeleteBuildingRelationship(HttpRequest req, int relationshipId)
		{
			return await ExecuteDbPipeline($"delete building relationship {relationshipId}", db =>
				TryFindBuildingRelationship(db, relationshipId)
				.Tap(existing => LogPrevious(req, existing))
				.Bind(existing => TryDeleteBuildingRelationship(db, req, existing))
			);
		}

		internal static async Task<Result<bool, Error>> DeleteBuildingRelationship(HttpRequestData req, int relationshipId)
		{
			return await ExecuteDbPipeline($"delete building relationship {relationshipId}", db =>
				TryFindBuildingRelationship(db, relationshipId)
				.Tap(existing => LogPrevious(req, existing))
				.Bind(existing => TryDeleteBuildingRelationship(db, req, existing))
			);
		}

		private static async Task<Result<BuildingRelationship, Error>> TryFindBuildingRelationship(PeopleContext db, int id)
		{
			var result = await db.BuildingRelationships
				.Include(r => r.Unit)
				.Include(r => r.Building)
				.SingleOrDefaultAsync(r => r.Id == id);
			return result == null
				? Pipeline.NotFound("No building support relationship was found with the ID provided.")
				: Pipeline.Success(result);
		}

		private static async Task<Result<BuildingRelationship, Error>> TryCreateBuildingRelationship(PeopleContext db, BuildingRelationshipRequest body)
		{
			var buildingRelationship = new BuildingRelationship
			{
				UnitId = body.UnitId,
				BuildingId = body.BuildingId
			};

			db.BuildingRelationships.Add(buildingRelationship);
			await db.SaveChangesAsync();
			return Pipeline.Success(buildingRelationship);
		}

		private static async Task<Result<BuildingRelationshipRequest, Error>> ValidateRequest(PeopleContext db, BuildingRelationshipRequest body, int? existingRelationshipId = null)
		{
			if (body.UnitId == 0 || body.BuildingId == 0)
			{
				return Pipeline.BadRequest("The request body was malformed, the unitId and/or buildingId field was missing.");
			}
			//Unit is already being checked/found in the Authorization step
			if (await db.Buildings.AnyAsync(b => b.Id == body.BuildingId) == false)
			{
				return Pipeline.NotFound("The specified building does not exist.");
			}
			if (await db.BuildingRelationships.AnyAsync(r => r.BuildingId == body.BuildingId && r.UnitId == body.UnitId && r.Id != existingRelationshipId))
			{
				return Pipeline.Conflict("The provided unit already has a support relationship with the provided building.");
			}
			return Pipeline.Success(body);
		}

		private static async Task<Result<bool, Error>> TryDeleteBuildingRelationship(PeopleContext db, HttpRequest req, BuildingRelationship buildingRelationship)
		{
			db.BuildingRelationships.Remove(buildingRelationship);
			await db.SaveChangesAsync();
			return Pipeline.Success(true);
		}

		private static async Task<Result<bool, Error>> TryDeleteBuildingRelationship(PeopleContext db, HttpRequestData req, BuildingRelationship buildingRelationship)
		{
			db.BuildingRelationships.Remove(buildingRelationship);
			await db.SaveChangesAsync();
			return Pipeline.Success(true);
		}
	}
}