using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;
using NUnit.Framework;

namespace Integration
{
	[TestFixture]
	[Category("UnitMembers")]
	public class UnitMembersTests : ApiTest
	{
		public class GetAll : ApiTest
		{
			[Test]
			public async Task FetchesAllUnitMembers()
			{
				var resp = await GetAuthenticated("memberships");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<UnitMemberResponse>>();
				var expectedIds = new[]{
					TestEntities.UnitMembers.RSwansonLeaderId,
					TestEntities.UnitMembers.LkNopeSubleadId,
					TestEntities.UnitMembers.BWyattMemberId,
					TestEntities.UnitMembers.AdminMemberId,
				};
				AssertIdsMatchContent(expectedIds, actual);
			}

			[Test]
			public async Task FetchAllHasExpectedRelations()
			{
				var resp = await GetAuthenticated("memberships");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<UnitMemberResponse>>();
				var ron = actual.SingleOrDefault(a => a.Id == TestEntities.UnitMembers.RSwansonLeaderId);
				Assert.NotNull(ron.Person);
				Assert.NotNull(ron.Unit);
				Assert.NotNull(ron.Unit.Parent);
				Assert.NotNull(ron.MemberTools);
			}
		}

		public class GetOne : ApiTest
		{
			[TestCase(TestEntities.UnitMembers.RSwansonLeaderId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task UnitMemberHasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"memberships/{id}");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task UnitMemberResponseHasCorrectShape()
			{
				var resp = await GetAuthenticated($"memberships/{TestEntities.UnitMembers.LkNopeSubleadId}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<UnitMemberResponse>();
				var expected = TestEntities.UnitMembers.LkNopeSublead;
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.UnitId, actual.UnitId);
				Assert.AreEqual(expected.Role, actual.Role);
				Assert.AreEqual(expected.Permissions, actual.Permissions);
				Assert.AreEqual(expected.PersonId, actual.PersonId);
				Assert.AreEqual(expected.Title, actual.Title);
				Assert.AreEqual(expected.Percentage, actual.Percentage);
				Assert.AreEqual(expected.Notes, actual.Notes); //Notes are stripped on membership getters
				// relations
				Assert.NotNull(actual.Person);
				Assert.AreEqual(expected.Person.Id, actual.Person.Id);
				Assert.NotNull(actual.Unit);
				Assert.AreEqual(expected.Unit.Id, actual.Unit.Id);
				Assert.NotNull(actual.MemberTools);
			}

			[TestCase(ValidRswansonJwt, TestEntities.UnitMembers.RSwansonLeaderId, PermsGroups.All, TestName="Ron1", Description="As Ron (owner) I can manage Ron's membership")]
			[TestCase(ValidRswansonJwt, TestEntities.UnitMembers.LkNopeSubleadId, PermsGroups.All, TestName="Ron2", Description="As Ron (owner) I can manage Leslie's membership")]
			[TestCase(ValidRswansonJwt, TestEntities.UnitMembers.BWyattMemberId, EntityPermissions.Get, TestName="Ron3", Description="As Ron (owner) I can't manage Ben's membership (different unit)")]
			[TestCase(ValidLknopeJwt, TestEntities.UnitMembers.RSwansonLeaderId, EntityPermissions.Get, TestName="Les1", Description="As Leslie (viewer) I can't manage Ron's membership")]
			[TestCase(ValidLknopeJwt, TestEntities.UnitMembers.LkNopeSubleadId, EntityPermissions.Get, TestName="Les2", Description="As Leslie (viewer) I can't manage Leslies's membership")]
            [TestCase(ValidLknopeJwt, TestEntities.UnitMembers.BWyattMemberId, EntityPermissions.Get, TestName="Les3", Description="As Leslie (viewer) I can't manage Ben's membership")]
			[TestCase(ValidBwyattJwt, TestEntities.UnitMembers.BWyattMemberId, PermsGroups.All, TestName="Ben1", Description="As Ben (ManageMebers) I can manage Ben's membership")]
			[TestCase(ValidBwyattJwt, TestEntities.UnitMembers.RSwansonLeaderId, EntityPermissions.Get, TestName="Ben2", Description="As Ben (ManageMebers) I can't manage Ron's membership (different unit)")]
			[TestCase(ValidBwyattJwt, TestEntities.UnitMembers.LkNopeSubleadId,  EntityPermissions.Get, TestName="Ben3", Description="As Ben (ManageMebers) I can't manage Leslie's membership (different unit)")]
            [TestCase(ValidAdminJwt, TestEntities.UnitMembers.RSwansonLeaderId, PermsGroups.All, TestName="Adm1", Description="As a service admin I can do anything to any unit")]
            [TestCase(ValidAdminJwt, TestEntities.UnitMembers.LkNopeSubleadId, PermsGroups.All, TestName="Adm2", Description="As a service admin I can do anything to any unit")]
            [TestCase(ValidAdminJwt, TestEntities.UnitMembers.BWyattMemberId, PermsGroups.All, TestName="Adm3", Description="As a service admin I can do anything to any unit")]
            public async Task ResponseHasCorrectXUserPermissionsHeader(string jwt, int membershipId, EntityPermissions expectedPermissions)
            {
                var resp = await GetAuthenticated($"memberships/{membershipId}", jwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, expectedPermissions);
            }

			[Test]
			public async Task ResponseInheritsXUserPermissionsFromParentUnit()
			{
				// Add a person to City of Pawnee that has All permissions on "City of Pawnee," but NO permissions directly on "Parks & Rec."
				var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
				var chris =  new Person()
				{
					Netid = "ctraeger",
					Name = "Traeger, Chris",
					NameFirst = "Chris",
					NameLast = "Traeger",
					Position = "Sr. Auditor",
					Location = "",
					Campus = "Indianapolis",
					CampusPhone = "317.441.3333",
					CampusEmail = "ctraeger@pawnee.in.us",
					Expertise = "Fitness",
					Notes = "",
					PhotoUrl = "https://sasquatchbrewery.com/wp-content/uploads/2018/06/lil.jpg",
					Responsibilities = Responsibilities.ItProjectMgt,
					DepartmentId = TestEntities.Departments.Auditor.Id,
					IsServiceAdmin = false
				};
				await db.People.AddAsync(chris);

				// Add chris to City of Pawnee with All permissions
				var chrisCityMembership = new UnitMember()
				{
					Role = Role.Member,
					Permissions = UnitPermissions.ManageMembers,
					PersonId = chris.Id,
					Title = "Auditor",
					Percentage = 100,
					Notes = "notes about Chris",
					Person = chris,
					UnitId = TestEntities.Units.CityOfPawneeUnitId,
					MemberTools = null
				};
				await db.UnitMembers.AddAsync(chrisCityMembership);
				await db.SaveChangesAsync();

				var chrisJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiY3RyYWVnZXIiLCJleHAiOjIxNDc0ODM2NDh9.hj0v52e6c3Q-1A9EE9cJMJ8Hm4zrAbVb2PCwBSF7uS-RkbIC26RBdkllP5Rb2GDo32U8dGisdiXzFcqtMhuHFfootJz8JDCzgcA6_t6ibc3NFq9CkqqWcjjlSPckQ8HPSGCmVsNsXPY6hk_aZLtuPZ_YqdeaTEicKr_hyu7rehoJNhimoi2iVKqOo9VYHhenrfkVcniZmREflvNMlbFijUlozjjFQgkycwGRflnIyaUXg50w4z-8QZEqonvnRbrbfJB8AcYE65MWL4lpklKATP1eYI9uNW0Irxcv5Hh7LB1N6RVPFgSfNCGAjpR4c__5O2_ogFMe2iTqIj5ax8QE9A";

				// Get "parks & rec", they should have PermsGroups.All because "City of Pawnee" is "Parks & Rec's" parent.
				var resp = await GetAuthenticated($"memberships/{chrisCityMembership.Id}", chrisJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);

			}
		}

