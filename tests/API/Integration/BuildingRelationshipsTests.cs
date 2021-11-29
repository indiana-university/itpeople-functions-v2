using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Models;
using Models.Enums;
using NUnit.Framework;

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
				var actual = await resp.Content.ReadAsAsync<List<BuildingRelationshipResponse>>();
				Assert.AreEqual(3, actual.Count);
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
				var actual = await resp.Content.ReadAsAsync<BuildingRelationshipResponse>();
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

			//201 
			[Test]
			public async Task CreateBuildingRelationship()
			{
				var resp = await PostAuthenticated("buildingRelationships", CityHallParksAndRec, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Created);
				var actual = await resp.Content.ReadAsAsync<BuildingRelationshipResponse>();

				Assert.NotZero(actual.Id);
				Assert.AreEqual(CityHallParksAndRec.UnitId, actual.Unit.Id);
				Assert.AreEqual(CityHallParksAndRec.BuildingId, actual.Building.Id);
			}

			//201
			[Test]
			public async Task BuildingRelationshipsOwnerCanCreate()
			{
				var resp = await PostAuthenticated("buildingRelationships", CityHallParksAndRec, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Created);
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

			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, HttpStatusCode.Created, PermsGroups.All, Description = "Owner")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, HttpStatusCode.Created, PermsGroups.All, Description = "Owner Inheritted From Parent")]
			public async Task BuildingRelationshipPostEntityPermissions(int unitWithPermissions, UnitPermissions providedPermission, HttpStatusCode expectedCode, EntityPermissions expectedPermission)
			{
				var req = new BuildingRelationshipRequest
				{
					UnitId = TestEntities.Units.ParksAndRecUnitId,
					BuildingId = TestEntities.Buildings.RonsCabinId
				};
				await PostReturnsCorrectEntityPermissions("buildingRelationships", req, unitWithPermissions, providedPermission, expectedCode, expectedPermission);
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
				var actual = await resp.Content.ReadAsAsync<BuildingRelationshipResponse>();

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

			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, HttpStatusCode.Forbidden, PermsGroups.All, Description = "Owner")]
			public async Task TargetUnitCannotSeizeBuildingRelationship(int unitWithPermissions, UnitPermissions providedPermission, HttpStatusCode expectedCode, EntityPermissions expectedPermission)
			{
				// Trying to change the unit from "City of Pawnee"(the parent unit) to "Parks & Rec."
				var req = new BuildingRelationshipRequest
				{
					UnitId = TestEntities.Units.ParksAndRecUnitId,
					BuildingId = TestEntities.Buildings.RonsCabinId
				};
				await PutReturnsCorrectEntityPermissions($"buildingRelationships/{TestEntities.BuildingRelationships.RonsCabinCityOfPawneeId}", req, unitWithPermissions, providedPermission, expectedCode, expectedPermission);
			}
			
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, HttpStatusCode.OK, PermsGroups.All, Description = "Owner")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, HttpStatusCode.OK, PermsGroups.All, Description = "Owner Inheritted From Parent")]
			public async Task BuildingRelationshipPutEntityPermissions(int unitWithPermissions, UnitPermissions providedPermission, HttpStatusCode expectedCode, EntityPermissions expectedPermission)
			{
				// Add a building relationshp for Parks & Rec so we can test inheritted permissions
				var relationship = await GenerateParksAndRecCabinRelationship();
				var req = new BuildingRelationshipRequest
				{
					UnitId = relationship.UnitId,
					BuildingId = TestEntities.Buildings.CityHallId
				};
				await PutReturnsCorrectEntityPermissions($"buildingRelationships/{relationship.Id}", req, unitWithPermissions, providedPermission, expectedCode, expectedPermission);
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

			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, HttpStatusCode.NoContent, PermsGroups.All, Description = "Owner")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, HttpStatusCode.NoContent, PermsGroups.All, Description = "Owner Inheritted From Parent")]
			public async Task BuildingRelationshipDeleteEntityPermissions(int unitWithPermissions, UnitPermissions providedPermission, HttpStatusCode expectedCode, EntityPermissions expectedPermission)
			{
				// Add a building relationshp for Parks & Rec so we can test inheritted permissions
				var relationship = await GenerateParksAndRecCabinRelationship();

				await DeleteReturnsCorrectEntityPermissions($"buildingRelationships/{relationship.Id}", unitWithPermissions, providedPermission, expectedCode, expectedPermission);
			}
		}

		public static async Task<BuildingRelationship> GenerateParksAndRecCabinRelationship()
		{
			var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
			var relationship = new BuildingRelationship
			{
				BuildingId = TestEntities.Buildings.RonsCabinId,
				UnitId = TestEntities.Units.ParksAndRecUnitId
			};
			await db.BuildingRelationships.AddAsync(relationship);
			await db.SaveChangesAsync();

			return relationship;
		}
	}
}
