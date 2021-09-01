using System;
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
        public const string ValidRswansonJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoicnN3YW5zbyIsImV4cCI6MjE0NzQ4MzY0OH0.WdfqZFA78p9-R0AQdL4tFA8_mPMlsivWZalK4CyNQBaLiSec4sCZ7f_AzVcMZMG9rn1dXlwkHghxqOVP2d66WxVu4-7Ho1n5ZuSHVHL15vmHaAe_oMZDeyxR6WXfM06xjXm5BDImkGJ-soowQYkqxLcVSG_pnavd24xT9-KgglF_qv9d6h0cqS5m1pk4Aml0aVYA0u9PTThQ2Lxz2tqvJXepUbkS1kZaATSL6Vim5lRyw89xGybgzH4Ins4SaAqAkBEkuVPSPoskniaqo74uJr31Z_EwREFblbE5c6nmiexmBwbazHAe3xgVNONBNZ3c85-Zk9sBrxnKyUvQHioQkQ";
        public const string ValidAdminJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiam9obmRvZSIsImV4cCI6MjE0NzQ4MzY0OH0.Yu5I3mKSfbK6xDDMsfUCO4S0HGfcuPnKveAVYk2b5bTK-znl99Ua4T2fynuoEMBkcHMy6udUMUcEWE0G8LD0ja5LSgaXAMretZzkrHzxsdBp05pJP51rPA7Ok1o2d9oZQu8nq4PotPR_kumDPk2x9HzQkDUInUBu9v3r_8-u8ikaMn4TVLIKw_P9x9mSwWg3wl85MKyun1xRiw5nC0DyZP39WUw14wkCS9I616PC4OwL1wiwMCDaoMxtg29DNqWasUsFXxHy4dfC5iRZC69I30tH49tn99vSrH9Kobiq5KnUAlP8ojabap2_sT_UVWQheS9zgvl_xYD2z9HU-5xvwg";
        public const string ValidLknopeJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoibGtub3BlIiwiZXhwIjoyMTQ3NDgzNjQ4fQ.juIkziH7BiRwVCWLYBrDIh28UeSpj4yrukzfy4E4AGWHGdOxbd1jQfe9bj7nr5mirEB0Zy0DMo-Yumut5pYRp1PGW9IhBkMIMjxSxxQl-XT4oXJsyy3JeLnmibmoPogeTyDrp_Q5WDbJWx6TzgQEsgEoJSWtWmjr3XpcL8IR4xMbcs-kaBrDGIhcJvjJsYhyOBLVKQyP3ji9N6aP6WPtnLdO67LZ4kigrHFjryKJ3e-sVtuMVPX2j_UZxkdAVZnFlAcksLzDdxXcXT7-4EXUshkBzO5fXMXE6BYI1sHdKKJJZYBXh5YIBkU2dfhATFBv9U3jkCGuFbN2xnbYN5BN5A";
        public const string ValidBwyattJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiYnd5YXR0IiwiZXhwIjoyMTQ3NDgzNjQ4fQ.TpTdpChOHR5gNXZwwPD02qeaTXFeIK3xq8pT1g6WREL1AgAs-uWc2TZeC44x1AD2rBeXWmCzHdkwPLfUJwa19jBu44lSu0BSeYKSL3fvf6RAvb0Hsf8zU_z-_qBo7js-ydXrRdnwKh3O5SwYq1nGGcDNgOU03f857elR_LPwvLt_KckOO0UYOHRHwBB51CkHtEez9QkbyaJdlxBko52w0jb4bLWrAA4QOfOrypTE6oeN8vFG1z0sd3rCh1yuU-6yc7aXHVZ5HmmzLXUSE3HJVsoPTwi4pG78gB_jqR0DSE8rHvfPz4m2kkzIZZdt2g11NcHSKeaUo3r4LFupQ2s4eg";
        public const string ValidCtraegerJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiY3RyYWVnZXIiLCJleHAiOjIxNDc0ODM2NDh9.hj0v52e6c3Q-1A9EE9cJMJ8Hm4zrAbVb2PCwBSF7uS-RkbIC26RBdkllP5Rb2GDo32U8dGisdiXzFcqtMhuHFfootJz8JDCzgcA6_t6ibc3NFq9CkqqWcjjlSPckQ8HPSGCmVsNsXPY6hk_aZLtuPZ_YqdeaTEicKr_hyu7rehoJNhimoi2iVKqOo9VYHhenrfkVcniZmREflvNMlbFijUlozjjFQgkycwGRflnIyaUXg50w4z-8QZEqonvnRbrbfJB8AcYE65MWL4lpklKATP1eYI9uNW0Irxcv5Hh7LB1N6RVPFgSfNCGAjpR4c__5O2_ogFMe2iTqIj5ax8QE9A";
        public const string ValidServiceAcct = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwiZXhwIjoyMTQ3NDgzNjQ4LCJzY29wZSI6WyJyZWFkIl0sImNsaWVudF9pZCI6InVpdHN3ZWIifQ.B7erbZUyovd4Pdxw8zjV2DVXzajOYRWG4Y9fLfY61gbWv36HuHqmOTPRC4PkTLAcGabGrjpwzf6JwLsYfUyw1AFH5lsIAp-X0zJ6_sZ8slmKOAqaVfnxol1jYncY_a9JWSbbOSCIzdt-C4j3EPTaI5kHOK0Jr0fbs0uJUGD8aM_7Pd-wFqAGz5QpeAcNgWz9pVYNVrOmy7Q6dgHnIEojtHxf5c2HiOmArzDqcAwZFvP2AGVH_nVKWA72a7ITLqlYfr982xUms5glI_ldCREH6_ajG3UZ2xLiegyiPoH_moL2QY7ED53zX3qiw6XSSJxX1H9TXcGMDDXwhuFgNTYcRw";
        
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

        public static void AssertEntityCollectionEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<T, T, bool> areEqual, string errorMessage) where T : Entity
        {
            var missingExpectedItemIds = expected
                .Where(exp => false == actual.Any(act => areEqual(exp, act)))
                .Select(r => r.Id)
                .ToList();
            var missingActualIds = actual
                .Where(act => false == expected.Any(exp => areEqual(exp, act)))
                .Select(r => r.Id)
                .ToList();

            var error = errorMessage;
            if(missingExpectedItemIds.Count > 0)
            {
                error = $"\n\tExpected Ids ({string.Join(", ", missingExpectedItemIds)}) had no exact match in Actual.";
            }

            if(missingActualIds.Count > 0)
            {
                error += $"\n\tActual Ids ({string.Join(", ", missingActualIds)}) had no exact match in Expected.";
            }

            if(missingExpectedItemIds.Count > 0 || missingActualIds.Count > 0)
            {
                Assert.Fail(error);
            }
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