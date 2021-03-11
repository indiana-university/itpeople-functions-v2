using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;

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
            var hosts = new[]{
                "ebidvt.uits.iu.edu",
                "apps.iu.edu",
                "prs.apps.iu.edu",
                "esdbp57p.uits.iu.edu"
            };
            return string.Join("; ", hosts.Select(TryGetIP));
        }

        private static string TryGetIP(string hostName)
        {
            try
            {
                var ip = System.Net.Dns.GetHostEntry(hostName);
                return $"{hostName}:{string.Join(",", ip.AddressList.Select(ipx=>ipx.ToString()))}";
            }
            catch (Exception ex)
            {
                return $"{hostName}:{ex.Message}";
            }
        }
    }
}
