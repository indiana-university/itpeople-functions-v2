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
using System;

namespace Integration
{
	public class SupportRelationshipsTests
	{

		public class GetAll : ApiTest
		{
			[Test]
			public async Task SupportRelationshipsGetAllHasCorrectNumber()
			{
				var resp = await GetAuthenticated("supportRelationships");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<SupportRelationshipResponse>>();
				Assert.AreEqual(2, actual.Count);
			}
		}
		public class GetOne : ApiTest
		{
			[TestCase(TestEntities.SupportRelationships.ParksAndRecRelationshipId, HttpStatusCode.OK)]
			[TestCase(9999, HttpStatusCode.NotFound)]
			public async Task SupportRelationshipsGetOneHasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
			{
				var resp = await GetAuthenticated($"supportRelationships/{id}");
				AssertStatusCode(resp, expectedStatus);
			}

			[Test]
			public async Task SupportRelationshipsGetOneResponseHasCorrectShape()
			{
				var resp = await GetAuthenticated($"supportRelationships/{TestEntities.SupportRelationships.ParksAndRecRelationshipId}");
				var actual = await resp.Content.ReadAsAsync<SupportRelationshipResponse>();
				var expected = TestEntities.SupportRelationships.ParksAndRecRelationship;
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.Department.Id, actual.Department.Id);
				Assert.AreEqual(expected.Unit.Id, actual.Unit.Id);
			}
		}

		public class SupportRelationshipCreate : ApiTest
		{
			private SupportRelationshipRequest FireAuditor = new SupportRelationshipRequest
			{
				UnitId = TestEntities.Units.Auditor.Id,
				DepartmentId = TestEntities.Departments.Fire.Id
			};

			//201 
			[Test]
			public async Task CreatedSupportRelationship()
			{
				var resp = await PostAuthenticated("supportRelationships", FireAuditor, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Created);
				var actual = await resp.Content.ReadAsAsync<SupportRelationship>();

				Assert.NotZero(actual.Id);
				Assert.AreEqual(FireAuditor.UnitId, actual.Unit.Id);
				Assert.AreEqual(FireAuditor.DepartmentId, actual.Department.Id);
			}


			//400 The request body is malformed or missing
			[Test]
			public async Task CannotCreateMalformedSupportRelationship()
			{
				var req = new
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId
				};
				var resp = await PostAuthenticated("supportRelationships", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.BadRequest, actual.StatusCode);
				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains("The request body was malformed, the unitId and/or departmentId field was missing.", actual.Errors);
				Assert.AreEqual("(none)", actual.Details);
			}

			//403 unauthorized
			[Test]
			public async Task SupportRelationshipsUnauthorizedCannotCreate()
			{
				var resp = await PostAuthenticated("supportRelationships", FireAuditor, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[TestCase(99999, TestEntities.Departments.FireId, Description = "Unit Id not found")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, 99999, Description = "Department Id not found")]
			public async Task NotFoundCannotCreateSupportRelationship(int unitId, int departmentId)
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = unitId,
					DepartmentId = departmentId
				};
				var resp = await PostAuthenticated($"supportRelationships", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
			}

			//409
			[Test]
			public async Task CannotCreateDuplicateSupportRelationship()
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = TestEntities.SupportRelationships.ParksAndRecRelationship.UnitId,
					DepartmentId = TestEntities.SupportRelationships.ParksAndRecRelationship.DepartmentId
				};
				var resp = await PostAuthenticated("supportRelationships", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.Conflict, actual.StatusCode);
				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains("The provided unit already has a support relationship with the provided department.", actual.Errors);
			}
		}
	
		public class SupportRelationshipEdit : ApiTest
		{
			[TestCase(TestEntities.Departments.AuditorId, Description="Update with new Department Id and same Unit Id")]
			[TestCase(TestEntities.Departments.ParksId,Description="Update with same Department Id and same Unit Id")]
			public async Task UpdateParksAndRecRelationship(int departmentId)
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = TestEntities.SupportRelationships.ParksAndRecRelationship.UnitId,
					DepartmentId = departmentId
				};

				var resp = await PutAuthenticated($"supportRelationships/{TestEntities.SupportRelationships.ParksAndRecRelationshipId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<SupportRelationshipResponse>();

				Assert.AreEqual(TestEntities.SupportRelationships.ParksAndRecRelationshipId, actual.Id);
				Assert.AreEqual(req.DepartmentId, actual.Department.Id);
				Assert.AreEqual(req.UnitId, actual.Unit.Id);
			}

			[Test]
			public async Task BadRequestCannotUpdateWithMalformedSupportRelationship()
			{
				var req = new
				{
					UnitId = TestEntities.Units.CityOfPawneeUnitId
				};

				var resp = await PutAuthenticated($"supportRelationships/{TestEntities.SupportRelationships.ParksAndRecRelationshipId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.BadRequest, actual.StatusCode);
				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains("The request body was malformed, the unitId and/or departmentId field was missing.", actual.Errors);
				Assert.AreEqual("(none)", actual.Details);
			}

			[Test]
			public async Task UnauthorizedCannotUpdateSupportRelationship()
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = TestEntities.Units.Auditor.Id,
					DepartmentId = TestEntities.Departments.FireId
				};
				var resp = await PutAuthenticated($"supportRelationships/{TestEntities.SupportRelationships.ParksAndRecRelationshipId}", req, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[TestCase(TestEntities.SupportRelationships.ParksAndRecRelationshipId, 99999, TestEntities.Departments.FireId, Description = "Unit Id not found")]
			[TestCase(TestEntities.SupportRelationships.ParksAndRecRelationshipId, TestEntities.Units.CityOfPawneeUnitId, 99999, Description = "Department Id not found")]
			[TestCase(99999, TestEntities.Units.CityOfPawneeUnitId, 99999, Description = "Department Support Relationship Id not found")]
			public async Task NotFoundCannotUpdateSupportRelationship(int relationshipid, int unitId, int departmentId)
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = unitId,
					DepartmentId = departmentId
				};
				var resp = await PutAuthenticated($"supportRelationships/{relationshipid}", req, ValidAdminJwt);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
			}

			[Test]
			public async Task ConflictUpdateSupportRelationship()
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = TestEntities.SupportRelationships.ParksAndRecRelationship.UnitId,
					DepartmentId = TestEntities.SupportRelationships.ParksAndRecRelationship.DepartmentId
				};

				var resp = await PutAuthenticated($"supportRelationships/{TestEntities.SupportRelationships.ParksAndRecUnitFireId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Conflict);
			}
		}
		public class SupportRelationshipDelete : ApiTest
		{
			[TestCase(TestEntities.SupportRelationships.ParksAndRecRelationshipId, ValidAdminJwt, HttpStatusCode.NoContent, Description = "Admin can delete a support relationship.")]
			[TestCase(TestEntities.SupportRelationships.ParksAndRecRelationshipId, ValidRswansonJwt, HttpStatusCode.Forbidden, Description = "Non-Admin cannot delete a support relationship.")]
			[TestCase(9999, ValidAdminJwt, HttpStatusCode.NotFound, Description = "Cannot delete a support relationship that does not exist.")]
			public async Task CanDeleteSupportRelationship(int relationshipId, string jwt, HttpStatusCode expectedCode)
			{
				var resp = await DeleteAuthenticated($"supportRelationships/{relationshipId}", jwt);
				AssertStatusCode(resp, expectedCode);
			}
		}
	}
}