using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using API.Middleware;

namespace API.Functions
{
    public static class HealthCheck
    {
        [FunctionName(nameof(HealthCheck.Ping))]
        public static HttpResponseMessage Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req) 
                => Response.Ok(req, Pipeline.Success("Pong!"));
    }
}
