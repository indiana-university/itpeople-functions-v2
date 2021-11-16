using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

		public class SsspTests : ApiTest
		{
			private Database.PeopleContext GetDb() => Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
			private static int UnitWithEmailId = 1;
			private static int SecondUnitWithEmailId = 2;
			private static int UnitWithNoEmailButLeaderWhoDoesId = 3;
			private static int UnitWithNoEmailButLeadersWhoDoId = 4;
			private static int UnitWithNoEmailAndNoLeaderWhoDoesId  = 5;
			private static int UnitWithEmailInactiveId = 6;
			private static int DuplicateAId = 7;
			private static int DuplicateBId = 8;
			private static int ParksDeptId = TestEntities.Departments.ParksId;
			
			private async Task Setup()
			{
				var db = GetDb();
				// Nuke existing Support Relationships and Units
				db.Database.ExecuteSqlRaw(@"
					TRUNCATE 
						public.building_relationships, 
						public.people, 
						public.support_relationships, 
						public.units,
						public.unit_members,
						public.unit_member_tools
					RESTART IDENTITY
					CASCADE;
				");

				var personWithEmail = new Person { Netid = "hasemail", Name="Email, Has", Position = "Middle Manager", Location="Poplars", Campus = "BL", Notes = "", PhotoUrl = "", CampusPhone = "55555", CampusEmail = "hasemail@iu.edu" };
				var anotherPersonWithEmail = new Person { Netid = "otherperson", Name="Email, Has Also", Position = "Lower Manager", Location="Ball hall", Campus = "IN", Notes = "", PhotoUrl = "", CampusPhone = "55557", CampusEmail = "otherperson@iupui.edu" };
				var personWithoutEmail = new Person { Netid = "noemail", Name="Email, Has Not", Position = "AVP", Location="Poplars", Campus = "BL", Notes = "", PhotoUrl = "", CampusPhone = "55556", CampusEmail = "" };

				db.People.AddRange(new List<Person> {personWithEmail, anotherPersonWithEmail, personWithoutEmail});
				await db.SaveChangesAsync();

				var unitWithEmail = new Unit { Id = UnitWithEmailId, Name = "Unit With Email", Description = "", Url = "", Email = "unit@fake.com", UnitMembers = new List<UnitMember>(), Active = true };
				var secondUnitWithEmail = new Unit { Id = SecondUnitWithEmailId, Name = "Another Unit With Email", Description = "", Url = "", Email = $"unit{SecondUnitWithEmailId}@fake.com", UnitMembers = new List<UnitMember>(), Active = true };
				
				var unitWithNoEmailButLeaderWhoDoes = new Unit { Id = UnitWithNoEmailButLeaderWhoDoesId, Name = "Unit Without Email, But Leader Does", Description = "", Url = "", Email = null, UnitMembers = new List<UnitMember>(), Active = true };
				unitWithNoEmailButLeaderWhoDoes.UnitMembers.Add(new UnitMember { Person = personWithEmail, Role = Role.Leader, Permissions = UnitPermissions.ManageMembers, Notes = "" });
				
				var unitWithNoEmailButLeadersWhoDo = new Unit { Id = UnitWithNoEmailButLeadersWhoDoId, Name = "Unit Without Email, But LeaderS Do", Description = "", Url = "", Email = null, UnitMembers = new List<UnitMember>(), Active = true };
				unitWithNoEmailButLeadersWhoDo.UnitMembers.Add(new UnitMember { Person = personWithEmail, Role = Role.Leader, Permissions = UnitPermissions.ManageMembers, Notes = "" });
				unitWithNoEmailButLeadersWhoDo.UnitMembers.Add(new UnitMember { Person = anotherPersonWithEmail, Role = Role.Leader, Permissions = UnitPermissions.ManageTools, Notes = "" });
				
				var unitWithNoEmailAndNoLeaderWhoDoes = new Unit { Id = UnitWithNoEmailAndNoLeaderWhoDoesId, Name = "Unit Without Email, And The Leader doesn't either", Description = "", Url = "", Email = null, UnitMembers = new List<UnitMember>(), Active = true };
				unitWithNoEmailAndNoLeaderWhoDoes.UnitMembers.Add(new UnitMember { Person = personWithoutEmail, Role = Role.Leader, Permissions = UnitPermissions.ManageMembers, Notes = "" });
				unitWithNoEmailAndNoLeaderWhoDoes.UnitMembers.Add(new UnitMember { Person = personWithEmail, Role = Role.Member, Permissions = UnitPermissions.ManageMembers, Notes = "" });

				var unitWithEmailInactive = new Unit { Id = UnitWithEmailInactiveId, Name = "Inactive Unit With Email", Description = "", Url = "", Email = "inactive@fake.com", UnitMembers = new List<UnitMember>(), Active = false };

				var duplicateEmailUnitA = new Unit { Id = DuplicateAId, Name = "Duplicate A", Description = "", Url = "", Email = personWithEmail.CampusEmail, UnitMembers = new List<UnitMember>(), Active = true };
				var duplicateEmailUnitB = new Unit { Id = DuplicateBId, Name = "Duplicate B", Description = "", Url = "", Email = null, UnitMembers = new List<UnitMember>(), Active = true };
				duplicateEmailUnitB.UnitMembers.Add(new UnitMember { Person = personWithEmail, Role = Role.Leader, Permissions = UnitPermissions.ManageMembers, Notes = "" });

				db.Units.AddRange(new List<Unit> { unitWithEmail, secondUnitWithEmail, unitWithNoEmailButLeaderWhoDoes, unitWithNoEmailButLeadersWhoDo, unitWithNoEmailAndNoLeaderWhoDoes, unitWithEmailInactive, duplicateEmailUnitA, duplicateEmailUnitB });
				await db.SaveChangesAsync();
			}

			private static readonly object[] _sourceLists = 
			{
				/* Department has single support relationship */
				// Supporting unit has an email
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithEmailId, ParksDeptId, null) }, 1},
				// Supporting unit has no email but ONE leader who does
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithNoEmailButLeaderWhoDoesId, ParksDeptId, null) }, 1},
				// Supporting unit has no email, but SEVERAL leaders who do
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithNoEmailButLeadersWhoDoId, ParksDeptId, null) }, 2},
				// Supporting unit has no email, and no leader with an email
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithNoEmailAndNoLeaderWhoDoesId, ParksDeptId, null) }, 0},
				// Supporting unit has an email but is no longer active.
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithEmailInactiveId, ParksDeptId, null) }, 0},

				/* Department has multiple support relationships*/
				// Both units have an email
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithEmailId, ParksDeptId, null), new SupportRelationship(SecondUnitWithEmailId, ParksDeptId, null) }, 2},
				// One unit has an email, the other does not, but has a leader that does
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithEmailId, ParksDeptId, null), new SupportRelationship(UnitWithNoEmailButLeaderWhoDoesId, ParksDeptId, null) }, 2},
				// One unit has no email but a leader that does, and the other has no email and no leaders
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithNoEmailButLeaderWhoDoesId, ParksDeptId, null), new SupportRelationship(UnitWithNoEmailAndNoLeaderWhoDoesId, ParksDeptId, null) }, 1},
				// One unit has an email, the other unit is not active
				new object[] { new List<SupportRelationship> { new SupportRelationship(UnitWithEmailId, ParksDeptId, null), new SupportRelationship(UnitWithEmailInactiveId, ParksDeptId, null) }, 1},
				// Supported by two units each having the same email address, one getting its email from its unit the other from its unit's leader.
				new object[] { new List<SupportRelationship> { new SupportRelationship(DuplicateAId, ParksDeptId, null), new SupportRelationship(DuplicateBId, ParksDeptId, null) }, 1},
			};
			

			[Test]
			[TestCaseSource("_sourceLists")]
			public async Task SupportRelationshipsGetSsspFormat(IEnumerable<SupportRelationship> relationships, int expectedMatches)
			{
				await Setup();
				var db = GetDb();
				// Insert the provided relationships
				db.SupportRelationships.AddRange(relationships);
				await db.SaveChangesAsync();

				// Get the listing from the API
				var resp = await GetAuthenticated("SsspSupportRelationships");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<SsspSupportRelationshipResponse>>();
				// Ensure all results are valid.
				Assert.False(actual.Any(sr => string.IsNullOrWhiteSpace(sr.ContactEmail)));
				Assert.False(actual.Any(sr => string.IsNullOrWhiteSpace(sr.Dept)));
				Assert.False(actual.Any(sr => string.IsNullOrWhiteSpace(sr.DeptDescription)));
				Assert.False(actual.Any(sr => sr.Key <= 0));

				// Ensure we got the expected number of results.
				Assert.AreEqual(expectedMatches, actual.Count);
			}
		}
	}
}