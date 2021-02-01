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

		
	}
}