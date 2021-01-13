using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Integration
{
    public abstract class ApiTest
    {
        protected static HttpClient Http = new HttpClient(){
            BaseAddress = new System.Uri("http://localhost:8080/api/")
        };

        protected static void AssertStatusCode(HttpResponseMessage resp, HttpStatusCode expected)
        {
            Assert.AreEqual(expected, resp.StatusCode);
        }

        protected static async Task AssertStringContent(HttpResponseMessage resp, string expected)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Assert.AreEqual(expected, body);
        }

    }
}