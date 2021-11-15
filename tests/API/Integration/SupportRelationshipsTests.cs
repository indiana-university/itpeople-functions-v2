using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;

namespace Integration
{
	public class SupportRelationshipsTests
	{
		public const string ArchivedUnitError = "The request body was malformed, the provided unit has been archived and is not available for new Support Relationships.";
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

			[Test]
			public async Task SupportRelationshipsGetSsspFormat()
			{
				/* Department has single support relationship */
				// Supporting unit has an email
				// Supporting unit has no email but ONE leader who does
				// Supporting unit has no email, but SEVERAL leaders who do
				// Supporting unit has no email, and no leader with an email
				// Supporting unit has an email but is no longer active.

				/* Department has multiple support relationships*/
				// Both units have an email
				// One unit has an email, the other does not, but has a leader that does
				// One unit has no email but a leader that does, and the other has no email and no leaders
				// One unit has an email, the other unit is not active

				var resp = await GetAuthenticated("SsspSupportRelationships");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<SsspSupportRelationshipResponse>>();
				Assert.AreEqual(2, actual.Count);
				Assert.False(true, "Still writing test.");
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
				Assert.AreEqual(expected.SupportType.Id, actual.SupportType.Id);
			}
		}

		public class SupportRelationshipCreate : ApiTest
		{
			private SupportRelationshipRequest FireAuditor = new SupportRelationshipRequest
			{
				UnitId = TestEntities.Units.Auditor.Id,
				DepartmentId = TestEntities.Departments.Fire.Id,
				SupportTypeId = TestEntities.SupportTypes.DesktopEndpoint.Id
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
				Assert.AreEqual(FireAuditor.SupportTypeId, actual.SupportType.Id);
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
				Assert.Contains("The request body was malformed, the unitId, departmentId, and/or supportTypeId field was missing or invalid.", actual.Errors);
				Assert.AreEqual("(none)", actual.Details);
			}

			//403 unauthorized
			[Test]
			public async Task SupportRelationshipsUnauthorizedCannotCreate()
			{
				var resp = await PostAuthenticated("supportRelationships", FireAuditor, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[TestCase(99999, TestEntities.Departments.FireId, null, Description = "Unit Id not found")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, 99999, null, Description = "Department Id not found")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, TestEntities.Departments.AuditorId, 99999, Description = "Support Type Id not found")]
			public async Task NotFoundCannotCreateSupportRelationship(int unitId, int departmentId, int? supportTypeId)
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = unitId,
					DepartmentId = departmentId,
					SupportTypeId = supportTypeId
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

			//400 Can't make a Support Relationship for an inactive(archived) Unit.
			[Test]
			public async Task CannotCreateSupportRelationshipForArchivedUnit()
			{
				var candlemakersSupportAudits = new SupportRelationshipRequest
				{
					UnitId = TestEntities.Units.ArchivedUnitId,
					DepartmentId = TestEntities.Departments.AuditorId
				};
				var resp = await PostAuthenticated("supportRelationships", candlemakersSupportAudits, ValidAdminJwt);
				
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
				var actual = await resp.Content.ReadAsAsync<ApiError>();

				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains(ArchivedUnitError, actual.Errors);
				Assert.AreEqual("(none)", actual.Details);
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
				Assert.Contains("The request body was malformed, the unitId, departmentId, and/or supportTypeId field was missing or invalid.", actual.Errors);
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

			[TestCase(TestEntities.SupportRelationships.ParksAndRecRelationshipId, 99999, TestEntities.Departments.FireId, null, Description = "Unit Id not found")]
			[TestCase(TestEntities.SupportRelationships.ParksAndRecRelationshipId, TestEntities.Units.CityOfPawneeUnitId, 99999, null, Description = "Department Id not found")]
			[TestCase(TestEntities.SupportRelationships.ParksAndRecRelationshipId, TestEntities.Units.CityOfPawneeUnitId, TestEntities.Departments.AuditorId, 99999, Description = "Support Type Id not found")]
			[TestCase(99999, TestEntities.Units.CityOfPawneeUnitId, 99999, null, Description = "Department Support Relationship Id not found")]
			public async Task NotFoundCannotUpdateSupportRelationship(int relationshipid, int unitId, int departmentId, int? supportTypeId)
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = unitId,
					DepartmentId = departmentId,
					SupportTypeId = supportTypeId
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

				var resp = await PutAuthenticated($"supportRelationships/{TestEntities.SupportRelationships.PawneeUnitFireId}", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.Conflict);
			}

			//Cannot update relationship to use an inactive Unit
			[Test]
			public async Task CannotUpdateSupportRelationshipToUseArchivedUnit()
			{
				var changeToInactiveUnit = new SupportRelationshipRequest {
					UnitId = TestEntities.Units.ArchivedUnitId,
					DepartmentId = TestEntities.Departments.FireId
				};

				var resp = await PutAuthenticated($"supportRelationships/{TestEntities.SupportRelationships.PawneeUnitFireId}", changeToInactiveUnit, ValidAdminJwt);

				var actual = await resp.Content.ReadAsAsync<ApiError>();
				AssertStatusCode(resp, HttpStatusCode.BadRequest);

				Assert.AreEqual(1, actual.Errors.Count);
				Assert.Contains(ArchivedUnitError, actual.Errors);
				Assert.AreEqual("(none)", actual.Details);
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