using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Integration
{
    [TestFixture]
    [Category("Documentation")]
    public class DocumentationTests : ApiTest
    {
        [Test]
        public async Task CanGenerateApiDocs()
        {
            var resp = await Http.GetAsync("openapi.json");
            AssertStatusCode(resp, HttpStatusCode.OK);            
        }
    }
}