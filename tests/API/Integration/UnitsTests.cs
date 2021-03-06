using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Models.Enums;

namespace Integration
{
	public class UnitsTests
    {
        public class GetAll : ApiTest
        {
            [Test]
            public async Task FetchesTopLevelUnitsByDefault()
            {
                var resp = await GetAuthenticated("units");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Unit>>();
                Assert.AreEqual(1, actual.Count);
                var expected = TestEntities.Units.CityOfPawnee;
                Assert.AreEqual(expected.Id, actual.Single().Id);
                Assert.AreEqual(expected.Name, actual.Single().Name);
            }

            [Test]
            public async Task SearchReturnsAResult()
            {
                var resp = await GetAuthenticated("units?q=parks");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Unit>>();
                Assert.AreEqual(1, actual.Count);
                var expected = TestEntities.Units.ParksAndRecUnit;
                var actualUnit = actual.Single();
                Assert.AreEqual(expected.Id, actualUnit.Id);
                Assert.AreEqual(expected.Name, actualUnit.Name);
                Assert.AreEqual(expected.ParentId, actualUnit.ParentId);
                Assert.NotNull(actualUnit.Parent);
                Assert.AreEqual(expected.Parent.Id, actualUnit.Parent.Id);
                Assert.AreEqual(expected.Parent.Name, actualUnit.Parent.Name);
            }

            [Test]
            public async Task SearchReturnsNoResults()
            {
                var resp = await GetAuthenticated("units?q=foo");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Unit>>();
                Assert.AreEqual(0, actual.Count);
            }

            [TestCase(ValidRswansonJwt, EntityPermissions.Get, Description="As non-admin I can't create/delete units")]
            [TestCase(ValidAdminJwt, PermsGroups.All, Description="As a service admin I can create/modify/delete units")]
            [TestCase(ValidServiceAcct, EntityPermissions.Get, Description="As a service account I can get, but not create/delete units")]
            public async Task ResponseHasCorrectXUserPermissionsHeader(string jwt, EntityPermissions expectedPermissions)
            {
                var resp = await GetAuthenticated($"units", jwt);
                AssertPermissions(resp, expectedPermissions);
            }
        }

        public class GetOne : ApiTest
        {
            [Test]
            public async Task GetParksAndRecUnit()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.ParksAndRecUnit.Id}");
                AssertStatusCode(resp, HttpStatusCode.OK);

