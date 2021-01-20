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
                var json = await resp.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<List<Person>>(json);
                Assert.AreEqual(3, actual.Count, $"\n********** Start Response JSON **********\n\n{json}\n\n*********** End Response JSON ***********\n");
            }

            [TestCase("rswanson", Description="Exact match of netid")]
            [TestCase("RSWANSON", Description="Search is case-insensitive")]
            [TestCase("rSwaN", Description="Search supports partial match")]
            public async Task CanSearchByNetid(string netid)
            {
                var resp = await GetAuthenticated($"people?q={netid}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var json = await resp.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<List<Person>>(json);
                Assert.AreEqual(1, actual.Count, $"\n********** Start Response JSON **********\n\n{json}\n\n*********** End Response JSON ***********\n");
                Assert.AreEqual("rswanson", actual.First().Netid);
            }

        }
    }
}