using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
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
                var expectedIds = new []{
                    TestEntities.UnitMembers.RSwansonLeaderId,
                    TestEntities.UnitMembers.LkNopeSubleadId,
                    TestEntities.UnitMembers.BWyattMemberId,
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
            public async Task HasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
            {
                var resp = await GetAuthenticated($"memberships/{id}");
                AssertStatusCode(resp, expectedStatus);
            }

            [Test]
            public async Task ResponseHasCorrectShape()
            {
                var resp = await GetAuthenticated($"memberships/{TestEntities.UnitMembers.RSwansonLeaderId}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<UnitMemberResponse>();
                var expected = TestEntities.UnitMembers.RSwansonDirector;
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.UnitId, actual.UnitId);
                Assert.AreEqual(expected.Role, actual.Role);
                Assert.AreEqual(expected.Permissions, actual.Permissions);
                Assert.AreEqual(expected.PersonId, actual.PersonId);
                Assert.AreEqual(expected.Title, actual.Title);
                Assert.AreEqual(expected.Percentage, actual.Percentage);
                Assert.AreEqual(expected.Notes, actual.Notes);
                // relations
                Assert.NotNull(actual.Person);
                Assert.AreEqual(expected.Person.Id, actual.Person.Id);
                Assert.NotNull(actual.Unit);
                Assert.AreEqual(expected.Unit.Id, actual.Unit.Id);
                Assert.NotNull(actual.MemberTools);
            }
        }

        public class UnitMemberCreate : ApiTest
		{
			//201 
			[TestCase(null, Description = "Create UnitMember with vacancy")]
			[TestCase(TestEntities.People.RSwansonId, Description = "Create UnitMember with existing person")]
			[TestCase(TestEntities.HrPeople.Tammy1Id, Description = "Create UnitMember with hr person")]
			public async Task CreatedUnitMembershipWithVacancy(int? personId)
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

			//403 unauthorized
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

			//409
			[Test]
			public async Task CannotCreateUnitMember()
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
    }
}