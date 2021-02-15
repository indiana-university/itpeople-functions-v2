using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Linq;
using API.Middleware;
using API.Data;
using Newtonsoft.Json;

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
				Assert.That(actual.First().Unit.Id, Is.EqualTo(expected.First().Unit.Id));
				Assert.That(expected.First().Unit.Parent.ParentId, Is.EqualTo(actual.First().Unit.Parent.ParentId));
			}
		}
	}
}