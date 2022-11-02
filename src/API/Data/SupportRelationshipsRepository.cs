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
		
		public static async Task<Result<List<UnitResponse>, Error>> GetValidReportSupportingUnits(string requestorNetId, int departmentId, int unitId)
		{
			return await ExecuteDbPipeline("Find the valid ReportSupportingUnits for departmentId.", async db =>
			{
				var validUnits = await TryGetValidReportSupportingUnits(db, requestorNetId, unitId, departmentId);
				var response = validUnits.Select(u => new UnitResponse(u)).ToList();
				return Pipeline.Success(response);
			});
		}

		internal static async Task<Result<SupportRelationship, Error>> CreateSupportRelationship(SupportRelationshipRequest body, EntityPermissions perms, string requestorNetId)
			=> await ExecuteDbPipeline("create a support relationship", db =>
				ValidateRequest(db, body, perms, requestorNetId)
				.Bind(_ => TryCreateSupportRelationship(db, perms, requestorNetId, body))
				.Bind(created => TryFindSupportRelationship(db, created.Id))
			);

		internal static async Task<Result<bool, Error>> DeleteSupportRelationship(HttpRequest req, string requestorNetId, int relationshipId)
		{
			return await ExecuteDbPipeline($"delete support relationship {relationshipId}", db =>
				TryFindSupportRelationship(db, relationshipId)
                .Tap(existing => LogPrevious(req, existing))
				.Bind(existing => TryDeleteSupportRelationship(db, req, requestorNetId, existing))
			);
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
		private static async Task<Result<SupportRelationship, Error>> TryCreateSupportRelationship(PeopleContext db, EntityPermissions perms, string requestorNetId, SupportRelationshipRequest body)
		{
			var relationship = new SupportRelationship
			{
				UnitId = body.UnitId,
				DepartmentId = body.DepartmentId,
				SupportTypeId = body.SupportTypeId
			};

			db.SupportRelationships.Add(relationship);

			// Get the full department, to update or generate a notification.
			var department = await db.Departments
				.Include(d => d.ReportSupportingUnit)
				.SingleAsync(d => d.Id == body.DepartmentId);

			var allowed = await CanUpdateDepartmentReportSupportingUnit(db, perms, requestorNetId, body.UnitId, body.DepartmentId, body.ReportSupportingUnitId);
			switch(allowed)
			{
				case CanChangeReportSupportingUnit.Yes:
					department.ReportSupportingUnitId = body.ReportSupportingUnitId;
					// TODO - Log the change to building before we do this.
					break;
				case CanChangeReportSupportingUnit.MayRequest:
					// Make a notification about the requested change.
					var reqReportUnit = await db.Units.SingleAsync(u => u.Id == body.ReportSupportingUnitId);
					var notification = new Notification
					{
						Message = $"{requestorNetId} requests to change {department.Name} Report Supporting Unit to {reqReportUnit.Name}."
					};

					await db.Notifications.AddAsync(notification);
					break;
				default:
					return Pipeline.Forbidden();
			}

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

		private static async Task<List<Unit>> TryGetValidReportSupportingUnits(PeopleContext db, string requestorNetId, int unitId, int departmentId)
		{
			var requestor = await db.People.SingleOrDefaultAsync(p => p.Netid == requestorNetId);

			var departmentOtherRelationships = await db.SupportRelationships
				.Include(sr => sr.Unit)
				.Include(sr => sr.Department)
				.Where(sr => sr.DepartmentId == departmentId && sr.UnitId != unitId)
				.ToListAsync();

			// Ensure the reportUnitId is in one of the department's SupportRelationship units' ancestry.
			var unitIdsToCheckAncestry = new List<int> { unitId };
			// Admins can set a ReportSupportingUnit based on ANY of the departments SupportRelationships
			if(requestor?.IsServiceAdmin == true)
			{
				unitIdsToCheckAncestry.AddRange(departmentOtherRelationships.Select(sr => sr.UnitId));
			}
			
			// Build a list of all the acceptable units this user can set.
			var acceptableUnits = new List<Unit>();
			foreach(var unitIdToCheck in unitIdsToCheckAncestry)
			{
				var familyTree = await AuthorizationRepository.BuildUnitTree(unitIdToCheck, db);
				acceptableUnits.AddRange(familyTree);
			}

			// If the department's current ReportSupportingUnit is not in acceptableUnits add it.
			var department = await db.Departments
				.Include(d => d.ReportSupportingUnit)
				.SingleAsync(d => d.Id == departmentId);
			
			if(department.ReportSupportingUnit != null && acceptableUnits.Any(u => u.Id == department.ReportSupportingUnit.Id) == false)
			{
				acceptableUnits.Add(department.ReportSupportingUnit);
			}

			return acceptableUnits;
		}

		private static async Task<CanChangeReportSupportingUnit> CanUpdateDepartmentReportSupportingUnit(PeopleContext db, EntityPermissions perms, string requestorNetId, int unitId, int departmentId, int reportUnitId)
		{
			if (perms.HasFlag(EntityPermissions.Post))
			{
				var requestor = await db.People.SingleOrDefaultAsync(p => p.Netid == requestorNetId);
				var departmentOtherRelationships = await db.SupportRelationships
					.Include(sr => sr.Unit)
					.Include(sr => sr.Department)
					.Where(sr => sr.DepartmentId == departmentId && sr.UnitId != unitId)
					.ToListAsync();

				var acceptableReportUnits = await TryGetValidReportSupportingUnits(db, requestorNetId, unitId, departmentId);
				var acceptableUnitIds = acceptableReportUnits.Select(u => u.Id).ToList();

				if(acceptableUnitIds.Contains(reportUnitId) == false)
				{
					return CanChangeReportSupportingUnit.InvalidUnit;
				}

				if (requestor?.IsServiceAdmin == true)
				{
					return CanChangeReportSupportingUnit.Yes;
				}

				// If it isn't changing the request is fine.
				var department = await db.Departments
					.Include(d => d.ReportSupportingUnit)
					.SingleAsync(d => d.Id == departmentId);
				if(department.ReportSupportingUnit?.Id == reportUnitId)
				{
					return CanChangeReportSupportingUnit.Yes;
				}

				// This user is a team leader, if there are no other SupportRelationships
				//  for the department they can set the ReportSupportingUnit.
				// If there are other SupportRelationships they can only request.
				return departmentOtherRelationships.Any()
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
			var allowed = await CanUpdateDepartmentReportSupportingUnit(db, perms, requestorNetId, body.UnitId, body.DepartmentId, body.ReportSupportingUnitId);
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
		
		private static async Task<Result<bool, Error>> TryDeleteSupportRelationship(PeopleContext db, HttpRequest req, string requestorNetId, SupportRelationship supportRelationship)
		{
			db.SupportRelationships.Remove(supportRelationship);

			// If the Department has no other SupportRelationships, set Department.ReportSupportingUnit to null.
			var otherSRs = await db.SupportRelationships
				.Include(sr => sr.Unit)
				.Include(sr => sr.Department)
				.Where(sr =>
					sr.Id != supportRelationship.Id
					&& sr.Department.Id == supportRelationship.Department.Id
				)
				.ToListAsync();
			
			var department = await db.Departments.SingleAsync(d => d.Id == supportRelationship.Department.Id);
			
			// if the DepartMent.ReportSupportingUnit is now not one of the Department's SupportUnit's Units(or their parents) we need to change the value
			var acceptableReportUnits = new List<Unit>();
			foreach(var sr in otherSRs)
			{
				var familyTree = await AuthorizationRepository.BuildUnitTree(sr.Unit.Id, db);
				acceptableReportUnits.AddRange(familyTree);
			}

			var reportUnitIsAcceptable = acceptableReportUnits.Any(a => a.Id == department.ReportSupportingUnit.Id);
			if(reportUnitIsAcceptable == false)
			{
				// TODO - Log this change to the Department.
				department.ReportSupportingUnit = null;
				// Generate a notification that Department no longer has a ReportSupportingUnit when action done by user that is not an admin.
				await db.Notifications.AddAsync(new Notification { Message = $"{requestorNetId} has removed Support Relationship between the unit {supportRelationship.Unit.Name} and department {supportRelationship.Department.Name}.  The department no longer has a Report Supporting Unit." });
			}

			await db.SaveChangesAsync();
			return Pipeline.Success(true);
		}
	}
}
