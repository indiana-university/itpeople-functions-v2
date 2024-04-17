using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Middleware;
using System.Threading.Tasks;

namespace API.Functions
{
    public static class HealthCheck
    {
        [Function(nameof(HealthCheck.Ping))]
        public static Task<IActionResult> Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req) 
                => Response.Ok(req, Pipeline.Success("Pong!"));
    }
}
