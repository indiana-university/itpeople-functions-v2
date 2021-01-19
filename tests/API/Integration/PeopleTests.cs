using System.Collections.Generic;
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
        }
    }
}