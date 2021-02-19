using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Linq;
using API.Middleware;
using Newtonsoft.Json;
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
            [TestCase(ValidAdminJwt, EntityPermissions.All, Description="As a service admin I can create/modify/delete units")]
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

            [TestCase(ValidRswansonJwt, TestEntities.Units.ParksAndRecUnitId, EntityPermissions.GetPut, Description="As Ron I can a unit I manage")]
            [TestCase(ValidRswansonJwt, TestEntities.Units.CityOfPawneeUnitId, EntityPermissions.Get, Description="As Ron I can't update a unit I don't manage")]
            [TestCase(ValidAdminJwt, TestEntities.Units.ParksAndRecUnitId, EntityPermissions.All, Description="As a service admin I can do anything to any unit")]
            [TestCase(ValidAdminJwt, TestEntities.Units.CityOfPawneeUnitId, EntityPermissions.All, Description="As a service admin I can do anything to any unit")]
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
                Assert.AreEqual("/units/4", resp.Headers.Location.OriginalString);
                var actual = await resp.Content.ReadAsAsync<Unit>();

                Assert.NotZero(actual.Id);
                Assert.AreEqual(req.Name, actual.Name);
                Assert.AreEqual(req.Description, actual.Description);
                Assert.AreEqual(req.Url, actual.Url);
                Assert.AreEqual(req.Email, actual.Email);
                Assert.NotNull(actual.Parent);
                Assert.AreEqual(req.ParentId, actual.Parent.Id);
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
                System.Environment.SetEnvironmentVariable("DatabaseConnectionString", Database.PeopleContext.LocalDatabaseConnectionString);
                var db = Database.PeopleContext.Create();
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
                var resp = await DeleteAuthenticated($"units/{TestEntities.Units.ParksAndRecUnitId}", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.NoContent);
                
                System.Environment.SetEnvironmentVariable("DatabaseConnectionString", Database.PeopleContext.LocalDatabaseConnectionString);
                var db = Database.PeopleContext.Create();

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
            }
        }
    }
}