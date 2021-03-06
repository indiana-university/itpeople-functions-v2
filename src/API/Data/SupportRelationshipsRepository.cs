using System.Collections.Generic;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Models;
using Database;
using Microsoft.AspNetCore.Http;

namespace API.Data
{
	public class SupportRelationshipsRepository : DataRepository
	{
		internal static Task<Result<List<SupportRelationship>, Error>> GetAll()
			=> ExecuteDbPipeline("search all support relationships", async db =>
			{
				var result = await db.SupportRelationships
					.Include(r => r.Unit)
					.Include(r => r.Department)
					.AsNoTracking()
					.ToListAsync();
				return Pipeline.Success(result);
			});

		public static Task<Result<SupportRelationship, Error>> GetOne(int id)
			=> ExecuteDbPipeline("get a support relationship by ID", db =>
				TryFindSupportRelationship(db, id));

		internal static async Task<Result<SupportRelationship, Error>> CreateSupportRelationship(SupportRelationshipRequest body)
			=> await ExecuteDbPipeline("create a support relationship", db =>
				ValidateRequest(db, body)
				.Bind(_ => TryCreateSupportRelationship(db, body))
				.Bind(created => TryFindSupportRelationship(db, created.Id))
			);

		internal static async Task<Result<SupportRelationship, Error>> UpdateSupportRelationship(HttpRequest req,SupportRelationshipRequest body, int relationshipId)
		{
			return await ExecuteDbPipeline($"update support relationship {relationshipId}", db =>
				ValidateRequest(db, body, relationshipId)
				.Bind(_ => TryFindSupportRelationship(db, relationshipId))
                .Tap(existing => LogPrevious(req, existing))
				.Bind(existing => TryUpdateSupportRelationship(db, existing, body))
				.Bind(_ => TryFindSupportRelationship(db, relationshipId))
			);
		}	
		internal static async Task<Result<bool, Error>> DeleteSupportRelationship(HttpRequest req, int relationshipId)
		{
			return await ExecuteDbPipeline($"delete support relationship {relationshipId}", db =>
				TryFindSupportRelationship(db, relationshipId)
                .Tap(existing => LogPrevious(req, existing))
				.Bind(existing => TryDeleteSupportRelationship(db, req, existing))
			);
		}

		private static async Task<Result<SupportRelationship, Error>> TryFindSupportRelationship(PeopleContext db, int id)
		{
			var result = await db.SupportRelationships
				.Include(r => r.Unit)
				.Include(r => r.Department)
				.SingleOrDefaultAsync(r => r.Id == id);
			return result == null
				? Pipeline.NotFound("No support relationship was found with the ID provided.")
				: Pipeline.Success(result);
		}
		private static async Task<Result<SupportRelationship, Error>> TryCreateSupportRelationship(PeopleContext db, SupportRelationshipRequest body)
		{
			var relationship = new SupportRelationship
			{
				UnitId = body.UnitId,
				DepartmentId = body.DepartmentId
			};

			db.SupportRelationships.Add(relationship);
			await db.SaveChangesAsync();
			return Pipeline.Success(relationship);
		}

		private static async Task<Result<SupportRelationshipRequest, Error>> ValidateRequest(PeopleContext db, SupportRelationshipRequest body, int? existingRelationshipId = null)
		{
			if (body.UnitId == 0 || body.DepartmentId == 0)
			{
				return Pipeline.BadRequest("The request body was malformed, the unitId and/or departmentId field was missing.");
			}
			//Unit is already being checked/found in the Authorization step
			if (await db.Departments.AnyAsync(d => d.Id == body.DepartmentId) == false)
			{
				return Pipeline.NotFound("The specified department does not exist.");
			}
			if (await db.SupportRelationships.AnyAsync(r => r.DepartmentId == body.DepartmentId && r.UnitId == body.UnitId && r.Id != existingRelationshipId))
			{
				return Pipeline.Conflict("The provided unit already has a support relationship with the provided department.");
			}
			return Pipeline.Success(body);
		}
		
		private static async Task<Result<SupportRelationship, Error>> TryUpdateSupportRelationship(PeopleContext db, SupportRelationship existing, SupportRelationshipRequest body)
		{
			existing.UnitId = body.UnitId;
			existing.DepartmentId = body.DepartmentId;

			await db.SaveChangesAsync();
			return Pipeline.Success(existing);
		}
		private static async Task<Result<bool, Error>> TryDeleteSupportRelationship(PeopleContext db, HttpRequest req, SupportRelationship supportRelationship)
		{
			db.SupportRelationships.Remove(supportRelationship);
			await db.SaveChangesAsync();
			return Pipeline.Success(true);
		}
	}
}