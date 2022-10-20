using System.Collections.Generic;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Models;
using Database;
using Microsoft.AspNetCore.Http;
using System.Linq;
using API.Functions;
using Models.Enums;
using System;

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
					.Include(r => r.SupportType)
					.Where(r => r.Unit.Active)
					.AsNoTracking()
					.ToListAsync();
				return Pipeline.Success(result);
			});

		public static Task<Result<SupportRelationship, Error>> GetOne(int id)
			=> ExecuteDbPipeline("get a support relationship by ID", db =>
				TryFindSupportRelationship(db, id));

		public static Task<Result<List<SsspSupportRelationshipResponse>, Error>> GetSssp(SsspSupportRelationshipParameters query)
			=> ExecuteDbPipeline("Get all support relationships in SSSP format", async db =>
			{
				// Get all the SupportRelationships that have a usable email
				// That means both units that have an email
				// or units that do not have an email, but have a Leader that has an email.
				
				// Materialize the SupportRelationships
				var supportRelationships = await db.SupportRelationships
					.Include(r => r.Department)
					.Include(r => r.Unit)
					.Include(r => r.Unit.UnitMembers).ThenInclude(r => r.Person)
					.Where(r => r.Unit.Active)
					.Where(r =>
						string.IsNullOrWhiteSpace(r.Unit.Email) == false
						|| r.Unit.UnitMembers.Any(um => um.Role == Role.Leader && string.IsNullOrWhiteSpace(um.Person.CampusEmail) == false))
					.AsNoTracking()
					.ToListAsync();

				// Massage them into SsspSupportRelationshipResponse
				var result = supportRelationships
					.SelectMany(sr => SsspSupportRelationshipResponse.FromSupportRelationship(sr))
					.Distinct(new SsspSupportRelationshipResponse.Comparer()) // Weed out duplicates
					.OrderBy(r => r.Dept)
					.ThenBy(r => r.ContactEmail)
					.ToList();
				
				// SSSP doesn't need a proper unique Id for all the relationshps, they just need a row identifier.
				var k = 1;
				foreach(var item in result)
				{
					item.Key = k;
					k++;
				}
				
				// If we were provided a department name to filter by do so
				// Arguably this should be up before we materialize the results
				// from the database, but this keeps the generated "keys" consistent
				// between requests. SSSP said that isn't important, but better safe than sorry.
				if(string.IsNullOrWhiteSpace(query.DepartmentName) == false)
				{
					result = result
						.Where(r => r.Dept == query.DepartmentName)
						.ToList();
				}

				return Pipeline.Success(result);
			});

		internal static async Task<Result<SupportRelationship, Error>> CreateSupportRelationship(SupportRelationshipRequest body, EntityPermissions perms, string requestorNetId)
			=> await ExecuteDbPipeline("create a support relationship", db =>
				ValidateRequest(db, body, perms, requestorNetId)
				.Bind(_ => TryCreateSupportRelationship(db, body))
				.Bind(created => TryFindSupportRelationship(db, created.Id))
				.Bind(created => TrySetDepartmentReportSupportingUnit(db, perms, requestorNetId, created))
			);

		internal static async Task<Result<bool, Error>> DeleteSupportRelationship(HttpRequest req, int relationshipId)
		{
			return await ExecuteDbPipeline($"delete support relationship {relationshipId}", db =>
				TryFindSupportRelationship(db, relationshipId)
                .Tap(existing => LogPrevious(req, existing))
				.Bind(existing => TryDeleteSupportRelationship(db, req, existing))
			);
		}
		
		// TODO - This all really belongs in validateRequest.
		private static async Task<Result<SupportRelationship, Error>> TrySetDepartmentReportSupportingUnit(PeopleContext db, EntityPermissions perms, string requestorNetId, SupportRelationship supportRelationship)
		{
			var allowed = await CanUpdateDepartmentReportSupportingUnit(db, perms, requestorNetId, supportRelationship.UnitId, supportRelationship.DepartmentId);

			switch(allowed)
			{
				case CanChangeReportSupportingUnit.Yes:
					throw new NotImplementedException("WIP: can change it.");
					break;
				case CanChangeReportSupportingUnit.MayRequest:
					throw new NotImplementedException("WIP: cannot change it.");
					break;
				default:
					return Pipeline.Forbidden();
			}
		}

		private static async Task<Result<SupportRelationship, Error>> TryFindSupportRelationship(PeopleContext db, int id)
		{
			var result = await db.SupportRelationships
				.Include(r => r.Unit)
				.Include(r => r.Department)
					.ThenInclude(d => d.ReportSupportingUnit)
				.Include(r => r.SupportType)
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
				DepartmentId = body.DepartmentId,
				SupportTypeId = body.SupportTypeId
			};

			db.SupportRelationships.Add(relationship);
			await db.SaveChangesAsync();
			return Pipeline.Success(relationship);
		}

		private enum CanChangeReportSupportingUnit
		{
			No = 0,
			InvalidUnit = 1,
			Yes = 2,
			MayRequest = 3
		}

		private static async Task<CanChangeReportSupportingUnit> CanUpdateDepartmentReportSupportingUnit(PeopleContext db, EntityPermissions perms, string requestorNetId, int unitId, int departmentId)
		{
			if (perms.HasFlag(EntityPermissions.Post))
			{
				var requestor = await db.People.SingleOrDefaultAsync(p => p.Netid == requestorNetId);

				if (requestor?.IsServiceAdmin == true)
				{
					return CanChangeReportSupportingUnit.Yes;
				}

				var departmentHasOtherRelationships = await db.SupportRelationships
					.Include(sr => sr.Unit)
					.Include(sr => sr.Department)
					.Where(sr => sr.DepartmentId == departmentId && sr.UnitId != unitId)
					.AnyAsync();
				
				// TODO: More checks about if the target Department.ReportSupportingUnit family tree is acceptable will go here.
				// return CanChangeReportSupportingUnit.InvalidUnit;

				return departmentHasOtherRelationships
					? CanChangeReportSupportingUnit.MayRequest
					: CanChangeReportSupportingUnit.Yes;
			}

			return CanChangeReportSupportingUnit.No;
		}

		private static async Task<Result<SupportRelationshipRequest, Error>> ValidateRequest(PeopleContext db, SupportRelationshipRequest body, EntityPermissions perms, string requestorNetId, int? existingRelationshipId = null)
		{
			if (body.UnitId == 0 || body.DepartmentId == 0 || body.SupportTypeId == 0)
			{
				return Pipeline.BadRequest("The request body was malformed, the unitId, departmentId, and/or supportTypeId field was missing or invalid.");
			}
			//Unit's existience is already confirmed in the Authorization step, but we must check if it is active.
			var unit = await db.Units.SingleAsync(u => u.Id == body.UnitId);
			if(unit.Active == false)
			{
				return Pipeline.BadRequest("The request body was malformed, the provided unit has been archived and is not available for new Support Relationships.");
			}
			
			if (await db.Departments.AnyAsync(d => d.Id == body.DepartmentId) == false)
			{
				return Pipeline.NotFound("The specified department does not exist.");
			}
			if (await db.SupportRelationships.AnyAsync(r => r.DepartmentId == body.DepartmentId && r.UnitId == body.UnitId && r.Id != existingRelationshipId))
			{
				return Pipeline.Conflict("The provided unit already has a support relationship with the provided department.");
			}
			if (body.SupportTypeId.HasValue && await db.SupportTypes.AnyAsync(t => t.Id == body.SupportTypeId) == false)
			{
				return Pipeline.NotFound("The specified support type does not exist.");
			}

			
			//Ensure the user has provided a ReportSupportingUnitId and has the permissions required to set the Department.ReportSupportingUnit they have provided.
			if(body.ReportSupportingUnitId == 0)
			{
				return Pipeline.BadRequest("The request body was malformed, the reportSupportingUnitId field was missing or invalid.");
			}
			var allowed = await CanUpdateDepartmentReportSupportingUnit(db, perms, requestorNetId, body.UnitId, body.DepartmentId);
			if(allowed == CanChangeReportSupportingUnit.No)
			{
				return Pipeline.BadRequest("You do have the permissions required to set the Department Report Supporting Unit you provided.");
			}
			if(allowed == CanChangeReportSupportingUnit.InvalidUnit)
			{
				return Pipeline.BadRequest("You may only set the Department Report Supporting Unit to your own unit or one of its parent units.");
			}

			return Pipeline.Success(body);
		}
		
		private static async Task<Result<bool, Error>> TryDeleteSupportRelationship(PeopleContext db, HttpRequest req, SupportRelationship supportRelationship)
		{
			db.SupportRelationships.Remove(supportRelationship);
			await db.SaveChangesAsync();
			return Pipeline.Success(true);
		}
	}
}
