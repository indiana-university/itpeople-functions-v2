using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Models;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Integration
{
    public abstract class ApiTest
    {
        // valid from 1/1/2000 - 1/19/2038 💥
        public const string ValidRswansonJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoicnN3YW5zb24iLCJleHAiOjIxNDc0ODM2NDh9.HFziMgrIblB2dwn3po6D_C0zLCCHgPRwn7LcZ7i0K24ihdXtNGwlhOaens5Z97D0mS1qSYpK5oiqNsivaUNJGq6jQMWV6Rgbxddaid2H4PcrZOCIFRLqkmgl_Wyk2TTlDWtKXruMQEPS_hbEttDFP0Dr6Ii2x5KQTupqYspaKqdXggXjrV4GAk22x5Zz5KSKrZSmvFNCRciaCrqycPGpLrPlBG_CTztdIF_ycWVOsuNPKQr9ds80T3xXO87pP2x1W3AAO_d5UYCLRBLxhjzoDO6OndVm5LG9xHpZMRyUADbN2MjV1a7XJECSuHrPxOwV6DXZ-74W4Tl7n_6N0jYEhg";

        [SetUp]
        public void Init()
        {
            Harness.DbContainer.ResetDatabase();
        }

        protected static HttpClient Http = new HttpClient(){
            BaseAddress = new System.Uri("http://localhost:8080/")
        };

        public Task<HttpResponseMessage> GetAuthenticated(string url, string token = ValidRswansonJwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", ValidRswansonJwt);
            return Http.SendAsync(request);
        }

        public Task<HttpResponseMessage> PutAuthenticated(string url, object body, string token = ValidRswansonJwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", ValidRswansonJwt);
            request.Content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
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