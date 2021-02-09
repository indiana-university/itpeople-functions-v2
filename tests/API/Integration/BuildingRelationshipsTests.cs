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

		public class BuildingRelationshipCreate : ApiTest
        {
            private BuildingRelationshipRequest CityHallParksAndRec = new BuildingRelationshipRequest {
				UnitId = TestEntities.Units.ParksAndRecUnitId,
				BuildingId = TestEntities.Buildings.CityHallId
			};

            //201 returns new unit
            [Test]
            public async Task CreateBuildingRelationship()
            {
				var resp = await PostAuthenticated("buildingRelationships", CityHallParksAndRec, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.Created);
                var actual = await resp.Content.ReadAsAsync<BuildingRelationship>();

                Assert.NotZero(actual.Id);
				Assert.AreEqual(CityHallParksAndRec.UnitId, actual.Unit.Id);
				Assert.AreEqual(CityHallParksAndRec.BuildingId, actual.Building.Id);
            }


            //400 The request body is malformed or missing
            [Test]
            public async Task CannotCreateMalformedBuildingRelationship()
            {
                var req = new {
					UnitId = 1
				};
                var resp = await PostAuthenticated("buildingRelationships", req, ValidAdminJwt);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual((int)HttpStatusCode.BadRequest, actual.StatusCode);
                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains("The request body was malformed, the unitId and/or buildingId field was missing.", actual.Errors);
                Assert.AreEqual("(none)", actual.Details);
            }

            //403 unauthorized
            [Test]
            public async Task UnauthorizedCannotCreate()
            {
                var resp = await PostAuthenticated("buildingRelationships", CityHallParksAndRec, ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.Forbidden);
            }

            //404 The specified unit parent does not exist
			[Test]
            public async Task NotFoundUnitIdCannotCreateBuildingRelationship()
            {
                var req = new {
					UnitId = 100
				};
                var resp = await PostAuthenticated("buildingRelationships", req, ValidAdminJwt);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
            }
			[Test]
            public async Task NotFoundBuildingIdCannotCreateBuildingRelationship()
            {
                var req = new {
					BuildingId = 100
				};
                var resp = await PostAuthenticated("buildingRelationships", req, ValidAdminJwt);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
            }

            //409
			[Test]
            public async Task CannotCreateDuplicateBuildingRelationship()
            {
                
                var resp = await PostAuthenticated("buildingRelationships", TestEntities.BuildingRelationships.CityHallCityOfPawnee, ValidAdminJwt);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual((int)HttpStatusCode.Conflict, actual.StatusCode);
             	Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains("The provided unit already has a support relationship with the provided building.", actual.Errors);
                
			}
        }
	}
}