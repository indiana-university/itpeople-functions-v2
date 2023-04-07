using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using API.Middleware;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace API.Functions
{
    public static class HealthCheck
    {
        [Function(nameof(HealthCheck.Ping))]
        public static Task<HttpResponseData> Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequestData req) 
                => Response.Ok(req, Pipeline.Success("Pong!"));
    }
}
