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
        public const string ValidRswansonJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoicnN3YW5zbyIsImV4cCI6MjE0NzQ4MzY0OH0.lrXQNXZ3eBSZ2CT3AHwxcEKCVqCpe2bO3kJJvnbgoHzbmDsKn_Lt1XqB98ujRY6f7Kfv0e04qokCfNVZRaMqbeJ-1D08XsmYzEXzw6gY2C8eHHMM-969xkxEWSV_tMH_4cMOC1cpFqOjbI1SpRyC2qD3ZKcwpRwGw2-k3omrQOWxdJL6oW63lk6QtTKgy-Ehblwu_t7kr6vbI6BIRliTBcRsrKlNypgri39-an0ehbKRK6EHeOlWV1Z5D0JmjaAMrpD--tWEFUuqE__mnwnNTm6uiVIppZX7P1AGDiWVykRL3ffHFSItzn_LjlffpmPIAZktgrSfLU81673NVpWnSw";
        public const string ValidAdminJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiam9obmRvZSIsImV4cCI6MjE0NzQ4MzY0OH0.UzxAU42jriBusgHzTXAfeT3y10irIrZZ1Mx9x8zafd7VZi3_hgnWXF93cBz0nBJhGwkzBssVDyCqGfhUtwwaG8DNpWKI5Ulo0A5vVYDmn6oNjKJd58oY9Adl9sRCbeK2mcz1jWZztIGCVMoE54D1jy7qA5VeK6o7f_IkRYuDYUYX_i9cKDjOZT953o17oYko1WeHh832ZBGpcLWgZssa2SSZsAyZevz74dq8gRfvhP9n2pDt45r6ZjjddEjWggWYqpWKLw88i20NfNiaKxGmRZ_RLJuQCuJb1ZjgZpv1fzUxAXSCuHlIUpW_Fy-ct0ElXp2yJS6ORCk0-EkbL7-GbA";
        public const string ValidLknopeJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoibGtub3BlIiwiZXhwIjoyMTQ3NDgzNjQ4fQ.tCPsxFSXUXRTIHUwN7H9zE3TtzncY8nJmRBLz-kiyAbx9WKokCOTzMRGRwxOmySPaMjs2HhvKW8LUFVIyPjRqqaIdQEzDD5QuZ7AhLsPHPFsvBOLnRuKy8_ySvSnXwl4l7uUSXjuFEruV7M93ZjzpkAgFPPMWPJO-axoFb4Z6Fecg8dzkefWmaPS175ZYHrJhwP2tUlhPuDuFy6hvM2u6Zg5qSspkGBo9Shzc9zT7pwn_R3nCRFkh62bso3YRyurfnrWyfOBYKkbwKeJTHWvfu_75Ik8Mva-rk4a24GdnbfIdOOr_wcNNTb37nQYo0VCORCHJAumVjOj0GB8qoeaKw";
        public const string ValidBwyattJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiYnd5YXR0IiwiZXhwIjoyMTQ3NDgzNjQ4fQ.AGGknbG4dY6wqM3EekowxBSOiKBtLvkmWamouO2wVKV-3LUWRCzhVlMuf2RRs6G7GystLuy4RbQAwrNKZL26jueFnhXol_BCwE-X30MX7RkuuvbyO67Tvv99pL56-6k7VWUstNNIYT82Y1II5SdKibTIIO_jcDf-pxQpWBYCIrSmnqhS-zn_YyqcsUhQuuzeXqI-k3t5ZiLJcNfkghTaw8-wNDbl1Oadd4Gz8CXfKb-K-agGWowNxlKgB0mxDWptSuHe0Clfphxr9fjXXGRPz4P0qbWrf33eKC20nRkUBNJ23OKvfPW9KNTCs6bhsc_FhbIby0qFOiTiIjfT0dySvg";
        public const string ValidCtraegerJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiY3RyYWVnZXIiLCJleHAiOjIxNDc0ODM2NDh9.nch1skbWZrxgv-o26dMmnlv0gVf4c5YWlf-Mdd56DnWSVCgrzl0EBFjhDvkHLNE6bcQjz2ZDfJ--goVaMpNoLJXR6zv-ybMMxb1hEKUO0Ra0_Ox_DiAs7MMBdlcB0ZF8F51F0CuRw02HD5g7OSN-pyyIg1o2muJhPMoGCS6KGVk5WoTSKPEteqwCpdRUHw-h4Gq-upQtsmmfLamvj1cos5LaDfwQtXCos_ADkXj7o9ywTT4KKD1cdqWLagc20WOimcmuOUPImdLPS-w6rVIp24oyk7L39zQOKPKgE1TyZiuaHfG9rHX8gIP6zkPio3hrv2ANB3Wz33tO1OAAOmloJA";
        public const string ValidJgergichJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwidXNlcl9uYW1lIjoiZ2FycnlnIiwiZXhwIjoyMTQ3NDgzNjQ4fQ.nzXBDn19dkV0h9-vRHkZto3jbxHigeWFXO726vX6rymmjtqqEDFgDka9CP4K3yFydNbsEi9vIzS-SdfgqJhJgw44D4_N2pYo2sj_RL0oYyMnnUuAiOugEzLmZph1fVxQ77xYQ9L9LMGVX3hneeNEmE5ZrK_RaFdNHzEsrFMK-UB-2Ol5AW9GH1vVcAxuebN8PCyHx1VFaPjArbazV6x3ot_8bgacKby3XFS2ecGc2Tm1eKcOg3reFB5NrkXMc_khPrpWPCy_XHXKa7KFoGIn9zhkzBOGipDZ-E9rqWDtE7hQRjbZWYWU1-Xx5ohsaXWcKiEEsX416zVaNOR8pppszA";
        public const string ValidServiceAcct = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjk0NjY4NDgwMCwiZXhwIjoyMTQ3NDgzNjQ4LCJzY29wZSI6WyJyZWFkIl0sImNsaWVudF9pZCI6InVpdHN3ZWIifQ.cZJ9SwfUISJ7TK6Hfhi21_dKJMrVSYRfszA1BcSm8Ftnyy3vX19dsPEdwyNYJRfF1jbeG7XqXTRG2E71JS0H5U3OC-9baAdiVe9v7hLkmaa2SdLXS_e2p_PZMSRt3zKXJD3Rn3GwCnk1JIFDwA61MuZrvUmBW0JOCU3Ljkx2XzgdinUKv97dwIyaSddPoP9i98ntE8iueU5VDPB6Gi6T9e-NreglEm65jVgbRbLatUSF7CeoNEBBbT7zc_bV05j9lsIuRHRy3OXwYUdijiXNrNw_cDHAqLWvWXnaTlKxWIw10-Qa_JydoDHxVER60TMjRmhtwrKzNLBNtAc_4xQmlw";
        
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

        ///<summary>Verifies that requests the URL by a user with certain UnitPermissions returns the expected EntityPermissions.</summary>
        protected async Task GetReturnsCorrectEntityPermissions(string url, int unitId, UnitPermissions providedPermission, EntityPermissions expectedPermission)
        {
            // Add user Jerry Gergich, who is not in Test Entities, and only used for this test.
            var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
            var jerry = new Person
            {
				Netid = "garryg",
				Name = "Gergich, Jerry",
				NameFirst = "Jerry",
				NameLast = "Gergich",
				Position = "Waiting for Retirement",
				Location = "BL",
				Campus = "Pawnee",
				CampusPhone = "812.856.5557",
				CampusEmail = "garryg@pawnee.in.us",
				Expertise = "Going unnoticed",
				Notes = "",
				PhotoUrl = "https://sasquatchbrewery.com/wp-content/uploads/2018/06/lil.jpg",
				Responsibilities = Responsibilities.None,
				DepartmentId = TestEntities.Departments.Parks.Id,
				IsServiceAdmin = false
            };
            await db.People.AddAsync(jerry);

            var membership = new UnitMember
            {
                Person = jerry,
                UnitId = unitId,
                Permissions = providedPermission,
				Title = "Forgotten Man",
				Percentage = 100,
				Notes = "",
				MemberTools = new List<MemberTool>()
            };
            await db.UnitMembers.AddAsync(membership);
            await db.SaveChangesAsync();

            // GET request the provided url with Jerry's JWT
            var resp = await GetAuthenticated(url, ValidJgergichJwt);
            AssertStatusCode(resp, HttpStatusCode.OK);
            
            // Verify they get the expectedPermission.
            AssertPermissions(resp, expectedPermission);
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