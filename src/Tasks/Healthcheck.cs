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
}
