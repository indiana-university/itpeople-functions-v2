using Microsoft.Azure.Functions.Worker;
using System.Linq;
using System;
using System.Collections.Generic;
using Models;
using Database;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.Functions.Worker.Http;

namespace Tasks
{
    public static class Healthcheck
    {
        [Function(nameof(Ping))]
        public static string Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequestData req) 
                => "Pong!";

        private static async Task<string> TryGetIP(string hostName)
        {
            var start = DateTime.Now;
            try
            {
                var ip = await Task.Run(()=>System.Net.Dns.GetHostEntry(hostName));
                return $"😎 ({(Elapsed(start))}): {hostName} -> {string.Join(",", ip.AddressList.Select(ipx=>ipx.ToString()))}";
            }
            catch (Exception ex)
            {
                return $"💩 ({(Elapsed(start))}): {hostName} -> {ex.Message}";
            }
        }

        [Function(nameof(SmokeTest))]
        public static async Task<string> SmokeTest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "smokeTest")] HttpRequestData req)
        {

            var dnsDb = TryGetIP("esdbp57p.uits.iu.edu");
            var dnsDenodoProd = TryGetIP("ebidvt.uits.iu.edu");
            var dnsDenodoDev = TryGetIP("ebidvt-dev.uits.iu.edu");
            var dnsLdap = TryGetIP("ads.iu.edu");
            var dnsUaa = TryGetIP("apps.iu.edu");
            var dnsProfile = TryGetIP("prs.apps.iu.edu");
            var dnsPie = TryGetIP("pie.iu.edu");
            var dnsPieStage = TryGetIP("pie-stage.eas.iu.edu");
            var db = TryDbConnect();
            var denodo = TryDenodoConnect();
            var ldap = TryLdapConnect();
            var uaa = TryUaaConnect();
            var pie = TryPiePing("pie.iu.edu");
            var pieStage = TryPiePing("pie-stage.eas.iu.edu");
            await Task.WhenAll(dnsDb, dnsDenodoDev, dnsDenodoProd, dnsLdap, dnsUaa, dnsProfile, dnsPie, dnsPieStage, db, denodo, ldap, uaa, pie, pieStage);
            return $@"
~~~~~~~~~~~~~~~~
~~~ Database ~~~
~~~~~~~~~~~~~~~~

DNS Resolution: {dnsDb.Result}
Connection:     {db.Result}


~~~~~~~~~~~~~~~~
~~~~ Denodo ~~~~
~~~~~~~~~~~~~~~~

Dev DNS:        {dnsDenodoDev.Result}
Prod DNS:       {dnsDenodoProd.Result}
Connection:     {denodo.Result}

~~~~~~~~~~~~~~~~
~~~~  LDAP  ~~~~
~~~~~~~~~~~~~~~~

DNS Resolution: {dnsLdap.Result}
Connection:     {ldap.Result}

~~~~~~~~~~~~~~~~
~~~~~ Apps ~~~~~
~~~~~~~~~~~~~~~~

UAA DNS Resolution: {dnsUaa.Result}
PRS DNS Resolution: {dnsProfile.Result}
UAA Connection:     {uaa.Result}

~~~~~~~~~~~~~~~~
~~~ Pie Prod ~~~
~~~~~~~~~~~~~~~~

DNS Resolution: {dnsPie.Result}
Connection:     {pie.Result}

~~~~~~~~~~~~~~~~
~~ Pie Stage  ~~
~~~~~~~~~~~~~~~~

DNS Resolution: {dnsPieStage.Result}
Connection:     {pieStage.Result}
";
        }

        private static async Task<string> TryLdapConnect()
        {
            var start = DateTime.Now;
            try
            {
                var result = await Task.Run(() => Tools.GetLdapConnection());
                return $"😎 ({Elapsed(start)})";
            }
            catch (Exception ex)
            {
                return $"💩 ({Elapsed(start)})\n{ex.ToString()}";
            }
        }

        private static async Task<string> TryPiePing(string baseUrl)
        {
            var start = DateTime.Now;
            try
            {
                var url = $"https://{baseUrl}/api/ping";
                var resp = await Utils.HttpClient.GetAsync(url);
                var result = await resp.Content.ReadAsStringAsync();
                return $"😎 Status Code : {resp.StatusCode}, Body: {result}, ({Elapsed(start)})";
            }
            catch (Exception ex)
            {
                return $"💩 ({Elapsed(start)})\n{ex.ToString()}";
            }
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
                    return $"😎 ({Elapsed(start)})";
                }
            }
            catch (Exception ex)
            {
                return $"💩 ({Elapsed(start)})\n{ex.ToString()}";
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
                return $"😎 ({Elapsed(start)})";
            }
            catch (Exception ex)
            {
                return $"💩 ({Elapsed(start)})\n{ex.ToString()}";
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
                return $"😎 ({Elapsed(start)})";
            }
            catch (Exception ex)
            {
                return $"💩 ({Elapsed(start)})\n{ex.ToString()}";
            }
        }

    }
}
