using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Models;
using Models.Enums;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Integration
{
	public abstract class ApiTest
    {
        // valid from 1/1/2000 - 1/19/2038 💥
        public const string ValidRswansonJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoicnN3YW5zb24iLCJleHAiOjIxNDc0ODM2NDh9.HFziMgrIblB2dwn3po6D_C0zLCCHgPRwn7LcZ7i0K24ihdXtNGwlhOaens5Z97D0mS1qSYpK5oiqNsivaUNJGq6jQMWV6Rgbxddaid2H4PcrZOCIFRLqkmgl_Wyk2TTlDWtKXruMQEPS_hbEttDFP0Dr6Ii2x5KQTupqYspaKqdXggXjrV4GAk22x5Zz5KSKrZSmvFNCRciaCrqycPGpLrPlBG_CTztdIF_ycWVOsuNPKQr9ds80T3xXO87pP2x1W3AAO_d5UYCLRBLxhjzoDO6OndVm5LG9xHpZMRyUADbN2MjV1a7XJECSuHrPxOwV6DXZ-74W4Tl7n_6N0jYEhg";
        public const string ValidAdminJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiYWRtaW4iLCJleHAiOjIxNDc0ODM2NDh9.OtlelEWHpp_Ybr3uyIKSx-ka3OCgbb-z48Zcapbg0_JFbGmtrtK3tcuDbn1l4_O9q1j-6-t9rV1BC-3qkiZiB_ES4KmpDcmFPQGuQr3zIl5wfLV1TUkeZGC6bfODSBHQIyVvPgSIjFJJbr3akRbVfjQe5j2cpyOHMumy2rOiHZd7YWWi9P4SmJTk9fbnZkcQ7lPpSXNG3S9c8ysGqlqY-I7a-oMBxjsk0a9udHTqvvDou_jwY6ot8LQyNwocMdSn5xiWhZC-rLlwFDfE1VUtUXmwGADrGEufvRMJ7rR_UM12vMud2JMtgrQpTrm9ym_UjaXu6V4SY3kxIOSHJuuXbw";

        [SetUp]
        public async Task Init()
        {
            var resp = await StateServer.GetAsync("state");
            AssertStatusCode(resp, HttpStatusCode.OK);
        }
        

        protected static HttpClient Http = new HttpClient(){
            BaseAddress = new System.Uri("http://localhost:8080/")
        };

        protected static HttpClient StateServer = new HttpClient(){
            BaseAddress = new System.Uri("http://localhost:8081/")
        };

        public Task<HttpResponseMessage> GetAnonymous(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return Http.SendAsync(request);
        }

        public Task<HttpResponseMessage> PostAuthenticated(string url, object body, string token = ValidRswansonJwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
            request.Content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
            return Http.SendAsync(request);
        }

        public Task<HttpResponseMessage> GetAuthenticated(string url, string token = ValidRswansonJwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
            return Http.SendAsync(request);
        }

        public Task<HttpResponseMessage> PutAuthenticated(string url, object body, string token = ValidRswansonJwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
            request.Content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
            return Http.SendAsync(request);
        }

        public Task<HttpResponseMessage> DeleteAuthenticated(string url, string token = ValidRswansonJwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
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

        protected static void AssertPermissions(HttpResponseMessage resp, EntityPermissions expectedPermissions)
        {
            var actualHeader = resp.Headers.SingleOrDefault(h => h.Key == "x-user-permissions");
            Assert.NotNull(actualHeader, "Permissions header is not present");
            Assert.AreEqual(1, actualHeader.Value.Count(), "Permissions header should have one value");
            Assert.AreEqual(expectedPermissions.ToString(), actualHeader.Value.Single());
        }

        protected static void AssertIdsMatchContent<T>(int[] expectedIds, IEnumerable<T> content) where T: Entity
        {
            CollectionAssert.AreEquivalent(expectedIds, content.Select(c => c.Id));
        }
    }

    public static class HttpContentExtensions
    {
        public static async Task<T> ReadAsAsync<T>(this HttpContent content)
        {
            var str = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(str, Json.JsonSerializerSettings);
        }
    }
}