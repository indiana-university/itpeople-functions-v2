using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace StateServer
{
    public static class Functions
    {
        [FunctionName(nameof(Functions.Ping))]
        public static IActionResult Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req) 
                => new OkObjectResult("Pong!");
       
       [FunctionName(nameof(Functions.State))]
        public static IActionResult State(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "state")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Resetting Postgres DB with test data...");
            Integration.DatabaseContainer.ResetDatabase(); 
            return new OkResult();
        }    
    }
}
