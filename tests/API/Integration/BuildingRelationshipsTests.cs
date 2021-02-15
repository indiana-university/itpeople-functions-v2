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
			public async Task BuildingRelationshipsGetAllHasCorrectNumber()
			{
				var resp = await GetAuthenticated("buildingRelationships");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<BuildingRelationship>>();
				Assert.AreEqual(2, actual.Count);
			}
		}

		public class GetOne : ApiTest
		{
			[TestCase(TestEntities.BuildingRelationships.CityHallCityOfPawneeId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task BuildingRelationshipsGetOneHasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"buildingRelationships/{id}");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task BuildingRelationshipsGetOneResponseHasCorrectShape()
			{
				var resp = await GetAuthenticated($"buildingRelationships/{TestEntities.BuildingRelationships.CityHallCityOfPawneeId}");
				var actual = await resp.Content.ReadAsAsync<BuildingRelationship>();
				var expected = TestEntities.BuildingRelationships.CityHallCityOfPawnee;
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.Building.Id, actual.Building.Id);
				Assert.AreEqual(expected.Unit.Id, actual.Unit.Id);
			}
		}

		public class BuildingRelationshipCreate : ApiTest
		{
			private BuildingRelationshipRequest CityHallParksAndRec = new BuildingRelationshipRequest
			{
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
				var req = new
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId
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
			public async Task BuildingRelationshipsUnauthorizedCannotCreate()
			{
				var resp = await PostAuthenticated("buildingRelationships", CityHallParksAndRec, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[TestCase(99999, TestEntities.Buildings.RonsCabinId, Description = "Unit Id not found")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, 99999, Description = "Building Id not found")]
			public async Task NotFoundCannotCreateBuildingRelationship(int unitId, int buildingId)
			{
				var req = new BuildingRelationshipRequest
				{
					UnitId = unitId,
					BuildingId = buildingId
				};
				var resp = await PostAuthenticated($"buildingRelationships", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
			}

			//409
			[Test]
			public async Task CannotCreateDuplicateBuildingRelationship()
			{
				var req = new BuildingRelationshipRequest
				{
					UnitId = TestEntities.BuildingRelationships.CityHallCityOfPawnee.UnitId,
					BuildingId = TestEntities.BuildingRelationships.CityHallCityOfPawnee.BuildingId
				};
				var resp = await PostAuthenticated("buildingRelationships", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.Conflict, actual.StatusCode);
				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains("The provided unit already has a support relationship with the provided building.", actual.Errors);
			}
		}

		public class BuildingRelationshipEdit : ApiTest
		{
			[TestCase(TestEntities.Buildings.SmallParkId, Description="Update with new Building Id and same Unit Id")]
			[TestCase(TestEntities.Buildings.CityHallId,Description="Update with same Building Id and same Unit Id")]
			public async Task UpdateCityHallCityOfPawnee(int buildingId)
			{
				var req = new BuildingRelationshipRequest
				{
					UnitId = TestEntities.BuildingRelationships.CityHallCityOfPawnee.UnitId,
					BuildingId = buildingId
				};

				var resp = await PutAuthenticated($"buildingRelationships/{TestEntities.BuildingRelationships.CityHallCityOfPawneeId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<BuildingRelationship>();

				Assert.AreEqual(TestEntities.BuildingRelationships.CityHallCityOfPawneeId, actual.Id);
				Assert.AreEqual(req.BuildingId, actual.Building.Id);
				Assert.AreEqual(req.UnitId, actual.Unit.Id);
			}

			[Test]
			public async Task BadRequestCannotUpdateWithMalformedBuildingRelationship()
			{
				var req = new
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId
				};

				var resp = await PutAuthenticated($"buildingRelationships/{TestEntities.BuildingRelationships.CityHallCityOfPawneeId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.BadRequest, actual.StatusCode);
				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains("The request body was malformed, the unitId and/or buildingId field was missing.", actual.Errors);
				Assert.AreEqual("(none)", actual.Details);
			}

			[Test]
			public async Task UnauthorizedCannotUpdateBuildingRelationship()
			{
				var req = new BuildingRelationshipRequest
				{
					UnitId = TestEntities.BuildingRelationships.CityHallCityOfPawnee.UnitId,
					BuildingId = TestEntities.Buildings.RonsCabin.Id
				};
				var resp = await PutAuthenticated($"buildingRelationships/{TestEntities.BuildingRelationships.CityHallCityOfPawneeId}", req, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[TestCase(TestEntities.BuildingRelationships.CityHallCityOfPawneeId, 99999, TestEntities.Buildings.RonsCabinId, Description = "Unit Id not found")]
			[TestCase(TestEntities.BuildingRelationships.CityHallCityOfPawneeId, TestEntities.Units.CityOfPawneeUnitId, 99999, Description = "Building Id not found")]
			[TestCase(99999, TestEntities.Units.CityOfPawneeUnitId, 99999, Description = "Building Relationship Id not found")]
			public async Task NotFoundCannotUpdateBuildingRelationship(int relationshipid, int unitId, int buildingId)
			{
				var req = new BuildingRelationshipRequest
				{
					UnitId = unitId,
					BuildingId = buildingId
				};
				var resp = await PutAuthenticated($"buildingRelationships/{relationshipid}", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
			}

			[Test]
			public async Task ConflictUpdateBuildingRelationship()
			{
				var req = new BuildingRelationshipRequest
				{
					UnitId = TestEntities.BuildingRelationships.CityHallCityOfPawnee.UnitId,
					BuildingId = TestEntities.BuildingRelationships.CityHallCityOfPawnee.BuildingId
				};

				var resp = await PutAuthenticated($"buildingRelationships/{TestEntities.BuildingRelationships.RonsCabinCityOfPawneeId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Conflict);
			}
		}

		public class BuildingRelationshipDelete : ApiTest
		{
			[TestCase(TestEntities.BuildingRelationships.CityHallCityOfPawneeId, ValidAdminJwt, HttpStatusCode.NoContent, Description = "Admin can delete a building relationship.")]
			[TestCase(TestEntities.BuildingRelationships.CityHallCityOfPawneeId, ValidRswansonJwt, HttpStatusCode.Forbidden, Description = "Non-Admin cannot delete a building relationship.")]
			[TestCase(9999, ValidAdminJwt, HttpStatusCode.NotFound, Description = "Cannot delete a building relationship that does not exist.")]
			public async Task CanDeleteBuildingRelationship(int relationshipId, string jwt, HttpStatusCode expectedCode)
			{
				var resp = await DeleteAuthenticated($"buildingRelationships/{relationshipId}", jwt);
				AssertStatusCode(resp, expectedCode);
			}
		}
	}
}