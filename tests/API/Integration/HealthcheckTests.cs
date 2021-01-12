using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Integration
{

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