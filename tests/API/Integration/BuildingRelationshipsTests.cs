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
	public class BuildingRelationshipsTests
	{

		public class GetAll : ApiTest
		{
			[Test]
			public async Task HasCorrectNumber()
			{
				var resp = await GetAuthenticated("buildingRelationships");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<BuildingRelationship>>();
				Assert.AreEqual(1, actual.Count);
			}
		}
		
		public class GetOne : ApiTest
		{
			[TestCase(TestEntities.BuildingRelationships.CityHallCityOfPawneeId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task HasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"buildingRelationships/{id}");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task ResponseHasCorrectShape()
			{
				var resp = await GetAuthenticated($"buildingRelationships/{TestEntities.Buildings.CityHallId}");
				var actual = await resp.Content.ReadAsAsync<BuildingRelationship>();
				var expected = TestEntities.BuildingRelationships.CityHallCityOfPawnee;
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.Building.Id, actual.Building.Id);
				Assert.AreEqual(expected.Unit.Id, actual.Unit.Id);
			}
		}
	}
}