		public class UnitMemberCreate : ApiTest
		{
			[TestCase(null, Description = "Create UnitMember with vacancy")]
			[TestCase(TestEntities.People.RSwansonId, Description = "Create UnitMember with existing person")]
			[TestCase(TestEntities.HrPeople.Tammy1Id, Description = "Create UnitMember with hr person")]
			public async Task CreatedUnitMembership(int? personId)
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId,
					Role = Role.Member,
					Permissions = UnitPermissions.Viewer,
					PersonId = personId,
					Title = "Title",
					Percentage = 100,
					Notes = ""
				};
				var resp = await PostAuthenticated("memberships", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Created);
				var actual = await resp.Content.ReadAsAsync<UnitMemberResponse>();

				Assert.NotZero(actual.Id);
				Assert.AreEqual(req.UnitId, actual.Unit.Id);
				Assert.AreEqual(req.Role, actual.Role);
				Assert.AreEqual(req.Permissions, actual.Permissions);
				Assert.AreEqual(req.PersonId, actual.PersonId);
				Assert.AreEqual(req.Title, actual.Title);
				Assert.AreEqual(req.Percentage, actual.Percentage);
			}

			[Test]
			public async Task UnitMembersBadRequestCannotCreate()
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId,
					Percentage = 101,
				};
				var resp = await PostAuthenticated("memberships", req, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
			}

