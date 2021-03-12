using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Collections.Generic;
using Models;
using Database;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Tasks
{
    public static class Healthcheck
    {
        [FunctionName(nameof(Ping))]
        public static string Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req) 
                => "Pong!";

        [FunctionName(nameof(DnsCheck))]
        public static string DnsCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dnsCheck")] HttpRequest req)
        {
            return DoDnsCheck();
        }

        private static string DoDnsCheck()
        {
            var hosts = new[]{
                "apps.iu.edu",
                "prs.apps.iu.edu",
                "ebidvt.uits.iu.edu",
                "esdbp57p.uits.iu.edu"
            };
            return string.Join("\n", hosts.Select(TryGetIP));
        }

        private static async Task<string> TryGetIP(string hostName)
        {
            var start = DateTime.Now;
            try
            {
                var ip = await Task.Run(()=>System.Net.Dns.GetHostEntry(hostName));
                return $"{hostName} ({(Elapsed(start))}): {string.Join(",", ip.AddressList.Select(ipx=>ipx.ToString()))}";
            }
            catch (Exception ex)
            {
                return $"{hostName} ({(Elapsed(start))}): {ex.Message}";
            }
        }

        [FunctionName(nameof(SmokeTest))]
        public static async Task<string> SmokeTest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "smokeTest")] HttpRequest req)
        {

            var dnsDb = TryGetIP("esdbp57p.uits.iu.edu");
            var dnsDenodo = TryGetIP("ebidvt.uits.iu.edu");
            var dnsUaa = TryGetIP("apps.iu.edu");
            var dnsProfile = TryGetIP("prs.apps.iu.edu");
            var db = TryDbConnect();
            var denodo = TryDenodoConnect();
            var uaa = TryUaaConnect();
            await Task.WhenAll(dnsDb, dnsDenodo, dnsUaa, dnsProfile, db, denodo, uaa);
            return $@"
~~~~~ DNS ~~~~~
Database: {dnsDb.Result}
Denodo:   {dnsDenodo.Result}
Apps/UAA: {dnsUaa.Result}
Apps/PRS: {dnsProfile.Result}

~~~ Database ~~~
{db.Result}

~~~~ Denodo ~~~~
Denodo:   {denodo.Result}

~~~ Apps/UAA ~~~
{uaa.Result}";
        }

        private static async Task<string> TryDbConnect()
        {
            var start = DateTime.Now;
            try
            {
                var connStr = Utils.Env("DatabaseConnectionString", required: true);
                using (var db = PeopleContext.Create(connStr))
                {
                    var tool = await db.Tools.FirstAsync();
                    return $"OK ({Elapsed(start)})";
                }
            }
            catch (Exception ex)
            {
                return $"Failed ({Elapsed(start)}):\n{ex.ToString()}";
            }
        }

        private static string Elapsed(DateTime start) 
            => $"{Math.Round((DateTime.Now - start).TotalSeconds, 1)}s";

        private static async Task<string> TryDenodoConnect()
        {
            var start = DateTime.Now;
            try
            {
                var denodoUrl = Utils.Env("DenodoBuildingsViewUrl", required: true);
                var denodoUser = Utils.Env("DenodoBuildingsViewUser", required: true);
                var denodoPwd = Utils.Env("DenodoBuildingsViewPassword", required: true);
                var basicAuth = Convert.ToBase64String(
                    System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(
                        $"{denodoUser}:{denodoPwd}"
                    )
                );
                var req = new HttpRequestMessage(HttpMethod.Get, denodoUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);            
                var resp = await Utils.HttpClient.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                return $"OK ({Elapsed(start)})";
            }
            catch (Exception ex)
            {
                return $"Failed ({Elapsed(start)}):\n{ex.ToString()}";
            }
        }

        private static async Task<string> TryUaaConnect()
        {
            var start = DateTime.Now;
            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string,string>{
                    {"grant_type", "client_credentials"},
                    {"client_id", Utils.Env("UaaClientCredentialId", required: true)},
                    {"client_secret", Utils.Env("UaaClientCredentialSecret", required: true)},
                });
                var url = Utils.Env("UaaClientCredentialUrl", required: true);
                var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                var resp = await Utils.HttpClient.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                return $"OK ({Elapsed(start)})";
            }
            catch (Exception ex)
            {
                return $"Failed ({Elapsed(start)}):\n{ex.ToString()}";
            }
        }

    }
}
