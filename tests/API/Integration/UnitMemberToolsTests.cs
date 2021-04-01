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
	[Category("UnitMemberTools")]
	public class UnitMemberToolsTests : ApiTest
	{
		public class GetAllUnitMemberTools : ApiTest
		{
			[Test]
			public async Task FetchesAllUnitMemberTools()
			{
				var resp = await GetAuthenticated("membertools");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<MemberToolResponse>>();
				var expectedIds = new[]{
					TestEntities.MemberTools.RonHammerId,
					TestEntities.MemberTools.AdminHammerId
				};
				AssertIdsMatchContent(expectedIds, actual);
			}
		}

		public class GetOneUnitMemberTool : ApiTest
		{
			[TestCase(TestEntities.MemberTools.RonHammerId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task UnitMemberToolHasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"membertools/{id}");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task UnitMemberToolResponseHasCorrectShape()
			{
				var resp = await GetAuthenticated($"membertools/{TestEntities.MemberTools.AdminHammerId}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<MemberToolResponse>();
				var expected = TestEntities.MemberTools.AdminMemberTool;
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.MembershipId, actual.MembershipId);
				Assert.AreEqual(expected.ToolId, actual.ToolId);
			}

			[TestCase(ValidRswansonJwt, TestEntities.MemberTools.RonHammerId, PermsGroups.All, TestName="Ron1", Description="As Ron (owner) I can manage Ron's tools")]
			[TestCase(ValidRswansonJwt, TestEntities.MemberTools.AdminHammerId, EntityPermissions.Get, TestName="Ron2", Description="As Ron (owner) I can manage Leslie's tools")]
			[TestCase(ValidLknopeJwt, TestEntities.MemberTools.RonHammerId, EntityPermissions.Get, TestName="Les1", Description="As Leslie (viewer) I can't manage Ron's tools")]
			[TestCase(ValidLknopeJwt, TestEntities.MemberTools.AdminHammerId, EntityPermissions.Get, TestName="Les2", Description="As Leslie (viewer) I can't manage Leslies's tools")]
			[TestCase(ValidBwyattJwt, TestEntities.MemberTools.RonHammerId, EntityPermissions.Get, TestName="Ben1", Description="As Ben (ManageMebers) I can't manage Ben's tools")]
			[TestCase(ValidBwyattJwt, TestEntities.MemberTools.AdminHammerId, EntityPermissions.Get, TestName="Ben2", Description="As Ben (ManageMebers) I can't manage Ron's tools (different unit)")]
            [TestCase(ValidAdminJwt, TestEntities.MemberTools.RonHammerId, PermsGroups.All, TestName="Adm1", Description="As a service admin I can do anything to any unit")]
            [TestCase(ValidAdminJwt, TestEntities.MemberTools.AdminHammerId, PermsGroups.All, TestName="Adm2", Description="As a service admin I can do anything to any unit")]
            public async Task ResponseHasCorrectXUserPermissionsHeader(string jwt, int memberToolsId, EntityPermissions expectedPermissions)
            {
                var resp = await GetAuthenticated($"membertools/{memberToolsId}", jwt);
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
					Permissions = UnitPermissions.ManageTools,
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

				// Get "parks & rec", they should have PermsGroups.All because "City of Pawnee" is "Parks & Rec's" parent.
				var resp = await GetAuthenticated($"membertools/{TestEntities.MemberTools.RonHammerId}", ValidCtraegerJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);

			}

			[Test]
			public async Task XUserPermissionsLimitedToChildUnits()
			{
				// Add "Softball League" Unit under Parks & Rec
				var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
				var softball = new Unit("Softball League", "description", "", "", TestEntities.Units.ParksAndRecUnitId);
				await db.Units.AddAsync(softball);
				// Add Chris as the owner for "Softball League", but with no permissions for the parent units.
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
				
				var chrisSoftballOwner = new UnitMember
				{
					Role = Role.Member,
					Permissions = UnitPermissions.Owner,
					Unit = softball,
					Person = chris,
					Title = "Auditor",
					Percentage = 100,
					Notes = "notes about Chris",
					MemberTools = null
				};
				await db.UnitMembers.AddAsync(chrisSoftballOwner);
				await db.SaveChangesAsync();

				// For a parks & rec tool chris should have "Get"
				var resp = await GetAuthenticated($"membertools/{TestEntities.MemberTools.RonHammerId}", ValidCtraegerJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, EntityPermissions.Get);
				
				// For a city too Chris should have "Get"
				resp = await GetAuthenticated($"membertools/{TestEntities.MemberTools.AdminHammerId}", ValidCtraegerJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, EntityPermissions.Get);
			}
		}

		public class UnitMemberToolsCreate : ApiTest
		{
			[TestCase(TestEntities.UnitMembers.BWyattMemberId, Description = "Give Wyatt the hammer.")]
			[TestCase(TestEntities.UnitMembers.LkNopeSubleadId, Description = "Give Sublead the hammer.")]
			public async Task CreatedUnitMemberTool(int membershipId)
			{
				var req = new MemberToolRequest
				{
					MembershipId = membershipId,
					ToolId = TestEntities.Tools.HammerId
				};
				var resp = await PostAuthenticated("membertools", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Created);
				var actual = await resp.Content.ReadAsAsync<MemberToolResponse>();

				Assert.NotZero(actual.Id);
				Assert.AreEqual(req.MembershipId, actual.MembershipId);
				Assert.AreEqual(req.ToolId, actual.ToolId);
			}
			
			[Test]
			public async Task UnitMemberToolsBadRequestCannotCreate()
			{
				var req = new MemberToolRequest
				{
					MembershipId = TestEntities.MemberTools.RonHammerId
				};
				var resp = await PostAuthenticated("membertools", req, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
			}
			
			[Test]
			public async Task UnitMemberToolsUnauthorizedCannotCreate()
			{
				var req = new MemberToolRequest
				{
					MembershipId = TestEntities.UnitMembers.LkNopeSubleadId,
					ToolId = TestEntities.Tools.HammerId
				};
				var resp = await PostAuthenticated("membertools", req, ValidLknopeJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}
			
			[TestCase(99999, TestEntities.Tools.HammerId, "The specified member does not exist.")]
			[TestCase(TestEntities.UnitMembers.BWyattMemberId, 99999, "The specified tool does not exist.")]
			public async Task NotFoundCannotCreateUnitMemberTool(int membershipId, int toolId, string expectedError)
			{
				var req = new MemberToolRequest
				{
					MembershipId = membershipId,
					ToolId = toolId
				};
				var resp = await PostAuthenticated($"membertools", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.NotFound);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.Contains(expectedError, actual.Errors);
			}
			
			[Test]
			public async Task ConflictCannotCreateUnitMemberTool()
			{
				var req = new MemberToolRequest
				{
					MembershipId = TestEntities.MemberTools.MemberTool.MembershipId,
					ToolId = TestEntities.MemberTools.MemberTool.ToolId
				};
				var resp = await PostAuthenticated("membertools", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.Conflict, actual.StatusCode);
				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains("The provided member already has access to the provided tool.", actual.Errors);
			}
		}

		public class UnitMemberToolsEdit : ApiTest
		{
			[TestCase(TestEntities.MemberTools.AdminHammerId, TestEntities.UnitMembers.LkNopeSubleadId, TestEntities.Tools.HammerId, Description = "Reassigned the admin hammer to the sub-lead UnitMember.")]
			[TestCase(TestEntities.MemberTools.RonHammerId, TestEntities.UnitMembers.RSwansonLeaderId, TestEntities.Tools.SawId, Description = "For RSwansonDirector Trade hammer for saw tool")]
			[TestCase(TestEntities.MemberTools.AdminHammerId, TestEntities.UnitMembers.LkNopeSubleadId, TestEntities.Tools.SawId, Description = "revise both membershipId and toolId of AdminMemberTool")]
			public async Task UpdateUnitMemberTool(int id, int membershipId, int toolId)
			{
				var req = new MemberToolRequest
				{
					Id = id,
					MembershipId = membershipId,
					ToolId = toolId
				};
				var resp = await PutAuthenticated($"membertools/{id}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<MemberToolResponse>();

				Assert.AreEqual(req.Id, actual.Id);
				Assert.AreEqual(req.MembershipId, actual.MembershipId);
				Assert.AreEqual(req.ToolId, actual.ToolId);
			}

			[Test(Description = "The MemberToolId in the URL does not match the Id in the body.")]
			public async Task UpdateUnitMemberToolUrlAndBodyMismatch()
			{
				var req = new MemberToolRequest
				{
					Id = TestEntities.MemberTools.AdminHammerId,
					MembershipId = TestEntities.UnitMembers.AdminMemberId,
					ToolId = TestEntities.Tools.SawId
				};
				var resp = await PutAuthenticated($"membertools/{TestEntities.MemberTools.RonHammerId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.Contains("The memberToolId in the URL does not match the id in the request body.", actual.Errors);
			}
			
			[Test]
			public async Task BadRequestCannotUpdateWithMalformedUnitMemberTool()
			{
				var req = new MemberToolRequest
				{
					ToolId = TestEntities.Tools.HammerId
				};

				var resp = await PutAuthenticated($"membertools/{TestEntities.UnitMembers.RSwansonLeaderId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual(1, actual.Errors.Count);
			}
			
			[Test]
			public async Task UnauthorizedCannotUpdateUnitMemberTool()
			{
				var req = new MemberToolRequest
				{
					MembershipId = TestEntities.UnitMembers.RSwansonLeaderId,
					ToolId = TestEntities.Tools.HammerId
				};
				var resp = await PutAuthenticated($"membertools/{TestEntities.MemberTools.RonHammerId}", req, ValidLknopeJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[TestCase(TestEntities.MemberTools.AdminHammerId, 99999, TestEntities.Tools.SawId, "The specified member does not exist.")]
			[TestCase(TestEntities.MemberTools.AdminHammerId, TestEntities.UnitMembers.RSwansonLeaderId, 99999, "The specified tool does not exist.")]
			[TestCase(99999, TestEntities.UnitMembers.RSwansonLeaderId, TestEntities.Tools.SawId, "The specified member/tool does not exist.")]
			public async Task NotFoundCannotUpdateUnitMemberTools(int id, int membershipId, int toolId, string expectedError)
			{
				var req = new MemberToolRequest
				{
					Id = id,
					MembershipId = membershipId,
					ToolId = toolId
				};
				var resp = await PutAuthenticated($"membertools/{id}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.NotFound);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.Contains(expectedError, actual.Errors);
			}
			
			[Test]
			public async Task ConflictUpdateUnitMemberTool()
			{
				var req = new MemberToolRequest
				{
					Id = TestEntities.MemberTools.RonHammerId,
					MembershipId = TestEntities.UnitMembers.AdminMemberId,
					ToolId = TestEntities.Tools.HammerId
				};

				var resp = await PutAuthenticated($"membertools/{TestEntities.MemberTools.RonHammerId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Conflict);
			}
		}

		public class UnitMemberToolsDelete : ApiTest
		{
			[TestCase(TestEntities.MemberTools.RonHammerId, ValidAdminJwt, HttpStatusCode.NoContent, Description = "Admin can delete a MemberTool")]
			[TestCase(TestEntities.MemberTools.RonHammerId, ValidRswansonJwt, HttpStatusCode.NoContent, Description = "Unit Owner can delete a MemberTool for that Unit")]
			[TestCase(TestEntities.MemberTools.RonHammerId, ValidLknopeJwt, HttpStatusCode.Forbidden, Description = "Unit Viewer cannot delete a MemberTool for that Unit")]
			[TestCase(9999, ValidAdminJwt, HttpStatusCode.NotFound, Description = "Cannot delete a MemberTool that does not exist.")]
			public async Task CanDeleteUnitMemberTool(int memberToolId, string jwt, HttpStatusCode expectedCode)
			{
				var resp = await DeleteAuthenticated($"membertools/{memberToolId}", jwt);
				AssertStatusCode(resp, expectedCode);
			}
			/*
			[Test]
			public async Task DeleteUnitMemberToolDoesNotCreateOrphans()
			{
				var resp = await DeleteAuthenticated($"membertools/{TestEntities.UnitMembers.RSwansonLeaderId}", ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.NoContent);
				
				var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
				
				Assert.IsEmpty(db.MemberTools.Where(mt => mt.MembershipId == TestEntities.UnitMembers.RSwansonLeaderId));
				Assert.IsEmpty(db.MemberTools.Where(mt => mt.Tool == null));
				Assert.IsEmpty(db.MemberTools.Where(mt => mt.UnitMember == null));
			}
			*/
		}
	}
}