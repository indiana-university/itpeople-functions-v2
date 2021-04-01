using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Linq;

namespace Integration
{
	public class BuildingsTests
	{

		public class GetAll : ApiTest
		{
			[Test]
			public async Task HasCorrectNumber()
			{
				var resp = await GetAuthenticated("buildings");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Building>>();
				Assert.AreEqual(3, actual.Count);
			}

			[TestCase("Pawnee City hall", Description = "Name match")]
			[TestCase("City", Description = "Partial name match")]
			public async Task CanSearchByName(string name)
			{
				var resp = await GetAuthenticated($"buildings?q={name}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Building>>();
				Assert.AreEqual(1, actual.Count);
				Assert.AreEqual(TestEntities.Buildings.CityHall.Id, actual.Single().Id);
				Assert.AreEqual(TestEntities.Buildings.CityHall.Name, actual.Single().Name);
			}

			[TestCase("RC123", Description = "Code match")]
			[TestCase("RC", Description = "Partial code match")]
			[TestCase("RC-123", Description = "Code with optional dash match")]
			public async Task CanSearchByCode(string code)
			{
				var resp = await GetAuthenticated($"buildings?q={code}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Building>>();
				Assert.AreEqual(1, actual.Count);
				Assert.AreEqual(TestEntities.Buildings.RonsCabin.Id, actual.Single().Id);
				Assert.AreEqual(TestEntities.Buildings.RonsCabin.Code, actual.Single().Code);
			}

			[TestCase("321 main st", Description = "Address match")]
			[TestCase("321", Description = "Partial address match")]
			public async Task CanSearchByAddress(string address)
			{
				var resp = await GetAuthenticated($"buildings?q={address}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Building>>();
				Assert.AreEqual(1, actual.Count);
				Assert.AreEqual(TestEntities.Buildings.SmallPark.Id, actual.Single().Id);
				Assert.AreEqual(TestEntities.Buildings.SmallPark.Address, actual.Single().Address);
			}

			[Test]
			public async Task SearchReturnsNoResults()
			{
				var resp = await GetAuthenticated("buildings?q=foo");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Building>>();
				Assert.AreEqual(0, actual.Count);
			}
		}

		public class GetOne : ApiTest
		{
			[TestCase(TestEntities.Buildings.CityHallId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task HasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"buildings/{id}");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task ResponseHasCorrectBuildingShape()
			{
				var resp = await GetAuthenticated($"buildings/{TestEntities.Buildings.CityHallId}");
				var actual = await resp.Content.ReadAsAsync<Building>();
				var expected = TestEntities.Buildings.CityHall;
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.Name, actual.Name);
				Assert.AreEqual(expected.Code, actual.Code);
			}
		}
		public class GetSupportingUnits : ApiTest
		{
			[TestCase(TestEntities.Buildings.CityHallId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task CanGetSupportingUnits(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"buildings/{id}/supportingunits");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task GetCityHallSupportingUnits()
			{
				var resp = await GetAuthenticated($"buildings/{TestEntities.Buildings.CityHallId}/supportingunits");
				var actual = await resp.Content.ReadAsAsync<List<BuildingRelationship>>();
				var expected = new List<BuildingRelationship> {TestEntities.BuildingRelationships.CityHallCityOfPawnee};
				Assert.That(actual.Count, Is.EqualTo(1));
				Assert.That(actual.First().Id, Is.EqualTo(expected.First().Id));
				Assert.That(actual.First().Building.Id, Is.EqualTo(expected.First().Building.Id));
				Assert.That(actual.First().Unit.Id, Is.EqualTo(expected.First().Unit.Id));
			}
		}

	}
}