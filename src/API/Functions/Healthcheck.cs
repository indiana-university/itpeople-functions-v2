using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace API.Functions
{
    public static class HealthCheck
    {
        [FunctionName(nameof(HealthCheck.Ping))]
        public static IActionResult Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req) 
                => new OkObjectResult("Pong!");

        [FunctionName(nameof(HealthCheck.RenderOpenApiJson))]
        public static IActionResult RenderOpenApiJson(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi.json")] HttpRequest req) 
                => new RedirectResult("/api/swagger.json");

    }
}
