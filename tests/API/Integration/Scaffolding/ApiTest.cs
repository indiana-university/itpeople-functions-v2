using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Integration
{
    public abstract class ApiTest
    {
        // valid from 1/1/2000 - 1/19/2038 💥
        public static string ValidJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiamhvZXJyIiwiZXhwIjoyMTQ3NDgzNjQ4fQ.ihv-eghNulIFiHRg7pvw48fel-w0vlhuaFEd2LtfgWyjy6S61tXxab2ewWWjzTmNLoKV7RhP5s47_I46RYO_pb38k68JjED-M86R6tcunMNYxCo45siLRUOLopl0TtDuuInvKQXOthKW2S82xSfesGbgfWxVOE1ihZ0Tp3YQ76fTINRwnE4DRB3nRHtr_a_FP0z8GJImdxFtjZb6gRS0IAqA1EXfxkgFG8Gy_tW7U_KZkhvseQ0OPVJojGti3Ll6dd5wsUius8z2bjyDLwyK79H88Ab3ksMurl-4QDC_l9BjWCrJ7IYdqrrCguQgk6w_Qp_TuRiZ-rzb4-Mv1DTQjA";

        [SetUp]
        public void Init()
        {
            Harness.DbContainer.ResetDatabase();
        }

        protected static HttpClient Http = new HttpClient(){
            BaseAddress = new System.Uri("http://localhost:8080/")
        };

        public Task<HttpResponseMessage> GetAuthenticated(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", ValidJwt);
            return Http.SendAsync(request);
        }

        protected static void AssertStatusCode(HttpResponseMessage resp, HttpStatusCode expected)
        {
            string content = "(none)";
            if (expected != resp.StatusCode)
            {
                try 
                {
                    content = resp.Content.ReadAsStringAsync().Result;
                } 
                catch {
                    content = "Failed to parse response content.";
                }
            }
            Assert.AreEqual(expected, resp.StatusCode, content);
        }

        protected static void AssertIdsMatchContent<T>(int[] expectedIds, IEnumerable<T> content) where T: Entity
        {
            CollectionAssert.AreEquivalent(expectedIds, content.Select(c => c.Id));
        }
    }
}