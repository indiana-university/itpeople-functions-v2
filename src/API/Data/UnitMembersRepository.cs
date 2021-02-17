using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
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
			var member = new UnitMember
			{
				UnitId = body.UnitId,
				Role = body.Role,
				Permissions = body.Permissions,
				Person = person,
				Title = body.Title,
				Percentage = body.Percentage,
				Notes = body.Notes
			};
			db.UnitMembers.Add(member);
			await db.SaveChangesAsync();
			return Pipeline.Success(member);
		}

		private static async Task<Result<Person, Error>> FindOrCreatePerson(PeopleContext db, UnitMemberRequest body)
		{
			if (string.IsNullOrWhiteSpace(body.Netid) && body.PersonId.HasValue == false)
			{
				return Pipeline.Success<Person>(null);
			}
			var existing = await db.People.SingleOrDefaultAsync(p => p.Id == body.PersonId || p.Netid == body.Netid);
			if (existing != null)
			{
				return Pipeline.Success(existing);
			}
			var hrPerson = await db.HrPeople.SingleOrDefaultAsync(p => p.Netid == body.Netid);
			if (hrPerson != null)
			{
				var matchingDepartment = await db.Departments.SingleOrDefaultAsync(d => d.Name.Equals(hrPerson.HrDepartment));
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
					DepartmentId = matchingDepartment?.Id
				};

				db.People.Add(newPerson);
				await db.SaveChangesAsync();
				return Pipeline.Success(newPerson);
			}
			return Pipeline.NotFound("The specified unit and/or person does not exist.");

		}

		private static async Task<Result<UnitMemberRequest, Error>> ValidateRequest(PeopleContext db, UnitMemberRequest body, int? existingRelationshipId = null)
		{
			if (await db.UnitMembers.AnyAsync(r => r.PersonId == body.PersonId && r.UnitId == body.UnitId && r.Id != existingRelationshipId))
			{
				return Pipeline.Conflict("The provided person is already a member of the provided unit.");
			}
			return Pipeline.Success(body);
		}
	}
}