			[Test]
			public async Task UnitMembersUnauthorizedCannotCreate()
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId,
				};
				var resp = await PostAuthenticated("memberships", req, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[TestCase(99999, TestEntities.People.RSwansonId, Description = "Unit Id not found")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, 99999, Description = "Person does not exist")]
			public async Task NotFoundCannotCreateUnitMember(int unitId, int personId)
			{
				var req = new UnitMemberRequest
				{
					UnitId = unitId,
					PersonId = personId
				};
				var resp = await PostAuthenticated($"memberships", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
			}

			[Test]
			public async Task ConflictCannotCreateUnitMember()
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.Units.ParksAndRecUnitId,
					Role = Role.Member,
					Permissions = UnitPermissions.Viewer,
					PersonId = TestEntities.People.RSwansonId,
					Title = "Title",
					Percentage = 100,
					Notes = ""
				};
				var resp = await PostAuthenticated("memberships", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.Conflict, actual.StatusCode);
				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains("The provided person is already a member of the provided unit.", actual.Errors);
			}
		}

		public class UnitMembersEdit : ApiTest
		{
			[TestCase(null, TestEntities.Units.ParksAndRecUnitId, Description = "Update Unit Member with vacancy")]
			[TestCase(TestEntities.People.RSwansonId, TestEntities.Units.AuditorId, Description = "Update Unit Member with existing person")]
			[TestCase(TestEntities.HrPeople.Tammy1Id, TestEntities.Units.ParksAndRecUnitId, Description = "Update Unit Member with hr person")]
			public async Task UpdateUnitMembership(int? personId, int unitId)
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId,
					Role = Role.Member,
					Permissions = UnitPermissions.Viewer,
					PersonId = personId,
					Title = "Title",
					Percentage = 100,
					Notes = ""
				};
				var resp = await PutAuthenticated($"memberships/{TestEntities.UnitMembers.RSwansonLeaderId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<UnitMemberResponse>();

				Assert.NotZero(actual.Id);
				Assert.AreEqual(req.UnitId, actual.Unit.Id);
				Assert.AreEqual(req.Role, actual.Role);
				Assert.AreEqual(req.Permissions, actual.Permissions);
				Assert.AreEqual(req.PersonId, actual.PersonId);
				Assert.AreEqual(req.Title, actual.Title);
				Assert.AreEqual(req.Percentage, actual.Percentage);
			}

			[Test]
			public async Task BadRequestCannotUpdateWithMalformedUnitMember()
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId,
					Percentage = 101,
				};

				var resp = await PutAuthenticated($"memberships/{TestEntities.UnitMembers.RSwansonLeaderId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.BadRequest, actual.StatusCode);
				Assert.AreEqual(1, actual.Errors.Count);

			}

			[Test]
			public async Task UnauthorizedCannotUpdateUnitMember()
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId,
				};
				var resp = await PutAuthenticated($"memberships/{TestEntities.UnitMembers.RSwansonLeaderId}", req, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[TestCase(TestEntities.UnitMembers.RSwansonLeaderId, 99999, TestEntities.People.RSwansonId, Description = "Update Unit Id not found")]
			[TestCase(TestEntities.UnitMembers.RSwansonLeaderId, TestEntities.Units.CityOfPawneeUnitId, 99999, Description = "Update Person Id not found")]
			[TestCase(99999, TestEntities.Units.CityOfPawneeUnitId, TestEntities.People.RSwansonId, Description = "Update membership Id not found")]
			public async Task NotFoundCannotUpdateUnitMembers(int membershipId, int unitId, int personId)
			{
				var req = new UnitMemberRequest
				{
					UnitId = unitId,
					PersonId = personId
				};
				var resp = await PutAuthenticated($"memberships/{membershipId}", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
			}

			[Test]
			public async Task ConflictUpdateUnitMembership()
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.UnitMembers.RSwansonDirector.UnitId,
					PersonId = TestEntities.UnitMembers.RSwansonDirector.PersonId
				};

				var resp = await PutAuthenticated($"memberships/{TestEntities.UnitMembers.LkNopeSubleadId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Conflict);
			}
		}
		public class UnitMembersDelete : ApiTest
		{
			[TestCase(TestEntities.UnitMembers.RSwansonLeaderId, ValidAdminJwt, HttpStatusCode.NoContent, Description = "Admin can delete a membership")]
			[TestCase(TestEntities.UnitMembers.RSwansonLeaderId, ValidRswansonJwt, HttpStatusCode.NoContent, Description = "Owner can delete a membership")]
			[TestCase(TestEntities.UnitMembers.RSwansonLeaderId, ValidLknopeJwt, HttpStatusCode.Forbidden, Description = "Viewer cannot delete a membership")]
			[TestCase(9999, ValidAdminJwt, HttpStatusCode.NotFound, Description = "Cannot delete a membership that does not exist.")]
			public async Task CanDeleteUnitMember(int membershipId, string jwt, HttpStatusCode expectedCode)
			{
				var resp = await DeleteAuthenticated($"memberships/{membershipId}", jwt);
				AssertStatusCode(resp, expectedCode);
			}

			[Test]
			public async Task DeleteUnitMemberDoesNotCreateOrphans()
			{
				var resp = await DeleteAuthenticated($"memberships/{TestEntities.UnitMembers.RSwansonLeaderId}", ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.NoContent);
				
				var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
				
				Assert.IsEmpty(db.MemberTools.Where(mt => mt.MembershipId == TestEntities.UnitMembers.RSwansonLeaderId));
				Assert.IsEmpty(db.MemberTools.Where(mt => mt.Tool == null));
				Assert.IsEmpty(db.MemberTools.Where(mt => mt.UnitMember == null));
			}
		}
	}
}