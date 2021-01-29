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
                var req = new Unit(ExpectedMayorsOffice.Name, ExpectedMayorsOffice.Description, ExpectedMayorsOffice.Url, ExpectedMayorsOffice.Email, ExpectedMayorsOffice.Parent);
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

            //403 unauthorized
            [Test]
            public async Task UnauthorizedCannotCreate()
            {
                var req = new Unit(ExpectedMayorsOffice.Name, ExpectedMayorsOffice.Description, ExpectedMayorsOffice.Url, ExpectedMayorsOffice.Email, ExpectedMayorsOffice.Parent);
                var resp = await PostAuthenticated("units", req, ValidRswansonJwt);
                AssertStatusCode(resp, HttpStatusCode.Unauthorized);
            }

            
            //400 The request body is malformed or missing
            [Test]
            public async Task CannotCreateMalformedUnit()
            {
                var req = new Unit("", ExpectedMayorsOffice.Description, ExpectedMayorsOffice.Url, ExpectedMayorsOffice.Email, ExpectedMayorsOffice.Parent);
                var resp = await PostAuthenticated("units", req, ValidAdminJwt);
                var actual = await resp.Content.ReadAsAsync<Error>();

                Assert.AreEqual(HttpStatusCode.BadRequest, actual.StatusCode);
                Assert.AreEqual(1, actual.Messages);
                Assert.Contains(UnitsRepository.MalformedRequest, actual.Messages.ToList());
                Assert.IsNull(actual.Exception);
            }

            //404 The specified unit parent does not exist
            [Test]
            public async Task CannotCreateUnitWithInvalidParentId()
            {
                var req = new Unit(ExpectedMayorsOffice.Name, ExpectedMayorsOffice.Description, ExpectedMayorsOffice.Url, ExpectedMayorsOffice.Email);
                req.ParentId = 9999;

                var resp = await PostAuthenticated("units", req, ValidAdminJwt);
                var actual = await resp.Content.ReadAsAsync<Error>();

                Assert.AreEqual(HttpStatusCode.NotFound, actual.StatusCode);
                Assert.AreEqual(1, actual.Messages);
                Assert.Contains(UnitsRepository.ParentNotFound, actual.Messages.ToList());
                Assert.IsNull(actual.Exception);
            }
        }
    }
}