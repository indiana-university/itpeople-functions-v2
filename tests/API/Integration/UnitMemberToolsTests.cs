using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
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
					TestEntities.MemberTools.MemberTool1,
					TestEntities.MemberTools.AdminMemberToolId
				};
				AssertIdsMatchContent(expectedIds, actual);
			}
		}

		public class GetOneUnitMemberTool : ApiTest
		{
			[TestCase(TestEntities.MemberTools.MemberTool1, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task UnitMemberToolHasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"membertools/{id}");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task UnitMemberToolResponseHasCorrectShape()
			{
				var resp = await GetAuthenticated($"membertools/{TestEntities.MemberTools.AdminMemberToolId}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<MemberToolResponse>();
				var expected = TestEntities.MemberTools.AdminMemberTool;
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.MembershipId, actual.MembershipId);
				Assert.AreEqual(expected.ToolId, actual.ToolId);
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
					MembershipId = TestEntities.MemberTools.MemberTool1
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
			[TestCase(TestEntities.MemberTools.AdminMemberToolId, TestEntities.UnitMembers.LkNopeSubleadId, TestEntities.Tools.HammerId, Description = "Reassigned the admin hammer to the sub-lead UnitMember.")]
			[TestCase(TestEntities.MemberTools.MemberTool1, TestEntities.UnitMembers.RSwansonLeaderId, TestEntities.Tools.SawId, Description = "For RSwansonDirector Trade hammer for saw tool")]
			[TestCase(TestEntities.MemberTools.AdminMemberToolId, TestEntities.UnitMembers.LkNopeSubleadId, TestEntities.Tools.SawId, Description = "revise both membershipId and toolId of AdminMemberTool")]
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
					Id = TestEntities.MemberTools.AdminMemberToolId,
					MembershipId = TestEntities.UnitMembers.AdminMemberId,
					ToolId = TestEntities.Tools.SawId
				};
				var resp = await PutAuthenticated($"membertools/{TestEntities.MemberTools.MemberTool1}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.Contains("The memberToolId in the URL does not match the id in the request body.", actual.Errors);
			}
			/*
			[Test]
			public async Task BadRequestCannotUpdateWithMalformedUnitMember()
			{
				var req = new UnitMemberRequest
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId,
					Percentage = 101,
				};

				var resp = await PutAuthenticated($"membertools/{TestEntities.UnitMembers.RSwansonLeaderId}", req, ValidAdminJwt);
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
				var resp = await PutAuthenticated($"membertools/{TestEntities.UnitMembers.RSwansonLeaderId}", req, ValidRswansonJwt);
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
				var resp = await PutAuthenticated($"membertools/{membershipId}", req, ValidAdminJwt);
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

				var resp = await PutAuthenticated($"membertools/{TestEntities.UnitMembers.LkNopeSubleadId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Conflict);
			}
			*/
		}
	}
}