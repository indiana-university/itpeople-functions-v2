using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Linq;

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
        }
    }
}