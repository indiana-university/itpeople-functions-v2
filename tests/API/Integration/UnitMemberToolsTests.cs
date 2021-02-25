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
	[Category("UnitMembers")]
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
			/*
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
			*/
		}
	}
}