                var actual = await resp.Content.ReadAsAsync<Unit>();
                var expected = TestEntities.Units.ParksAndRecUnit;
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Name, actual.Name);
                Assert.AreEqual(expected.Parent?.Id, actual.Parent?.Id);
            }

            
            [TestCase(ValidRswansonJwt, TestEntities.Units.ParksAndRecUnitId, PermsGroups.All, Description="As Ron I can do anything to a unit I own")]
            [TestCase(ValidRswansonJwt, TestEntities.Units.CityOfPawneeUnitId, EntityPermissions.Get, Description="As Ron I can't update a unit I don't manage")]
            [TestCase(ValidAdminJwt, TestEntities.Units.ParksAndRecUnitId, PermsGroups.All, Description="As a service admin I can do anything to any unit")]
            [TestCase(ValidAdminJwt, TestEntities.Units.CityOfPawneeUnitId, PermsGroups.All, Description="As a service admin I can do anything to any unit")]
            [TestCase(ValidServiceAcct, TestEntities.Units.ParksAndRecUnitId, EntityPermissions.Get, Description="As a service account I can get a unit")]
            public async Task ResponseHasCorrectXUserPermissionsHeader(string jwt, int unitId, EntityPermissions expectedPermissions)
            {
                var resp = await GetAuthenticated($"units/{unitId}", jwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, expectedPermissions);
            }
        }

        public class UnitCreate : ApiTest
        {
            private Unit ExpectedMayorsOffice = new Unit(){
                Name = "Pawnee Mayor's Office",
                Description = "The Office of the Mayor",
                Url = "http://gunderson.geocities.com",
                Email = "mayor@example.com",
                Parent = TestEntities.Units.CityOfPawnee
            };

            //201 returns new unit
            [Test]
            public async Task CreateMayorsOffice()
            {
                var req = new Unit(ExpectedMayorsOffice.Name, ExpectedMayorsOffice.Description, ExpectedMayorsOffice.Url, ExpectedMayorsOffice.Email, ExpectedMayorsOffice.Parent.Id);
                var resp = await PostAuthenticated("units", req, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.Created);
                var actual = await resp.Content.ReadAsAsync<Unit>();

                Assert.NotZero(actual.Id);
                Assert.AreEqual(req.Name, actual.Name);
                Assert.AreEqual(req.Description, actual.Description);
                Assert.AreEqual(req.Url, actual.Url);
                Assert.AreEqual(req.Email, actual.Email);
                Assert.NotNull(actual.Parent);
                Assert.AreEqual(req.ParentId, actual.Parent.Id);
            }

            [Test]
            public async Task CreateWithDefaultValues()
            {
                var req = new UnitRequest { Name = "Foo" };
                var resp = await PostAuthenticated("units", req, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.Created);
            }

            //403 unauthorized
            [Test]
            public async Task UnauthorizedCannotCreate()
            {
                var req = new Unit(ExpectedMayorsOffice.Name, ExpectedMayorsOffice.Description, ExpectedMayorsOffice.Url, ExpectedMayorsOffice.Email, ExpectedMayorsOffice.Parent.Id);
                var resp = await PostAuthenticated("units", req, ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.Forbidden);
            }

            
            //400 The request body is malformed or missing
            [Test]
            public async Task CannotCreateMalformedUnit()
            {
                var req = new Unit("", ExpectedMayorsOffice.Description, ExpectedMayorsOffice.Url, ExpectedMayorsOffice.Email, ExpectedMayorsOffice.Parent.Id);
                var resp = await PostAuthenticated("units", req, ValidAdminJwt);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual((int)HttpStatusCode.BadRequest, actual.StatusCode);
                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains(UnitRequest.MalformedRequest, actual.Errors);
                Assert.AreEqual("(none)", actual.Details);
            }

            //404 The specified unit parent does not exist
            [Test]
            public async Task CannotCreateUnitWithInvalidParentId()
            {
                var req = new Unit(ExpectedMayorsOffice.Name, ExpectedMayorsOffice.Description, ExpectedMayorsOffice.Url, ExpectedMayorsOffice.Email);
                req.ParentId = 9999;

                var resp = await PostAuthenticated("units", req, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.NotFound);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains($"No parent unit found with ID ({req.ParentId}).", actual.Errors);
                Assert.AreEqual("(none)", actual.Details);
            }
        }

        public class UnitEdit : ApiTest
        {
            // 200
            [Test]
            public async Task UpdatePawnee()
            {
                var req = new Unit("Eagleton", "Gated Community of Eagleton, Indiana", null, "hoa@eagleton.biz");
                req.Id = TestEntities.Units.CityOfPawnee.Id;

                var resp = await PutAuthenticated($"units/{TestEntities.Units.CityOfPawnee.Id}", req, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<Unit>();

                Assert.AreEqual(TestEntities.Units.CityOfPawnee.Id, actual.Id);
                Assert.AreEqual(req.Name, actual.Name);
                Assert.AreEqual(req.Description, actual.Description);
                Assert.AreEqual(req.Url, actual.Url);
                Assert.AreEqual(req.Email, actual.Email);
                Assert.IsNull(actual.Parent);
            }
            // 400 The request body is malformed, or the unit name is missing.
            [Test]
            public async Task CannotUpdateWithMalformedUnit()
            {
                var req = new Unit("", "Gated Community of Eagleton, Indiana", null, "hoa@eagleton.biz");
                req.Id = TestEntities.Units.CityOfPawnee.Id;

                var resp = await PutAuthenticated($"units/{TestEntities.Units.CityOfPawnee.Id}", req, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.BadRequest);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual((int)HttpStatusCode.BadRequest, actual.StatusCode);
                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains(UnitRequest.MalformedRequest, actual.Errors);
                Assert.AreEqual("(none)", actual.Details);
            }

            // 403 You do not have permission to modify this unit.
            [Test]
            public async Task UnauthorizedCannotUpdateUnit()
            {
                var req = new Unit("Freehold of Pawnee", "The Independent Free City of Pawnee", "http://pawnee.i2p", null);
                req.Id = TestEntities.Units.CityOfPawnee.Id;

                var resp = await PutAuthenticated($"units/{TestEntities.Units.CityOfPawnee.Id}", req, ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.Forbidden);
            }
            
            // 404 No unit was found with the ID provided, or the specified unit parent does not exist.
            [Test]
            public async Task CannotUpdateUnitWithInvalidParentId()
            {
                var req = new Unit(TestEntities.Units.CityOfPawnee.Name, TestEntities.Units.CityOfPawnee.Description, TestEntities.Units.CityOfPawnee.Url, TestEntities.Units.CityOfPawnee.Email);
                req.Id = TestEntities.Units.CityOfPawnee.Id;
                req.ParentId = 9999;

                var resp = await PutAuthenticated($"units/{TestEntities.Units.CityOfPawnee.Id}", req, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.NotFound);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual((int)HttpStatusCode.NotFound, actual.StatusCode);
                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains($"No parent unit found with ID ({req.ParentId}).", actual.Errors);
                Assert.AreEqual("(none)", actual.Details);
            }

            [Test]
            public async Task ChangeUnitParent()
            {
                //Create a unit to test.
                var createReq = new Unit("Test", "test", null, null, TestEntities.Units.ParksAndRecUnitId);
                var createResp = await PostAuthenticated("units", createReq, ValidAdminJwt);
                AssertStatusCode(createResp, HttpStatusCode.Created);
                var createResult = await createResp.Content.ReadAsAsync<Unit>();

                //Change the unit's parent from parks & rec to city.
                createResult.ParentId = TestEntities.Units.CityOfPawneeUnitId;
                var resp = await PutAuthenticated($"units/{createResult.Id}", createResult, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<Unit>();

                Assert.AreEqual(TestEntities.Units.CityOfPawneeUnitId, actual.ParentId);
                Assert.AreEqual(TestEntities.Units.CityOfPawneeUnitId, actual.Parent.Id);
            }

            [Test]
            public async Task UnitMembersArePreservedWhenEdited()
            {
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                var existingParksAndRecUnitMembers = db.UnitMembers
                    .Where(um => um.UnitId == TestEntities.Units.ParksAndRecUnitId)
                    .AsNoTracking()
                    .ToList();

                var req = new Unit("Changed Name", "", "", "", TestEntities.Units.ParksAndRecUnit.Parent.Id);
                
                var resp = await PutAuthenticated($"units/{TestEntities.Units.ParksAndRecUnitId}", req, ValidAdminJwt);
                
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<Unit>();

                var resultParksAndRecUnitMembers = db.UnitMembers
                    .Where(um => um.UnitId == TestEntities.Units.ParksAndRecUnitId)
                    .AsNoTracking()
                    .ToList();
                
                Assert.AreEqual(existingParksAndRecUnitMembers.Count, resultParksAndRecUnitMembers.Count);
                AssertIdsMatchContent(existingParksAndRecUnitMembers.Select(m => m.Id).ToArray(), resultParksAndRecUnitMembers);
            }
        }

        public class UnitDelete : ApiTest
        {
            [TestCase(TestEntities.Units.ParksAndRecUnitId, ValidAdminJwt, HttpStatusCode.NoContent, Description = "Admin may delete a unit that has no children.")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, ValidRswansonJwt, HttpStatusCode.Forbidden, Description = "Non-Admin cannot delete a unit.")]
            [TestCase(9999, ValidAdminJwt, HttpStatusCode.NotFound, Description = "Cannot delete a unit that does not exist.")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, ValidAdminJwt, HttpStatusCode.Conflict, Description = "Cannot delete a unit that has children.")]
            public async Task CanDeleteUnit(int unitId, string jwt, HttpStatusCode expectedCode)
            {
                var resp = await DeleteAuthenticated($"units/{unitId}", jwt);
                AssertStatusCode(resp, expectedCode);
            }                     
            
            
            [Test]
            public async Task CannotDeleteUnitWithChildren()
            {
                var resp = await DeleteAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.Conflict);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains("Unit 1 has child units, with ids: 2, 3. These must be reassigned prior to deletion.", actual.Errors);
                Assert.AreEqual("(none)", actual.Details);
            }

            [Test]
            public async Task DeleteUnitDoesNotCreateOrphans()
            {
                // Delete all the units to ensure all types of relations are exercised.
                var resp = await DeleteAuthenticated($"units/{TestEntities.Units.AuditorId}", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.NoContent);
                resp = await DeleteAuthenticated($"units/{TestEntities.Units.ParksAndRecUnitId}", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.NoContent);
                resp = await DeleteAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.NoContent);
                
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);

                // You can use this block of code to induce one of the problems we are testing for.
                /*
                var ron = db.People.Include(p => p.UnitMemberships).Single(p => p.Id.Equals(TestEntities.People.RSwansonId));
                var um = ron.UnitMemberships.First();
                ron.UnitMemberships.Remove(um);
                await db.SaveChangesAsync();
                */

                var memberTools = db.MemberTools
                    .Include(mt => mt.UnitMember)
                    .Include(mt => mt.Tool);
                
                Assert.IsEmpty(memberTools.Where(mt => mt.UnitMember == null));
                Assert.IsEmpty(memberTools.Where(mt => mt.Tool == null));

                var unitMembers = db.UnitMembers
                    .Include(um => um.Unit)
                    .Include(um => um.Person);
                
                Assert.IsEmpty(unitMembers.Where(um => um.Unit == null));
                Assert.IsEmpty(unitMembers.Where(um => um.Person == null));
                
                var supportRelationships = db.SupportRelationships
                    .Include(sr => sr.Unit)
                    .Include(sr => sr.Department);

                Assert.IsEmpty(supportRelationships.Where(um => um.Unit == null));
                Assert.IsEmpty(supportRelationships.Where(um => um.Department == null));

                var buildingRelationships = db.BuildingRelationships
                    .Include(sr => sr.Unit)
                    .Include(sr => sr.Building);

                Assert.IsEmpty(buildingRelationships.Where(um => um.Unit == null));
                Assert.IsEmpty(buildingRelationships.Where(um => um.Building == null));
            }
        }

        [TestFixture]
        public class UnitGetChildren : ApiTest
        {
            [Test]
            public async Task AuthRequired()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}/children", "bad token");
                AssertStatusCode(resp, HttpStatusCode.Unauthorized);
            }

            [Test]
            public async Task UnitMustExist()
            {
                var resp = await GetAuthenticated($"units/9999/children");
                AssertStatusCode(resp, HttpStatusCode.NotFound);
            }

            [TestCase(TestEntities.Units.CityOfPawneeUnitId, new[]{TestEntities.Units.AuditorId, TestEntities.Units.ParksAndRecUnitId})]
            [TestCase(TestEntities.Units.AuditorId, new int[0])]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, new int[0])]
            public async Task CanGetExpectedChildren(int unitId, int[] expectedChildIds)
            {
                var resp = await GetAuthenticated($"units/{unitId}/children");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<UnitResponse>>();
                AssertIdsMatchContent(expectedChildIds, actual);
                Assert.True(actual.All(a => a.ParentId == unitId));
                Assert.True(actual.All(a => a.Parent != null));
                Assert.True(actual.All(a => a.Parent.Id == unitId));
            }

            [TestCase(ValidRswansonJwt, EntityPermissions.Get, Description="As non-admin I can't create/delete units")]
            [TestCase(ValidAdminJwt, PermsGroups.All, Description="As a service admin I can create/modify/delete units")]
            [TestCase(ValidServiceAcct, EntityPermissions.Get, Description="As a service account I can get, but not create/delete units")]
            public async Task ResponseHasCorrectXUserPermissionsHeader(string jwt, EntityPermissions expectedPermissions)
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}/children", jwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, expectedPermissions);
            }
        }

        [TestFixture]
        public class UnitGetMembers : ApiTest
        {
            [Test]
            public async Task AuthRequired()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}/members", "bad token");
                AssertStatusCode(resp, HttpStatusCode.Unauthorized);
            }

            [Test]
            public async Task UnitMustExist()
            {
                var resp = await GetAuthenticated($"units/9999/members");
                AssertStatusCode(resp, HttpStatusCode.NotFound);
            }

            [TestCase(TestEntities.Units.CityOfPawneeUnitId, new []{TestEntities.UnitMembers.AdminMemberId})]
            [TestCase(TestEntities.Units.AuditorId, new []{TestEntities.UnitMembers.BWyattMemberId})]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, new []{TestEntities.UnitMembers.RSwansonLeaderId, TestEntities.UnitMembers.LkNopeSubleadId})]
            public async Task CanGetExpectedChildren(int unitId, int[] expectedMemberIds)
            {
                var resp = await GetAuthenticated($"units/{unitId}/members");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<UnitMemberResponse>>();
                AssertIdsMatchContent(expectedMemberIds, actual);
            }

            [TestCase(ValidRswansonJwt, TestEntities.Units.ParksAndRecUnitId, false, Description="Ron sees notes for unit he manages.")]
            [TestCase(ValidRswansonJwt, TestEntities.Units.AuditorId, true, Description="Ron doesn't see notes for unit he doesn't manage.")]
            [TestCase(ValidAdminJwt, TestEntities.Units.ParksAndRecUnitId, false)]
            [TestCase(ValidAdminJwt, TestEntities.Units.AuditorId, false)]
            [TestCase(ValidServiceAcct, TestEntities.Units.AuditorId, true, Description="service account since they do not manage any units.")]
            public async Task NotesAreHidden(string requestor, int unitId, bool expectNotesHidden)
            {
                var resp = await GetAuthenticated($"units/{unitId}/members", requestor);
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<UnitMemberResponse>>();
                Assert.AreEqual(expectNotesHidden, actual.All(a => string.IsNullOrWhiteSpace(a.Notes)));
            }
        }

        [TestFixture]
        public class UnitGetSupportedBuildings : ApiTest
        {
            [Test]
            public async Task AuthRequired()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}/supportedBuildings", "bad token");
                AssertStatusCode(resp, HttpStatusCode.Unauthorized);
            }

            [Test]
            public async Task UnitMustExist()
            {
                var resp = await GetAuthenticated($"units/9999/supportedBuildings");
                AssertStatusCode(resp, HttpStatusCode.NotFound);
            }

            [TestCase(TestEntities.Units.CityOfPawneeUnitId, new[]{TestEntities.BuildingRelationships.CityHallCityOfPawneeId, TestEntities.BuildingRelationships.RonsCabinCityOfPawneeId})]
            [TestCase(TestEntities.Units.AuditorId, new int[0])]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, new int[0])]
            public async Task CanGetExpectedRelationships(int unitId, int[] expectedRelationIds)
            {
                var resp = await GetAuthenticated($"units/{unitId}/supportedBuildings");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<BuildingRelationshipResponse>>();
                AssertIdsMatchContent(expectedRelationIds, actual);
                Assert.True(actual.All(a => a.UnitId == unitId));
                Assert.True(actual.All(a => a.Unit != null));
                Assert.True(actual.All(a => a.Unit.Id == unitId));
                Assert.True(actual.All(a => a.Building != null));
            }
        }


        [TestFixture]
        public class UnitGetSupportedDepartments : ApiTest
        {
            [Test]
            public async Task AuthRequired()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}/supportedDepartments", "bad token");
                AssertStatusCode(resp, HttpStatusCode.Unauthorized);
            }

            [Test]
            public async Task UnitMustExist()
            {
                var resp = await GetAuthenticated($"units/9999/supportedDepartments");
                AssertStatusCode(resp, HttpStatusCode.NotFound);
            }

            [TestCase(TestEntities.Units.ParksAndRecUnitId, new int[0])]
            [TestCase(TestEntities.Units.AuditorId, new int[0])]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, new int[]{TestEntities.SupportRelationships.ParksAndRecRelationshipId, TestEntities.SupportRelationships.PawneeUnitFireId})]
            public async Task CanGetExpectedRelationships(int unitId, int[] expectedRelationIds)
            {
                var resp = await GetAuthenticated($"units/{unitId}/supportedDepartments");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<SupportRelationshipResponse>>();
                AssertIdsMatchContent(expectedRelationIds, actual);
                Assert.True(actual.All(a => a.UnitId == unitId));
                Assert.True(actual.All(a => a.Unit != null));
                Assert.True(actual.All(a => a.Unit.Id == unitId));
                Assert.True(actual.All(a => a.Department != null));
            }
        }

        [TestFixture]
        public class UnitGetTools : ApiTest
        {
            [Test]
            public async Task AuthRequired()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}/tools", "bad token");
                AssertStatusCode(resp, HttpStatusCode.Unauthorized);
            }

            [Test]
            public async Task UnitMustExist()
            {
                var resp = await GetAuthenticated($"units/9999/tools");
                AssertStatusCode(resp, HttpStatusCode.NotFound);
            }

            [TestCase(TestEntities.Units.ParksAndRecUnitId, new[]{TestEntities.Tools.HammerId, TestEntities.Tools.SawId})]
            [TestCase(TestEntities.Units.AuditorId, new[]{TestEntities.Tools.HammerId, TestEntities.Tools.SawId})]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, new[]{TestEntities.Tools.HammerId, TestEntities.Tools.SawId})]
            public async Task CanGetExpectedTools(int unitId, int[] expectedToolIds)
            {
                var resp = await GetAuthenticated($"units/{unitId}/tools");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Tool>>();
                AssertIdsMatchContent(expectedToolIds, actual);
            }
        }
    }
}
