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
	public class UnitMembersRepository : DataRepository
	{
		internal static Task<Result<List<UnitMember>, Error>> GetAll()
			=> ExecuteDbPipeline("get all unit members", async db =>
			{
				var result = await db.UnitMembers
					.Include(u => u.Unit)
					.Include(u => u.Unit.Parent)
					.Include(u => u.Person)
					.Include(u => u.MemberTools)
					.AsNoTracking()
					.ToListAsync();
				return Pipeline.Success(result);
			});

		public static Task<Result<UnitMember, Error>> GetOne(int id)
			=> ExecuteDbPipeline("get a membership by ID", db =>
				TryFindMembership(db, id));

		internal static async Task<Result<UnitMember, Error>> CreateMembership(UnitMemberRequest body)
			=> await ExecuteDbPipeline("create a membership", db =>
				ValidateRequest(db, body)
				.Bind(_ => FindOrCreatePerson(db, body))
				.Bind(person => TryCreateMembership(db, body, person))
				.Bind(created => TryFindMembership(db, created.Id))
			);

		internal static async Task<Result<UnitMember, Error>> UpdateMembership(HttpRequestData req, UnitMemberRequest body, int membershipId)
		{
			Person person = null;
			return await ExecuteDbPipeline($"update membership {membershipId}", db =>
				ValidateRequest(db, body, membershipId)
				.Bind(_ => FindOrCreatePerson(db, body))
				.Tap(existingPerson => person = existingPerson)
				.Bind(_ => TryFindMembership(db, membershipId))
				.Tap(existingRequest => LogPrevious(req, existingRequest))
				.Bind(existingRequest => TryUpdateMembership(db, existingRequest, body, person))
				.Bind(_ => TryFindMembership(db, membershipId))
			);
		}
		internal static async Task<Result<bool, Error>> DeleteMembership(HttpRequestData req, int membershipId)
		{
			return await ExecuteDbPipeline($"delete membership {membershipId}", db =>
				TryFindMembership(db, membershipId)
                .Tap(existing => LogPrevious(req, existing))
				.Bind(existing => TryDeleteMembership(db, req, existing))
			);
		}

		private static async Task<Result<UnitMember, Error>> TryFindMembership(PeopleContext db, int id)
		{
			var result = await db.UnitMembers
				.Include(u => u.Unit)
				.Include(u => u.Unit.Parent)
				.Include(u => u.Person)
				.Include(u => u.MemberTools)
				.SingleOrDefaultAsync(d => d.Id == id);
			return result == null
				? Pipeline.NotFound("No unit membership was found with the ID provided.")
				: Pipeline.Success(result);
		}

		private static async Task<Result<UnitMember, Error>> TryCreateMembership(PeopleContext db, UnitMemberRequest body, Person person)
		{
			var member = GetUpdatedUnitMember(new UnitMember(), body, person);
			db.UnitMembers.Add(member);
			await db.SaveChangesAsync();
			return Pipeline.Success(member);
		}

		private static async Task<Result<Person, Error>> FindOrCreatePerson(PeopleContext db, UnitMemberRequest body)
		{
			if (string.IsNullOrWhiteSpace(body.NetId) && body.PersonId.HasValue == false)
			{
				return Pipeline.Success<Person>(null);
			}
			
			var existing = await db.People.SingleOrDefaultAsync(p => p.Id == body.PersonId || p.Netid == body.NetId);
			if (existing != null)
			{
				return Pipeline.Success(existing);
			}

			var hrPerson = await db.HrPeople.SingleOrDefaultAsync(p => p.Netid == body.NetId);
			if (hrPerson == null)
			{
				var adResult = PeopleRepository.TryFindPersonWithAd(body.NetId);
				if(adResult.IsFailure)
				{
					return Pipeline.NotFound("The specified person does not exist in the HR API or Active Directory.");
				}

				hrPerson = adResult.Value;
			}

			var matchingDepartment = await db.Departments.SingleOrDefaultAsync(d => d.Name.Equals(hrPerson.HrDepartment));
			if (matchingDepartment == null)
			{
				return Pipeline.NotFound("The specified person's department does not exist in the directory.");
			}

			var newPerson = new Person
			{
				Netid = hrPerson.Netid,
				Name = hrPerson.Name,
				NameFirst = hrPerson.NameFirst,
				NameLast = hrPerson.NameLast,
				Position = hrPerson.Position,
				Campus = hrPerson.Campus,
				CampusPhone = hrPerson.CampusPhone,
				CampusEmail = hrPerson.CampusEmail,
				DepartmentId = matchingDepartment?.Id,
				Location = "",
				Expertise = "",
				Notes = body.Notes ?? "",
				PhotoUrl = ""
			};

			db.People.Add(newPerson);
			await db.SaveChangesAsync();
			return Pipeline.Success(newPerson);
		}

		private static async Task<Result<UnitMemberRequest, Error>> ValidateRequest(PeopleContext db, UnitMemberRequest body, int? existingRelationshipId = null)
		{
			if (await db.UnitMembers.AnyAsync(r => r.PersonId == body.PersonId && r.UnitId == body.UnitId && r.Id != existingRelationshipId))
			{
				return Pipeline.Conflict("The provided person is already a member of the provided unit.");
			}

			var unit = await db.Units.SingleAsync(u => u.Id == body.UnitId);
			if(unit.Active == false)
			{
				return Pipeline.BadRequest("The provided unit has been archived and is not available for new Unit Members.");
			}

			return Pipeline.Success(body);
		}
		private static async Task<Result<UnitMember, Error>> TryUpdateMembership(PeopleContext db, UnitMember existing, UnitMemberRequest body, Person person)
		{
			existing = GetUpdatedUnitMember(existing, body, person);
			await db.SaveChangesAsync();
			return Pipeline.Success(existing);
		}


		private static UnitMember GetUpdatedUnitMember(UnitMember unitMember, UnitMemberRequest body, Person person)
		{
			unitMember.UnitId = body.UnitId;
			unitMember.Role = body.Role;
			unitMember.Permissions = body.Permissions;
			unitMember.PersonId = person?.Id;
			unitMember.Title = body.Title;
			unitMember.Percentage = body.Percentage;
			unitMember.Notes = body.Notes ?? "";
			return unitMember;
		}
		private static async Task<Result<bool, Error>> TryDeleteMembership(PeopleContext db, HttpRequestData req, UnitMember unitMember)
		{
			db.MemberTools.RemoveRange(unitMember.MemberTools);
			db.UnitMembers.Remove(unitMember);
			await db.SaveChangesAsync();
			return Pipeline.Success(true);
		}
	}
}
