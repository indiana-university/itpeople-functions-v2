using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Linq;

namespace Integration
{
	public class DepartmentsTests
	{

		public class GetAll : ApiTest
		{
			[Test]
			public async Task HasCorrectNumber()
			{
				var resp = await GetAuthenticated("departments");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Department>>();
				Assert.AreEqual(3, actual.Count);
			}

			[TestCase("Parks Department", Description = "Name match")]
			[TestCase("Park", Description = "Partial name match")]
			public async Task CanSearchByName(string name)
			{
				var resp = await GetAuthenticated($"departments?q={name}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Department>>();
				Assert.AreEqual(1, actual.Count);
				Assert.AreEqual(TestEntities.Departments.Parks.Id, actual.Single().Id);
				Assert.AreEqual(TestEntities.Departments.Parks.Name, actual.Single().Name);
			}

			[TestCase("Your local Parks department.", Description = "Description match")]
			[TestCase("Your local Par", Description = "Partial Description match")]
			public async Task CanSearchByDescription(string code)
			{
				var resp = await GetAuthenticated($"departments?q={code}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Department>>();
				Assert.AreEqual(1, actual.Count);
				Assert.AreEqual(TestEntities.Departments.Parks.Id, actual.Single().Id);
				Assert.AreEqual(TestEntities.Departments.Parks.Description, actual.Single().Description);
			}

			[Test]
			public async Task SearchReturnsNoResults()
			{
				var resp = await GetAuthenticated("Departments?q=foo");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Department>>();
				Assert.AreEqual(0, actual.Count);
			}

			[TestCase("0", 3)]
			[TestCase("2", 2)]
			[TestCase("-1", 3)]
			[TestCase("twenty-five", 3)]
			public async Task KnowYourLimitations(string limit, int expectedRecords)
			{
				var resp = await GetAuthenticated($"Departments?_limit={limit}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Department>>();
				Assert.AreEqual(expectedRecords, actual.Count);
			}
		}
		public class GetOne : ApiTest
		{
			[TestCase(TestEntities.Departments.ParksId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task HasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"departments/{id}");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task ResponseHasCorrectShape()
			{
				var resp = await GetAuthenticated($"departments/{TestEntities.Departments.ParksId}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<Department>();
				var expected = TestEntities.Departments.Parks;
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.Name, actual.Name);
				Assert.AreEqual(expected.Description, actual.Description);
			}

            [Test]
            public async Task ReturnsBadRequestWhenDepartmentIdInvalid()
            {
                var resp = await GetAuthenticated("departments/invalid");
                AssertStatusCode(resp, HttpStatusCode.BadRequest);

                var issue = await resp.Content.ReadAsAsync<ApiError>();

                Assert.That(issue, Is.Not.Null);
                Assert.That(issue.Details, Is.EqualTo("(none)"));
                Assert.That(issue.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(issue.Errors.FirstOrDefault(), Is.EqualTo("Expected departmentId to be an integer value"));
            }
        }

		public class GetMemberUnits : ApiTest
		{
			[TestCase(TestEntities.Departments.ParksId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task CanGetSupportingUnits(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"departments/{id}/memberUnits");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task GetAuditorParksMemberUnits()
			{
				var resp = await GetAuthenticated($"departments/{TestEntities.Departments.Auditor.Id}/memberUnits");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Unit>>();
				var expected = new List<Unit> { TestEntities.Units.Auditor };
				Assert.That(actual.Count, Is.EqualTo(1));
				Assert.That(actual.First().Id, Is.EqualTo(expected.First().Id));
				Assert.That(actual.First().Parent.Id, Is.EqualTo(expected.First().Parent.Id));
			}

			[Test]
			public async Task GetsDistinctMemberUnits()
			{
				// Add Chris as a second member of the Auditor team.
				var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
				var chris = new Person
				{
					Netid = "ctraeger",
					Name = "Traeger, Chris",
					NameFirst = "Chris",
					NameLast = "Traeger",
					Position = "Sr. Auditor",
					Location = "Pawnee",
					Campus = "Pawnee",
					CampusPhone = "5-6789",
					CampusEmail = "ctraeger@auditz.com",
					Notes = "",
					PhotoUrl = "",
					Responsibilities = Responsibilities.None,
					DepartmentId = TestEntities.Departments.AuditorId,
					UnitMemberships = new List<UnitMember>()
				};
				
				var chrisAuditor = new UnitMember
				{
					UnitId = TestEntities.Units.AuditorId,
					Person = chris,
					Role = Role.Leader,
					Title = "Sr. Auditor",
					Percentage = 100,
					Notes = "Chris notes",
					MemberTools = new List<MemberTool>()
				};
				await db.UnitMembers.AddAsync(chrisAuditor);
				await db.SaveChangesAsync();

				var resp = await GetAuthenticated($"departments/{TestEntities.Departments.Auditor.Id}/memberUnits");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Unit>>();
				Assert.AreEqual(1, actual.Count);
				Assert.True(actual.Any(u => u.Id.Equals(TestEntities.Units.AuditorId)));
			}

            [Test]
            public async Task ReturnsBadRequestWhenDepartmentIdInvalid()
            {
                var resp = await GetAuthenticated("departments/invalid/memberUnits");
                AssertStatusCode(resp, HttpStatusCode.BadRequest);

                var issue = await resp.Content.ReadAsAsync<ApiError>();

                Assert.That(issue, Is.Not.Null);
                Assert.That(issue.Details, Is.EqualTo("(none)"));
                Assert.That(issue.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(issue.Errors.FirstOrDefault(), Is.EqualTo("Expected departmentId to be an integer value"));
            }
        }

		public class GetSupportingUnits : ApiTest
		{
			[TestCase(TestEntities.Departments.ParksId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task CanGetSupportingUnits(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"departments/{id}/supportingunits");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task GetParksSupportingUnits()
			{
				var resp = await GetAuthenticated($"departments/{TestEntities.Departments.ParksId}/supportingunits");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<SupportRelationship>>();
				var expected = new List<SupportRelationship> {TestEntities.SupportRelationships.ParksAndRecRelationship};
				Assert.That(actual.Count, Is.EqualTo(1));
				Assert.That(actual.First().Id, Is.EqualTo(expected.First().Id));
				Assert.That(actual.First().Department.Id, Is.EqualTo(expected.First().Department.Id));
				Assert.That(actual.First().SupportType.Id, Is.EqualTo(expected.First().SupportType.Id));
				Assert.That(actual.First().Unit.Id, Is.EqualTo(expected.First().Unit.Id));
			}

            [Test]
            public async Task ReturnsBadRequestWhenDepartmentIdInvalid()
            {
                var resp = await GetAuthenticated("departments/invalid/supportingUnits");
                AssertStatusCode(resp, HttpStatusCode.BadRequest);

                var issue = await resp.Content.ReadAsAsync<ApiError>();

                Assert.That(issue, Is.Not.Null);
                Assert.That(issue.Details, Is.EqualTo("(none)"));
                Assert.That(issue.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(issue.Errors.FirstOrDefault(), Is.EqualTo("Expected departmentId to be an integer value"));
            }
        }
	}
}