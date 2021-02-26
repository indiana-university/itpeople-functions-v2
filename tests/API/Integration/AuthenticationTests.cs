using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;

namespace Integration
{
    public class AuthenticationTests : ApiTest
    {
        
        
        // expired 1/1/2000
        private static string ExpiredJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDcwMCwidXNlcl9uYW1lIjoiamhvZXJyIiwiZXhwIjo5NDY2ODQ4MDB9.CpjTHlt_wMTtoB79EsLEZSQRw8wNkIaLL_I3dFf3GAFuosQdAd8PkAxbDzJBki-pTEWCLvxH8d2zxDPn4RYzKW2whMMrFrG6q49-aHwlwHAP22Ku60nU4JcqmiTPJ99i5d0XTvNfU1y2EMfQ2uiJ9fbd46FifgrY4faqzOUrWlchcLNv4GGK_nB4BP7PY2xh0SntHlbrEpNdzG_6NJAlo0JurGohSBj_DqWfM8geE0pxZfXEwQnTjJZ1wAViLGbMii_NFfhFQ94G7Y1gGeH357T8584THJCcnzCxmLqSUUn1O0_L21m49438598faPrWkG9ATgOPc14AklTfIf07qA";

        // not valid until 1/19/2038
        private static string UnripeJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjIxNDc0ODM2NDgsInVzZXJfbmFtZSI6Impob2VyciIsImV4cCI6MjE0NzQ4MzcwMH0.R7HWbH7noMZSqxGUKo4dwAd_xu6XrkOhK5UkwHVxk3lBl_m7hsM9nI8siXXV3cuo4eqjRLfnqtxf3DbQnOSxC8cEFpOJ-pdikPEdZGWxtz4b4JLWG4szVKr_C9TXRxiCUk9HhkRHsSCrUoXFDQlYB4GzluJIFFHQHTw6GYIRcFWN5prTgrcqtDn5f0jJSXdLBHD7D9WOw_EzOEsVdGhD2_ier1MCljLWPLiCj37wSh2qOvzUPlyMbbvk1r73fc6ykg8zbquQgC93RYaCCaSqqb3S_Yh6YaLYcxnywJ7_t-tgHNn30vuz7GbaTpZwiZXYkd9b9u_BW6b7b7U2D16e9w";
        private static string AnotherIssuerJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiamhvZXJyIiwiZXhwIjoyMTQ3NDgzNjQ4fQ.DRiuXgovE7RDVJQtxVMQkrjXW89Wg3SF__r2qhN4ZJ3z4_dDSXhcK4EglE-quXS8MZBx1sGwAvMCvmTEVhYlFN3AcMfo3gv-8Ajcc-7jFXyjI_usFdjjjnyfyyYQMtyCprsd9PTB1g1xlvJjK4EDHyxoIAitTy3oeyvMJFBgenB6tVSXn76_8UV4lxf5xlL8NjMutZABb7Bj1TDVt2vkwZlUN34SjqgYPIdR8rmxze4515yoHv6QmQzqlG4SbJm8lHLXn8AK-FcGbOBOtbhX9ZN71t3Glf_WlzvGhrSb0ZXH7cFlFjyl_cp06iPjz6-9hmojVXmerOwQ-ouwSANj8A";

        [Test]
        public async Task AuthorizationHeaderRequired()
        {
            var resp = await Http.GetAsync("people");
            AssertStatusCode(resp, HttpStatusCode.BadRequest);
            var actual = await resp.Content.ReadAsAsync<ApiError>();
            Assert.AreEqual(actual.Errors.First(), "Request is missing an Authorization header.");
        }

        [Test]
        public async Task AuthorizationHeaderMustHaveBearerScheme()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "people");
            request.Headers.Authorization = new AuthenticationHeaderValue("some_scheme");
            var resp = await Http.SendAsync(request);
            AssertStatusCode(resp, HttpStatusCode.BadRequest);
            var actual = await resp.Content.ReadAsAsync<ApiError>();
            Assert.AreEqual(actual.Errors.First(), "Request Authorization header scheme must be \"Bearer\"");
        }

        [TestCase("bearer", Description="scheme should be case-insensitive (lowercase)")]
        [TestCase("Bearer", Description="scheme should be case-insensitive (uppercase)")]
        public async Task AuthorizationHeaderMustHaveBearerToken(string scheme)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "people");
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme);
            var resp = await Http.SendAsync(request);
            AssertStatusCode(resp, HttpStatusCode.BadRequest);
            var actual = await resp.Content.ReadAsAsync<ApiError>();
            Assert.AreEqual(actual.Errors.First(), "Request Authorization header contains empty \"Bearer\" token.");
        }

        [Test]
        public async Task AuthorizationHeaderMustHaveProperlyFormedJwt()
        {
            await AssertBadToken("malformed jwt");
        }

        [Test]
        public async Task ExpiredJwtRejected()
        {
            await AssertBadToken(ExpiredJwt);
        }

        [Test]
        public async Task UnripeJwtRejected()
        {
            await AssertBadToken(UnripeJwt);
        }

        [Test]
        public async Task JwtFromAnotherIssuerRejected()
        {
            await AssertBadToken(AnotherIssuerJwt);
        }

        private static async Task AssertBadToken(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "people");
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
            var resp = await Http.SendAsync(request);
            AssertStatusCode(resp, HttpStatusCode.Unauthorized);
        }
    
    }
}