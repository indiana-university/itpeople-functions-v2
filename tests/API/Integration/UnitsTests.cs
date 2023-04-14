using System.Collections.Generic;
using System.Net;
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

            [TestCase(UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(UnitPermissions.Owner, EntityPermissions.Get, Description = "Owner")]
            public async Task ReturnsCorrectPermissionsUnitListing(UnitPermissions providedPermission, EntityPermissions expectedPermission)
                => await GetReturnsCorrectEntityPermissions($"units", TestEntities.Units.AuditorId, providedPermission, expectedPermission);
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


            [TestCase(ValidRswansonJwt, TestEntities.Units.ParksAndRecUnitId, PermsGroups.GetPut, Description="As Ron I can get and update a unit")]
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

            [TestCase(TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, PermsGroups.GetPut, Description = "Owner")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, PermsGroups.GetPut, Description = "Owner Inheritted From Parent")]
            public async Task ReturnsCorrectPermissionsSingleUnit(int unitToCheck, int unitWithPermissions, UnitPermissions providedPermission, EntityPermissions expectedPermission)
                => await GetReturnsCorrectEntityPermissions($"units/{unitToCheck}", unitWithPermissions, providedPermission, expectedPermission);
            
            [Test]
            public async Task BegottenUnitPermissions()
            {
                // Add units 4 levels deep under Parks & Rec unit.
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                
                var childUnit = new Unit("Child", "Child of Parks & Rec", "bleh", "bleh@fake.com", TestEntities.Units.ParksAndRecUnitId);
                await db.Units.AddAsync(childUnit);
                await db.SaveChangesAsync();

                var grandChildUnit = new Unit("Grandchild", "Grandchild of Parks & Rec", "bleh", "bleh@fake.com", childUnit.Id);
                await db.Units.AddAsync(grandChildUnit);
                await db.SaveChangesAsync();

                var greatGrandChildUnit = new Unit("Great-Grandchild", "Great-Grandchild of Parks & Rec", "bleh", "bleh@fake.com", grandChildUnit.Id);
                await db.Units.AddAsync(greatGrandChildUnit);
                await db.SaveChangesAsync();

                var greatGreatGrandChildUnit = new Unit("Great-Great-Grandchild", "Great-Great-Grandchild of Parks & Rec", "bleh", "bleh@fake.com", greatGrandChildUnit.Id);
                await db.Units.AddAsync(greatGreatGrandChildUnit);
                await db.SaveChangesAsync();

                // Ron is the owner of the Parks and Rec unit, he should have the same permissions on all child units.
                var resp = await GetAuthenticated($"units/{TestEntities.Units.ParksAndRecUnitId}", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.GetPut);
                
                resp = await GetAuthenticated($"units/{childUnit.Id}", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.GetPut);

                resp = await GetAuthenticated($"units/{grandChildUnit.Id}", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.GetPut);

                resp = await GetAuthenticated($"units/{greatGrandChildUnit.Id}", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.GetPut);

                resp = await GetAuthenticated($"units/{greatGreatGrandChildUnit.Id}", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.GetPut);
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
            public async Task EditingDoesNotChangeUnitActive()
            {
                var req = new Unit("Eagleton", "Gated Community of Eagleton, Indiana", null, "hoa@eagleton.biz", null, false);
                req.Id = TestEntities.Units.CityOfPawnee.Id;

                var resp = await PutAuthenticated($"units/{TestEntities.Units.CityOfPawnee.Id}", req, ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<Unit>();

                //The Active value should not have changed
                Assert.IsTrue(actual.Active);
                //The other values should have.
                Assert.AreEqual(TestEntities.Units.CityOfPawnee.Id, actual.Id);
                Assert.AreEqual(req.Name, actual.Name);
                Assert.AreEqual(req.Description, actual.Description);
                Assert.AreEqual(req.Url, actual.Url);
                Assert.AreEqual(req.Email, actual.Email);
                Assert.IsNull(actual.Parent);
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

                // Verify the value was removed from the database
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                var actual = await db.Units.SingleOrDefaultAsync(u => u.Id == unitId);
                if(expectedCode == HttpStatusCode.NoContent || expectedCode == HttpStatusCode.NotFound)
                {
                    Assert.Null(actual);
                }
                else
                {
                    Assert.NotNull(actual);
                }

            }


            [Test]
            public async Task CannotDeleteUnitWithChildren()
            {
                var resp = await DeleteAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.Conflict);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains("Unit 1 has child units, with ids: 2, 3, 4. These must be reassigned prior to deletion.", actual.Errors);
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
                resp = await DeleteAuthenticated($"units/{TestEntities.Units.ArchivedUnitId}", ValidAdminJwt);
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

            [TestCase(ValidAdminJwt, HttpStatusCode.OK, Description = "Admin can archive a unit.")]
            [TestCase(ValidRswansonJwt, HttpStatusCode.Forbidden, Description = "Owner cannot archive a unit.")]
            [TestCase(ValidLknopeJwt, HttpStatusCode.Forbidden, Description = "Viewer cannot archive a unit.")]
            [TestCase(ValidCtraegerJwt, HttpStatusCode.Forbidden, Description = "Unassociated user cannot archive a unit.")]
            public async Task ArchiveUnit(string jwt, HttpStatusCode expectedCode)
            {
                int unitId = TestEntities.Units.ParksAndRecUnitId;
                var resp = await DeleteAuthenticated($"units/{unitId}/archive", jwt);
                AssertStatusCode(resp, expectedCode);

                var expectedActive = expectedCode == HttpStatusCode.OK ? false : true;

                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                var actual = await db.Units.SingleOrDefaultAsync(u => u.Id == unitId);

                Assert.NotNull(actual);
                Assert.AreEqual(expectedActive, actual.Active);
            }

            [TestCase(ValidAdminJwt, HttpStatusCode.OK, Description = "Admin can unarchive a unit.")]
            [TestCase(ValidRswansonJwt, HttpStatusCode.Forbidden, Description = "Owner cannot unarchive a unit.")]
            [TestCase(ValidLknopeJwt, HttpStatusCode.Forbidden, Description = "Viewer cannot unarchive a unit.")]
            [TestCase(ValidCtraegerJwt, HttpStatusCode.Forbidden, Description = "Unassociated user cannot unarchive a unit.")]
            public async Task UnarchiveUnit(string jwt, HttpStatusCode expectedCode)
            {
                int unitId = TestEntities.Units.ParksAndRecUnitId;
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);

                // Make ParksAndRecUnit inactive so we can reactivate it.
                var unit = await db.Units.SingleAsync(u => u.Id == unitId);
                unit.Active = false;
                await db.SaveChangesAsync();
                db.Entry(unit).State = EntityState.Detached;// Stop tracking the unit so we can get an updated value later

                // Request to re-active the unit
                var resp = await DeleteAuthenticated($"units/{unitId}/archive", jwt);
                AssertStatusCode(resp, expectedCode);

                var expectedActive = expectedCode == HttpStatusCode.OK ? true : false;

                var actual = await db.Units.SingleOrDefaultAsync(u => u.Id == unitId);

                Assert.NotNull(actual);
                Assert.AreEqual(expectedActive, actual.Active);
            }

            [Test]
            public async Task CannotArchiveUnitWithChildren()
            {
                var resp = await DeleteAuthenticated($"units/{TestEntities.Units.CityOfPawneeUnitId}/archive", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.Conflict);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains("Unit 1 has child units, with ids: 2, 3. These must be reassigned, deleted, or archived before this request can be completed.", actual.Errors);
                Assert.AreEqual("(none)", actual.Details);
            }

            [Test]
            public async Task CannotUnarchiveUnitWithInactiveParent()
            {
                //setup db for test having CityOfPawneeUnit and its child (ParksAndRecUnit) as inactive
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                var parentUnit = (await db.Units.SingleAsync(u => u.Id == TestEntities.Units.CityOfPawneeUnitId));
                parentUnit.Active = false;
                var childUnit = (await db.Units.SingleAsync(u => u.Id == TestEntities.Units.ParksAndRecUnitId));
                childUnit.Active = false;
                await db.SaveChangesAsync();
                db.Entry(parentUnit).State = EntityState.Detached;// Stop tracking the unit so we can get an updated value later
                db.Entry(childUnit).State = EntityState.Detached;// Stop tracking the unit so we can get an updated value later

                var resp = await DeleteAuthenticated($"units/{TestEntities.Units.ParksAndRecUnitId}/archive", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.Conflict);
                var actual = await resp.Content.ReadAsAsync<ApiError>();

                Assert.AreEqual(1, actual.Errors.Count);
                Assert.Contains("Unit 2 has a parent unit 1 that is archived. This parent unit must be unarchived before this request can be completed.", actual.Errors);
                Assert.AreEqual("(none)", actual.Details);
            }

            [Test]
            public async Task ArchiveDoesNotBreakRelationships()
            {
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                var orig = await GetUnitAndRelated(db, TestEntities.Units.ParksAndRecUnitId);
                db.Entry(orig).State = EntityState.Detached;// Stop tracking the unit so we can get an updated value later

                var resp = await DeleteAuthenticated($"units/{TestEntities.Units.ParksAndRecUnitId}/archive", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                Unit actual = await GetUnitAndRelated(db, TestEntities.Units.ParksAndRecUnitId);

                Assert.AreNotEqual(orig.Active, actual.Active);

                Assert.AreEqual(orig.ParentId, actual.ParentId);

                var am = actual.UnitMembers.Select(um => um.Id);
                var om = orig.UnitMembers.Select(um => um.Id);
                CollectionAssert.AreEqual(om, am, "Not all UnitMember relationshps are intact.");

                // Person relationships are complex, so use a lambda to compare the items in the expected and actual collections.
                // Make sure the UnitMember.Id and the person, their permission, and role have not changed.
                AssertEntityCollectionEqual(orig.UnitMembers, actual.UnitMembers, (op, ap) => op.Id == ap.Id && ap.PersonId == op.PersonId && ap.Permissions == op.Permissions && ap.Role == op.Role, "Not all UnitMembers are intact.");

                // Gather up all the tool memberships.
                // In the previous test we ensured the person in each membershp hadn't changed.  Now make sure they have all the correct tools.
                var amt = actual.UnitMembers.SelectMany(um => um.MemberTools);
                var omt = orig.UnitMembers.SelectMany(um => um.MemberTools);
                AssertEntityCollectionEqual(omt, amt, (o, e) => o.MembershipId == e.MembershipId && o.ToolId == e.ToolId, "Not all UnitMembers.MemberTools are intact.");

                // Make sure all SupportRelationshps are intact.
                AssertEntityCollectionEqual(orig.SupportRelationships, actual.SupportRelationships, (o, a) => o.Id == a.Id && o.DepartmentId == a.DepartmentId, "Not all SupportRelationships are intact.");

                // Make sure all BuildingRelationships are intact.
                AssertEntityCollectionEqual(orig.BuildingRelationships, actual.BuildingRelationships, (o, a) => o.Id == a.Id && o.BuildingId == a.BuildingId, "Not all BuildingRelationships are intact.");
            }

            private static async Task<Unit> GetUnitAndRelated(Database.PeopleContext db, int unitId)
            {

                //Ensure the relationships for the unit are still in place after it became inactive.
                return await db.Units
                    .Include(u => u.Parent)
                    .Include(u => u.UnitMembers).ThenInclude(um => um.Person)
                    .Include(u => u.UnitMembers).ThenInclude(um => um.MemberTools)
                    .Include(u => u.SupportRelationships).ThenInclude(sr => sr.Department)
                    .Include(u => u.BuildingRelationships).ThenInclude(sr => sr.Building)
                    .SingleAsync(u => u.Id == unitId);
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

            [TestCase(TestEntities.Units.CityOfPawneeUnitId, new[]{TestEntities.Units.AuditorId, TestEntities.Units.ParksAndRecUnitId, TestEntities.Units.ArchivedUnitId})]
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

            /* More cases of non-admins not being able to PUT POST DELETE child units. */
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, EntityPermissions.Get, Description = "Owner")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, EntityPermissions.Get, Description = "Owner Inheritted From Parent")]
            public async Task ReturnsCorrectPermissionsUnitChildren(int unitWithPermissions, UnitPermissions providedPermission, EntityPermissions expectedPermission)
            {
                // Add a child unit to Parks & Rec and test it.  This is less painfull than adding another test entity.
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                var childUnit = new Unit
                {
                    Name = "Parks and Rec Maintenance",
                    Description = "The folks who do actual work",
                    Url = "http://pawneeindiana.com/parks-and-recreation-maintenance/",
                    Email = "maintenance@example.com",
                    ParentId = TestEntities.Units.ParksAndRecUnitId
                };
                await db.Units.AddAsync(childUnit);
                await db.SaveChangesAsync();

                await GetReturnsCorrectEntityPermissions($"units/{TestEntities.Units.ParksAndRecUnitId}/children", unitWithPermissions, providedPermission, expectedPermission);
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
            public async Task CanGetExpectedMembers(int unitId, int[] expectedMemberIds)
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

            [TestCase(UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(UnitPermissions.Owner, EntityPermissions.Get, Description = "Owner")]
            public async Task ReturnsCorrectPermissionsWhenUnitRetired(UnitPermissions providedPermission, EntityPermissions expectedPermission)
			{
                await GetReturnsCorrectEntityPermissions($"units/{TestEntities.Units.ArchivedUnitId}/members", TestEntities.Units.ArchivedUnitId, providedPermission, expectedPermission);
            }

            [Test]
            public async Task ReturnsCorrectPermissionsWhenUnitRetiredForServiceAdmin()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.ArchivedUnitId}/members", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);
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
            [TestCase(TestEntities.Units.ParksAndRecUnitId, new int[]{TestEntities.BuildingRelationships.SmallParkParksandRecId})]
            public async Task CanGetExpectedBuildingRelationships(int unitId, int[] expectedRelationIds)
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

            [TestCase(UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(UnitPermissions.Owner, EntityPermissions.Get, Description = "Owner")]
            public async Task ReturnsCorrectPermissionsWhenUnitRetired(UnitPermissions providedPermission, EntityPermissions expectedPermission)
            {
                await GetReturnsCorrectEntityPermissions($"units/{TestEntities.Units.ArchivedUnitId}/supportedBuildings", TestEntities.Units.ArchivedUnitId, providedPermission, expectedPermission);
            }

            [Test]
            public async Task ReturnsCorrectPermissionsWhenUnitRetiredForServiceAdmin()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.ArchivedUnitId}/supportedBuildings", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);
            }

            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, PermsGroups.All, Description = "Owner")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, PermsGroups.All, Description = "Owner Inheritted From Parent")]
            public async Task ReturnsCorrectPermissionsUnitBuildingRelationship(int unitWithPermissions, UnitPermissions providedPermission, EntityPermissions expectedPermission)
                => await GetReturnsCorrectEntityPermissions($"units/{TestEntities.Units.ParksAndRecUnitId}/supportedBuildings", unitWithPermissions, providedPermission, expectedPermission);
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
                Assert.True(actual.All(a => a.SupportType != null));
            }

            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(TestEntities.Units.ParksAndRecUnitId, UnitPermissions.Owner, PermsGroups.All, Description = "Owner")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember Inheritted From Parent")]
            [TestCase(TestEntities.Units.CityOfPawneeUnitId, UnitPermissions.Owner, PermsGroups.All, Description = "Owner Inheritted From Parent")]
            public async Task ReturnsCorrectPermissionsUnitSupportRelationships(int unitWithPermissions, UnitPermissions providedPermission, EntityPermissions expectedPermission)
            {
                // Add a supported department to Parks & Rec and test it.  This is less painfull than adding another test entity.
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                var supportedDepartment = new Department
                {
                    Name = "Department of Redundancy Department",
                    Description = "If something is worth doing..."
                };
                await db.Departments.AddAsync(supportedDepartment);
                var relationship = new SupportRelationship
                {
                    UnitId = TestEntities.Units.ParksAndRecUnitId,
                    Department = supportedDepartment,
                    SupportTypeId = TestEntities.SupportTypes.FullServiceId
                };
                await db.SupportRelationships.AddAsync(relationship);
                await db.SaveChangesAsync();

                await GetReturnsCorrectEntityPermissions($"units/{TestEntities.Units.ParksAndRecUnitId}/supportedDepartments", unitWithPermissions, providedPermission, expectedPermission);
            }

            [TestCase(UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(UnitPermissions.Owner, EntityPermissions.Get, Description = "Owner")]
            public async Task ReturnsCorrectPermissionsWhenUnitRetired(UnitPermissions providedPermission, EntityPermissions expectedPermission)
            {
                await GetReturnsCorrectEntityPermissions($"units/{TestEntities.Units.ArchivedUnitId}/supportedDepartments", TestEntities.Units.ArchivedUnitId, providedPermission, expectedPermission);
            }

            [Test]
            public async Task ReturnsCorrectPermissionsWhenUnitRetiredForServiceAdmin()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.ArchivedUnitId}/supportedDepartments", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);
            }
        }

        [TestFixture]
        public class UnitGetTools : ApiTest
        {
			[TestCase(ValidLknopeJwt, EntityPermissions.Get, Description = "As non-admin I can't create/delete member tools")]
			[TestCase(ValidAdminJwt, PermsGroups.All, Description = "As a service admin I can create/modify/delete member tools")]
			[TestCase(ValidBwyattJwt, PermsGroups.All, Description = "With manage tools permission, I can create/modify/delete member tools")]

			public async Task ResponseHasCorrectXUserPermissionsHeader(string jwt, EntityPermissions expectedPermissions)
			{
				var resp = await GetAuthenticated($"units/3/tools", jwt);
				AssertPermissions(resp, expectedPermissions);
			}

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

            [TestCase(UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(UnitPermissions.ManageTools, EntityPermissions.Get, Description = "ManageTools")]
            [TestCase(UnitPermissions.ManageMembers, EntityPermissions.Get, Description = "ManageMember")]
            [TestCase(UnitPermissions.Owner, EntityPermissions.Get, Description = "Owner")]
            public async Task ReturnsCorrectPermissionsWhenUnitRetired(UnitPermissions providedPermission, EntityPermissions expectedPermission)
            {
                await GetReturnsCorrectEntityPermissions($"units/{TestEntities.Units.ArchivedUnitId}/tools", TestEntities.Units.ArchivedUnitId, providedPermission, expectedPermission);
            }

            [Test]
            public async Task ReturnsCorrectPermissionsWhenUnitRetiredForServiceAdmin()
            {
                var resp = await GetAuthenticated($"units/{TestEntities.Units.ArchivedUnitId}/tools", ValidAdminJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);
            }

            [TestCase(UnitPermissions.Viewer, EntityPermissions.Get, Description = "Viewer")]
            [TestCase(UnitPermissions.ManageTools, PermsGroups.All, Description = "ManageTools")]
            [TestCase(UnitPermissions.ManageMembers, PermsGroups.All, Description = "ManageMember")]
            [TestCase(UnitPermissions.Owner, PermsGroups.All, Description = "Owner")]
            public async Task ReturnsCorrectPermissionsForUnitPermissions(UnitPermissions providedPermission, EntityPermissions expectedPermission)
                => await GetReturnsCorrectEntityPermissions($"units/{TestEntities.Units.AuditorId}/tools", TestEntities.Units.AuditorId, providedPermission, expectedPermission);
            
            [Test]
            public async Task BegottenUnitToolsPermissions()
            {
                // Add units 4 levels deep under Parks & Rec unit.
                var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
                
                var childUnit = new Unit("Child", "Child of Parks & Rec", "bleh", "bleh@fake.com", TestEntities.Units.ParksAndRecUnitId);
                await db.Units.AddAsync(childUnit);
                await db.SaveChangesAsync();

                var grandChildUnit = new Unit("Grandchild", "Grandchild of Parks & Rec", "bleh", "bleh@fake.com", childUnit.Id);
                await db.Units.AddAsync(grandChildUnit);
                await db.SaveChangesAsync();

                var greatGrandChildUnit = new Unit("Great-Grandchild", "Great-Grandchild of Parks & Rec", "bleh", "bleh@fake.com", grandChildUnit.Id);
                await db.Units.AddAsync(greatGrandChildUnit);
                await db.SaveChangesAsync();

                var greatGreatGrandChildUnit = new Unit("Great-Great-Grandchild", "Great-Great-Grandchild of Parks & Rec", "bleh", "bleh@fake.com", greatGrandChildUnit.Id);
                await db.Units.AddAsync(greatGreatGrandChildUnit);
                await db.SaveChangesAsync();

                // Ron is the owner of the Parks and Rec unit, he should have the same permissions on all child units.
                var resp = await GetAuthenticated($"units/{TestEntities.Units.ParksAndRecUnitId}/tools", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);
                
                resp = await GetAuthenticated($"units/{childUnit.Id}/tools", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);

                resp = await GetAuthenticated($"units/{grandChildUnit.Id}/tools", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);

                resp = await GetAuthenticated($"units/{greatGrandChildUnit.Id}/tools", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);

                resp = await GetAuthenticated($"units/{greatGreatGrandChildUnit.Id}/tools", ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.OK);
                AssertPermissions(resp, PermsGroups.All);
            }
        }
    }
}
