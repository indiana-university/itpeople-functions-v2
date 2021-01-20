using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Integration
{
    public class PeopleTests
    {
        public class GetAll : ApiTest
        {
            [Test]
            public async Task HasCorrectNumber()
            {
                var resp = await GetAuthenticated("people");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(3, actual.Count);
            }

            [TestCase("rswanso", Description="Exact match of netid")]
            [TestCase("RSWANSO", Description="Search is case-insensitive")]
            [TestCase("rSwaN", Description="Partial netid match")]
            public async Task CanSearchByNetid(string netid)
            {
                var resp = await GetAuthenticated($"people?q={netid}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual(TestEntities.People.RSwanson.Id, actual.Single().Id);
            }

            [TestCase("Ron", Description="Name match")]
            [TestCase("Ro", Description="Partial name match")]
            public async Task CanSearchByName(string netid)
            {
                var resp = await GetAuthenticated($"people?q={netid}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual(TestEntities.People.RSwanson.Id, actual.Single().Id);
            }
        }
    }
}