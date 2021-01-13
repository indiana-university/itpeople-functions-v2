using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
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
        }
    }

    public class HealthcheckTests : ApiTest
    {
        [Test]
        public async Task Ping()
        {
            var resp = await Http.GetAsync("ping");
            AssertStatusCode(resp, HttpStatusCode.OK);
            await AssertStringContent(resp, "Pong!");
        }
    }
}