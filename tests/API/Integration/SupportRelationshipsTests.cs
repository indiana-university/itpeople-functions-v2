using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;
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
				SupportTypeId = TestEntities.SupportTypes.DesktopEndpoint.Id,
				ReportSupportingUnitId = TestEntities.Units.Auditor.Id
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

			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, HttpStatusCode.Created, PermsGroups.All, Description = "Owner")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, HttpStatusCode.Created, PermsGroups.All, Description = "Owner Inheritted From Parent")]
			public async Task SupportRelationshipPostEntityPermissions(int unitWithPermissions, UnitPermissions providedPermission, HttpStatusCode expectedCode, EntityPermissions expectedPermission)
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = TestEntities.Units.ParksAndRecUnitId,
					DepartmentId = TestEntities.Departments.AuditorId,
					ReportSupportingUnitId = TestEntities.Units.ParksAndRecUnitId
				};
				await PostReturnsCorrectEntityPermissions("supportRelationships", req, unitWithPermissions, providedPermission, expectedCode, expectedPermission);
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

			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember")]
			[TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, HttpStatusCode.NoContent, PermsGroups.All, Description = "Owner")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, HttpStatusCode.Forbidden, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
			[TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, HttpStatusCode.NoContent, PermsGroups.All, Description = "Owner Inheritted From Parent")]
			public async Task SupportRelationshipDeleteEntityPermissions(int unitWithPermissions, UnitPermissions providedPermission, HttpStatusCode expectedCode, EntityPermissions expectedPermission)
			{
				// Add a support relationshp for Parks & Rec so we can test inheritted permissions
				var relationship = await GenerateParksAndRecSupportingAuditDept();

				await DeleteReturnsCorrectEntityPermissions($"supportRelationships/{relationship.Id}", unitWithPermissions, providedPermission, expectedCode, expectedPermission);
			}
		}

		public static async Task<SupportRelationship> GenerateParksAndRecSupportingAuditDept()
		{
			var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
			var relationship = new SupportRelationship
			{
				UnitId = TestEntities.Units.ParksAndRecUnitId,
				DepartmentId = TestEntities.Departments.AuditorId,
				SupportTypeId = TestEntities.SupportTypes.FullServiceId
			};
			await db.SupportRelationships.AddAsync(relationship);
			await db.SaveChangesAsync();
			return relationship;
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

				db.Departments.AddRange(new List<Department> {
					TestEntities.Departments.Parks,
					TestEntities.Departments.Fire,
					TestEntities.Departments.Auditor
				});
				await db.SaveChangesAsync();

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

			private static IEnumerable<TestCaseData> _Cases
			{
				get
				{
					/* Department has single support relationship */
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithEmailId, ParksDeptId, null) }, 1, "Supporting unit has an email");
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithNoEmailButLeaderWhoDoesId, ParksDeptId, null) }, 1, "Supporting unit has no email but ONE leader who does");
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithNoEmailButLeadersWhoDoId, ParksDeptId, null) }, 2, "Supporting unit has no email, but SEVERAL leaders who do");
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithNoEmailAndNoLeaderWhoDoesId, ParksDeptId, null) }, 0, "Supporting unit has no email, and no leader with an email");
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithEmailInactiveId, ParksDeptId, null) }, 0, "Supporting unit has an email but is no longer active.");

					/* Department has multiple support relationships*/
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithEmailId, ParksDeptId, null), new SupportRelationship(SecondUnitWithEmailId, ParksDeptId, null) }, 2, "Both units have an email");
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithEmailId, ParksDeptId, null), new SupportRelationship(UnitWithNoEmailButLeaderWhoDoesId, ParksDeptId, null) }, 2, "One unit has an email, the other does not, but has a leader that does");
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithNoEmailButLeaderWhoDoesId, ParksDeptId, null), new SupportRelationship(UnitWithNoEmailAndNoLeaderWhoDoesId, ParksDeptId, null) }, 1, "One unit has no email but a leader that does, and the other has no email and no leaders");
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(UnitWithEmailId, ParksDeptId, null), new SupportRelationship(UnitWithEmailInactiveId, ParksDeptId, null) }, 1, "One unit has an email, the other unit is not active");
					yield return new TestCaseData(new List<SupportRelationship> { new SupportRelationship(DuplicateAId, ParksDeptId, null), new SupportRelationship(DuplicateBId, ParksDeptId, null) }, 1, "Supported by two units each having the same email address, one getting its email from its unit the other from its unit's leader.");
				}
			}

			[Test]
			[TestCaseSource(nameof(_Cases))]
			public async Task SupportRelationshipsGetSsspFormat(IEnumerable<SupportRelationship> relationships, int expectedMatches, string description)
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

			[Test]
			public async Task FilterSsspRelationshipResults()
			{
				// Reset the state of the database, and setup all our Support Relationships between Units and Departments.
				await Setup();
				var db = GetDb();
				
				var parksSupportRelationship = new SupportRelationship(UnitWithEmailId, ParksDeptId, null);
				var fireSupportRelationship = new SupportRelationship(UnitWithEmailId, TestEntities.Departments.FireId, null);
				var otherFireSupportRelationship = new SupportRelationship(UnitWithNoEmailButLeaderWhoDoesId, TestEntities.Departments.FireId, null);
				
				await db.SupportRelationships.AddRangeAsync(new List<SupportRelationship> { parksSupportRelationship, fireSupportRelationship, otherFireSupportRelationship });
				await db.SaveChangesAsync();

				// Request filtered results from the API endpoint
				var resp = await GetAuthenticated($"SsspSupportRelationships?dept={TestEntities.Departments.FireName}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<SsspSupportRelationshipResponse>>();

				// Make sure we got what we expected.
				Assert.AreEqual(2, actual.Count);
				Assert.True(actual.All(r => r.Dept == TestEntities.Departments.FireName));

				// Ensure explicitly URL Encoded values work
				resp = await GetAuthenticated($"SsspSupportRelationships?dept={System.Web.HttpUtility.UrlEncode(TestEntities.Departments.FireName)}");
				AssertStatusCode(resp, HttpStatusCode.OK);
				actual = await resp.Content.ReadAsAsync<List<SsspSupportRelationshipResponse>>();

				// Make sure we got what we expected.
				Assert.AreEqual(2, actual.Count);
				Assert.True(actual.All(r => r.Dept == TestEntities.Departments.FireName));

				// Ensure non-existent department names get no results
				resp = await GetAuthenticated($"SsspSupportRelationships?dept=Department That Does Not Exist");
				AssertStatusCode(resp, HttpStatusCode.OK);
				actual = await resp.Content.ReadAsAsync<List<SsspSupportRelationshipResponse>>();

				// Make sure we got what we expected.
				Assert.AreEqual(0, actual.Count);
			}
		}

		public class SupportRelationshipReportSupportingUnit : ApiTest
		{
			private const string _InvalidUnit = "You may only set the Department Report Supporting Unit to your own unit or one of its parent units.";
			private Database.PeopleContext GetDb() => Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);

			private async Task<Department> CreateDepartment(string name = "Test Department", string description = "Test Dept. description")
			{
				var db = GetDb();
				var dept = new Department
				{
					Name = name,
					Description = description,
				};

				await db.Departments.AddAsync(dept);
				await db.SaveChangesAsync();

				return dept;
			}

			private async Task<SupportRelationship> CreateSupportRelationship(int departmentId, int unitId, int supportTypeId, int reportSupportingUnitId, string jwt = ValidAdminJwt)
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = unitId,
					DepartmentId = departmentId,
					SupportTypeId = supportTypeId,
					ReportSupportingUnitId = reportSupportingUnitId
				};

				var createResponse = await PostAuthenticated("SupportRelationships", req, jwt);
				AssertStatusCode(createResponse, HttpStatusCode.Created);
				var createResult = await createResponse.Content.ReadAsAsync<SupportRelationship>();
				Assert.AreEqual(req.UnitId, createResult.Unit.Id);
				Assert.AreEqual(req.DepartmentId, createResult.Department.Id);
				Assert.AreEqual(req.SupportTypeId, createResult.SupportType.Id);
				Assert.AreEqual(req.ReportSupportingUnitId, createResult.Department.ReportSupportingUnit.Id);
				return createResult;
			}

			/// <summary>When a Department has SupportRelationship added for it, the Department.ReportSupportingUnit must not be null.</summary>
			[Test]
			public async Task CreatingSupportRelationshipMustSetAReportSupportingUnit()
			{
				var req = new SupportRelationshipRequest
				{
					UnitId = TestEntities.Units.Auditor.Id,
					DepartmentId = TestEntities.Departments.Fire.Id,
					SupportTypeId = TestEntities.SupportTypes.DesktopEndpoint.Id
				};
				var resp = await PostAuthenticated("SupportRelationships", req, ValidAdminJwt);
				
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
				var error = await resp.Content.ReadAsAsync<ApiError>();
				Assert.Contains("The request body was malformed, the reportSupportingUnitId field was missing or invalid.", error.Errors);
			}

			[TestCase(ValidAdminJwt, true)]
			[TestCase(ValidRswansonJwt, false)]// Ron is the leader of the Parks & Rec unit, but not an admin.
			public async Task OnlyAdminsChangeDepartmentReportSupportingUnitWhenMultipleSupportRelationshipsExist(string jwt, bool canSet)
			{
				// Make a new department, and establish a SupportRelationship and ReportSupportingUnit to the "City of Pawneee" unit.
				var testDepartment = await CreateDepartment();
				var citySR = await CreateSupportRelationship(testDepartment.Id, TestEntities.Units.CityOfPawneeUnitId, TestEntities.SupportTypes.FullServiceId, TestEntities.Units.CityOfPawneeUnitId);
				
				// Make a request to create a new SupportRelationship for testDepartment to the "Parks & Rec" unit, and to also change the ReportSupportingUnit to "Parks & Rec".
				// If they are an Admin this will work.
				// If they are just a unit leader, it will simply make a request, because now there is more than one SupportRelationshp.
				var req = new SupportRelationshipRequest
				{
					UnitId = TestEntities.Units.ParksAndRecUnitId,
					DepartmentId = testDepartment.Id,
					SupportTypeId = TestEntities.SupportTypes.DesktopEndpoint.Id,
					ReportSupportingUnitId = TestEntities.Units.ParksAndRecUnitId
				};

				var resp = await PostAuthenticated("SupportRelationShips", req, jwt);
				var respAsString = await resp.Content.ReadAsStringAsync();
				AssertStatusCode(resp, HttpStatusCode.Created);
				var actual = await resp.Content.ReadAsAsync<SupportRelationshipResponse>();

				if(canSet)
				{
					Assert.AreEqual(TestEntities.Units.ParksAndRecUnitId, actual.Department.ReportSupportingUnit.Id);
				}
				else
				{
					//It should not have changed, but a notification of Ron's request to change it should have been created.
					Assert.AreEqual(TestEntities.Units.CityOfPawneeUnitId, actual.Department.ReportSupportingUnit.Id);
					var db = GetDb();
					var notifications = await db.Notifications.ToListAsync();
					Assert.AreEqual(1, notifications.Count);
					Assert.AreEqual($"rswanso requests to change {testDepartment.Name} Report Supporting Unit to {TestEntities.Units.ParksAndRecUnit.Name}.", notifications.First().Message);
				}
			}

			[Test]
			public async Task OwnerCanLeaveDepartmentReportSupportingUnitAsExistingValueOutsideTheirUnitAncestry()
			{
				var db = GetDb();
				var units = await GenerateUnitFamilyTrees(db);
				var testDept = await CreateDepartment();
				// Make a SupportRelationship between unitB_1 and testDept.
				await CreateSupportRelationship(testDept.Id, units.UnitB_1.Id, TestEntities.SupportTypes.DesktopEndpointId, units.UnitB_1.Id);

				// Make Ron the lead for UnitA_1
				var ronUnitMembership = new UnitMember
				{
					UnitId = units.UnitA_1.Id,
					PersonId = TestEntities.People.RSwansonId,
					Role = Role.Leader,
					Permissions = UnitPermissions.Owner,
					Title = "Lord Protector",
					Percentage = 1,
					Notes = string.Empty
				};
				await db.UnitMembers.AddAsync(ronUnitMembership);
				await db.SaveChangesAsync();

				// Ron adds a SupportRelationship for his unit, but leaves Department.ReportSupportingUnit unchanged.
				var req = new SupportRelationshipRequest
				{
					UnitId = units.UnitA_1.Id,
					DepartmentId = testDept.Id,
					SupportTypeId = TestEntities.SupportTypes.WebAppInfrastructureId,
					ReportSupportingUnitId = units.UnitB_1.Id
				};

				var resp = await PostAuthenticated("SupportRelationships", req, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Created);
				var actual = await resp.Content.ReadAsAsync<SupportRelationshipResponse>();
				Assert.AreEqual(testDept.Id, actual.Department.Id);
				
				var notifications = await db.Notifications.ToListAsync();
				Assert.AreEqual(0, notifications.Count);
			}

			[Test]
			public async Task ReportSupportingUnitMustBeNullWhenNoSupportRelationshipsExist()
			{
				// Create a new Department to test with.
				var testDept = await CreateDepartment();

				// Create a SupportRelationship and set the department's ReportSupportingUnit.
				// Rswanson is the Parks & Rec unit leadter, should be able to create.
				var createResult = await CreateSupportRelationship(testDept.Id, TestEntities.Units.ParksAndRecUnitId, TestEntities.SupportTypes.DesktopEndpointId, TestEntities.Units.ParksAndRecUnitId, ValidRswansonJwt);

				// Request to RemoveSupportRelationship
				var resp = await DeleteAuthenticated($"supportRelationships/{createResult.Id}", ValidRswansonJwt);// Rswanson is the Parks & Rec unit leadter, should be able to delete.
				// We expect it to be deleted.
				AssertStatusCode(resp, HttpStatusCode.NoContent);

				// We also expect that after the update testDept will no longer have a ReportSupportingUnit
				var db = GetDb();
				var updatedDept = db.Departments
					.Include(d => d.ReportSupportingUnit)
					.Single(d => d.Id == testDept.Id);
				Assert.IsNull(updatedDept.ReportSupportingUnit);

				// Ensure a notification was created when removed by a non-Admin.
				var notifications = await db.Notifications.ToListAsync();
				Assert.AreEqual(1, notifications.Count);
				var expectedMessage = $"rswanso has removed Support Relationship between the unit {createResult.Unit.Name} and department {testDept.Name}.  The department no longer has a Report Supporting Unit.";
				Assert.AreEqual(expectedMessage, notifications.First().Message);
			}

			///<summary>
			/// When a SupportRelation ship for a department is deleted, and the
			/// Department.ReportSupportingUnit is still related to one of the Department's
			/// other SupportRelationships' Unit then Department.ReportSupportingUnit should not change.
			///</summary>
			[Test]
			public async Task ReportSupportingUnitDoesNotBecomeNullWhenSupportRelationshipsExist()
			{
				// Create a new Department to test with.
				var testDept = await CreateDepartment();

				// Create two SupportRelationships for the department, one for Parks & Rec unit, and one for the Auditor unit.
				// Set the ReportSupportingUnit to be City of Pawnee - the Parks & Rec and Auditor units mutal parent.. 
				var parksSR = await CreateSupportRelationship(testDept.Id, TestEntities.Units.ParksAndRecUnitId, TestEntities.SupportTypes.DesktopEndpointId, TestEntities.Units.CityOfPawneeUnitId);
				var auditorSR = await CreateSupportRelationship(testDept.Id, TestEntities.Units.AuditorId, TestEntities.SupportTypes.ResearchInfrastructureId, TestEntities.Units.CityOfPawneeUnitId);

				// Delete the Parks & Rec support relationship
				var resp = await DeleteAuthenticated($"SupportRelationships/{parksSR.Id}", ValidRswansonJwt);// Ron is the leader of the unit, so he should be able to perform this delete.
				AssertStatusCode(resp, HttpStatusCode.NoContent);

				// Department.ReportSupportingUnit should have remained the City of Pawnee unit.
				var db = GetDb();
				var departmentResult = await db.Departments
					.Include(d => d.ReportSupportingUnit)
					.SingleOrDefaultAsync(d => d.Id == testDept.Id);
				Assert.AreEqual(TestEntities.Units.CityOfPawneeUnitId, departmentResult.ReportSupportingUnit?.Id);

				// No notification should have been made.
				Assert.AreEqual(0, db.Notifications.Count());
			}

			///<summary>
			/// A variation on 
			/// When a SupportRelation ship for a department is deleted, and the
			/// Department.ReportSupportingUnit is no longer related to any of the Department's
			/// other SupportRelationships then Department.ReportSupportingUnit should become null
			///</summary>
			[Test]
			public async Task ReportSupportingUnitBecomesNullWhenRelatedSupportRelationshipsEnd()
			{
				// Create a new Department to test with.
				var testDept = await CreateDepartment();

				// Create two SupportRelationships for the department, one for Parks & Rec unit, and one for the Auditor unit.
				// Set the ReportSupportingUnit to be parks and rec. 
				var parksSR = await CreateSupportRelationship(testDept.Id, TestEntities.Units.ParksAndRecUnitId, TestEntities.SupportTypes.DesktopEndpointId, TestEntities.Units.ParksAndRecUnitId);
				var auditorSR = await CreateSupportRelationship(testDept.Id, TestEntities.Units.AuditorId, TestEntities.SupportTypes.ResearchInfrastructureId, TestEntities.Units.ParksAndRecUnitId);

				// Delete the Parks & Rec support relationship
				var resp = await DeleteAuthenticated($"SupportRelationships/{parksSR.Id}", ValidRswansonJwt);// Ron is the leader of the unit, so he should be able to perform this delete.
				AssertStatusCode(resp, HttpStatusCode.NoContent);

				// Department.ReportSupportingUnit should have defaulted to the auditor
				var db = GetDb();
				var departmentResult = await db.Departments.Include(d => d.ReportSupportingUnit).SingleOrDefaultAsync(d => d.Id == testDept.Id);
				Assert.IsNull(departmentResult.ReportSupportingUnit);

				// Ensure a notification was created when removed by a non-Admin.
				var notifications = await db.Notifications.ToListAsync();
				Assert.AreEqual(1, notifications.Count);
				var expectedMessage = $"rswanso has removed Support Relationship between the unit {parksSR.Unit.Name} and department {testDept.Name}.  The department no longer has a Report Supporting Unit.";
				Assert.AreEqual(expectedMessage, notifications.First().Message);
			}

			/// <summary>The department's SupportingUnit cannot be set to a unit that is not one of the units in the building's SupportRelationships, or one of those units' parents.</summary>
			[TestCase(ValidAdminJwt, HttpStatusCode.BadRequest, _InvalidUnit, Description = "Admin Bad Assignment")]
			[TestCase(ValidRswansonJwt, HttpStatusCode.BadRequest, _InvalidUnit, Description = "Other team's leader")]
			[TestCase(ValidBwyattJwt, HttpStatusCode.Forbidden, "You are not authorized to make this request.", Description = "Non-Lead should get 403")]
			public async Task ReportSupportingUnitMustBeInASupportRelationshipUnitAncestry(string jwt, HttpStatusCode expectedCode, string expectedError)
			{
				var db = GetDb();

				// Create a new Department to test with.
				var testDept = await CreateDepartment();

				// Add a new unit that is independent from all the other units in the DB.
				var indieUnit = new Unit("Independent Unit", "tu ne cede malis, sed contra audentior ito", "https://example.com", "fake@example.com");
				await db.Units.AddAsync(indieUnit);
				await db.SaveChangesAsync();

				// Make a request that provides a ReportSupportingUnitId that is not in the ancestry of any units
				// testDept has a SupportRelationship with.
				var req = new SupportRelationshipRequest
				{
					UnitId = TestEntities.Units.ParksAndRecUnitId,
					DepartmentId = testDept.Id,
					SupportTypeId = TestEntities.SupportTypes.WebAppInfrastructureId,
					ReportSupportingUnitId = indieUnit.Id
				};

				var resp = await PostAuthenticated("SupportRelationships", req, jwt);
				AssertStatusCode(resp, expectedCode);
				var error = await resp.Content.ReadAsAsync<ApiError>();
				Assert.Contains(expectedError, error.Errors);
			}


			private async Task<(Unit UnitA, Unit UnitA_1, Unit UnitA_1_1, Unit UnitA_2, Unit UnitB, Unit UnitB_1)> GenerateUnitFamilyTrees(Database.PeopleContext db)
			{
				var unitA = new Unit("Unit A", "A top", "http://fake.com", "fake@fake.com");
				await db.Units.AddAsync(unitA);
				await db.SaveChangesAsync();
				
				var unitA_1 = new Unit("Unit A-1", "A child", "http://fake.com", "fake@fake.com", unitA.Id);
				await db.Units.AddAsync(unitA_1);
				await db.SaveChangesAsync();

				var unitA_1_1 = new Unit("Unit A-1-1", "A grandchild", "http://fake.com", "fake@fake.com", unitA_1.Id);
				await db.Units.AddAsync(unitA_1_1);
				await db.SaveChangesAsync();

				var unitA_2 = new Unit("Unit A-2", "A child", "http://fake.com", "fake@fake.com", unitA.Id);
				await db.Units.AddAsync(unitA_2);
				await db.SaveChangesAsync();

				var unitB = new Unit("Unit B", "B top", "http://fake.com", "fake@fake.com");
				await db.Units.AddAsync(unitB);
				await db.SaveChangesAsync();

				var unitB_1 = new Unit("Unit B-1", "B child", "http://fake.com", "fake@fake.com", unitB.Id);
				await db.Units.AddAsync(unitB_1);
				await db.SaveChangesAsync();

				return (unitA, unitA_1, unitA_1_1, unitA_2, unitB, unitB_1);
			}

			[Test]
			public async Task AdminGetsSupportingUnitSuggestionsForAllSupportRelationships()
			{
				var db = GetDb();
				var units = await GenerateUnitFamilyTrees(db);
				var testDept = await CreateDepartment();
				// Make a SupportRelationship between unitB_1 and testDept.
				await CreateSupportRelationship(testDept.Id, units.UnitB_1.Id, TestEntities.SupportTypes.DesktopEndpointId, units.UnitB_1.Id);

				// Make a GET request to find the valid ReportSupportingUnits for an Admin user as if they were going to make a new SupportRelationship between UnitA_1_1 and testDept.
				var resp = await GetAuthenticated($"ValidReportSupportingUnits?departmentId={testDept.Id}&unitId={units.UnitA_1_1.Id}", ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);

				var actual = await resp.Content.ReadAsAsync<List<Unit>>();

				var expected = new List<Unit> { units.UnitA, units.UnitA_1, units.UnitA_1_1, units.UnitB, units.UnitB_1 };

				var actualIds = actual.Select(u => u.Id).ToList();
				var expectedIds = expected.Select(u => u.Id).ToList();
				Assert.That(actualIds, Is.EquivalentTo(expectedIds));
			}

			[Test]
			public async Task UnitLeaderGetsSupportingUnitSuggestionsForUnitFamilyTree()
			{
				var db = GetDb();
				var units = await GenerateUnitFamilyTrees(db);
				var testDept = await CreateDepartment();
				// Make a SupportRelationship between unitB_1 and testDept.
				await CreateSupportRelationship(testDept.Id, units.UnitB_1.Id, TestEntities.SupportTypes.DesktopEndpointId, units.UnitB_1.Id);

				// Make Ron the lead for UnitA_1
				var ronUnitMembership = new UnitMember
				{
					UnitId = units.UnitA_1.Id,
					PersonId = TestEntities.People.RSwansonId,
					Role = Role.Leader,
					Permissions = UnitPermissions.ManageMembers,
					Title = "Lord Protector",
					Percentage = 1,
					Notes = string.Empty
				};
				await db.UnitMembers.AddAsync(ronUnitMembership);
				await db.SaveChangesAsync();

				// Make a GET request to find the valid ReportSupportingUnits for Ron as if he were going to make a new SupportRelationship between UnitA_1_1 and testDept.
				var resp = await GetAuthenticated($"ValidReportSupportingUnits?departmentId={testDept.Id}&unitId={units.UnitA_1_1.Id}", ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);

				var actual = await resp.Content.ReadAsAsync<List<Unit>>();

				var expected = new List<Unit> { units.UnitA, units.UnitA_1, units.UnitA_1_1, units.UnitB_1 };

				var actualIds = actual.Select(u => u.Id).ToList();
				var expectedIds = expected.Select(u => u.Id).ToList();
				Assert.That(actualIds, Is.EquivalentTo(expectedIds));
			}

			// The following tests really belong in DepartmentsTests, but all the tooling is here.
			[Test]
			public async Task AdminCanSetValidReportSupportingUnit()
			{
				var db = GetDb();
				var dept = await CreateDepartment();
				var units = await GenerateUnitFamilyTrees(db);
				// Create a support Relationship between dept and UnitA_1.
				// This means unitA and unitA_1 are valid options, and all other units are not.
				await CreateSupportRelationship(dept.Id, units.UnitA_1.Id, TestEntities.SupportTypes.FullServiceId, units.UnitA_1.Id);

				// Make a request to set dept.ReportSupportingUnit to UnitA.
				var req = new Department { Id = dept.Id, Name = dept.Name, ReportSupportingUnit = units.UnitA };
				var resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);

				var actual = await resp.Content.ReadAsAsync<Department>();
				Assert.AreEqual(dept.Id, actual.Id);
				Assert.AreEqual(dept.Name, actual.Name);
				Assert.AreEqual(dept.Description, actual.Description);
				Assert.AreEqual(units.UnitA.Id, actual.ReportSupportingUnit.Id);

				// Make a request to set dept.ReportSupportingUnit to UnitA_1
				req = new Department { Id = dept.Id, Name = dept.Name, ReportSupportingUnit = units.UnitA_1 };
				resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);

				actual = await resp.Content.ReadAsAsync<Department>();
				Assert.AreEqual(dept.Id, actual.Id);
				Assert.AreEqual(dept.Name, actual.Name);
				Assert.AreEqual(dept.Description, actual.Description);
				Assert.AreEqual(units.UnitA_1.Id, actual.ReportSupportingUnit.Id);

				// Ensure we cannot set to any of the invalid units.
				req = new Department { Id = dept.Id, Name = dept.Name, ReportSupportingUnit = units.UnitA_1_1 };
				resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);

				req = new Department { Id = dept.Id, Name = dept.Name, ReportSupportingUnit = units.UnitA_2 };
				resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);

				req = new Department { Id = dept.Id, Name = dept.Name, ReportSupportingUnit = units.UnitB };
				resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);

				req = new Department { Id = dept.Id, Name = dept.Name, ReportSupportingUnit = units.UnitB_1 };
				resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.BadRequest);
			}

			[Test]
			public async Task HandlesNonExistantDepartmentOrUnit()
			{
				var db = GetDb();
				var dept = await CreateDepartment();
				var units = await GenerateUnitFamilyTrees(db);
				// Create a support Relationship between dept and UnitA_1.
				// This means unitA and unitA_1 are valid options, and all other units are not.
				await CreateSupportRelationship(dept.Id, units.UnitA_1.Id, TestEntities.SupportTypes.FullServiceId, units.UnitA_1.Id);

				// Make a request to set with a bad unit value
				var req = new Department { Id = dept.Id, Name = dept.Name, ReportSupportingUnit = new Unit { Id = 40000, Name = "Not Real" } };
				var resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.NotFound);

				// Make a request to set with a bad department value
				req = new Department { Id = 40000, Name = "Not Real", ReportSupportingUnit = units.UnitA_1 };
				resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.NotFound);
			}

			[TestCase(ValidRswansonJwt)]
			[TestCase(ValidLknopeJwt)]
			[TestCase(ValidJgergichJwt)]
			[TestCase(ValidBwyattJwt)]
			public async Task NonAdminCannotSetValidReportSupportingUnit(string jwt)
			{
				var db = GetDb();
				var dept = await CreateDepartment();
				var units = await GenerateUnitFamilyTrees(db);
				// Create a support Relationship between dept and UnitA_1.
				// This means unitA and unitA_1 are valid options, and all other units are not.
				await CreateSupportRelationship(dept.Id, units.UnitA_1.Id, TestEntities.SupportTypes.FullServiceId, units.UnitA_1.Id);

				// Make a request to set dept.ReportSupportingUnit to UnitA.
				var req = new Department { Id = dept.Id, Name = dept.Name, ReportSupportingUnit = units.UnitA };
				var resp = await PutAuthenticated("SetDepartmentReportSupportingUnit", req, jwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}
		}
	}
}
