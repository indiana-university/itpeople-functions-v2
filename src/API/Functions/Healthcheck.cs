using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Middleware;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using API.Data;

namespace API.Functions
{
    public static class HealthCheck
    {
        [Function(nameof(HealthCheck.Ping))]
        public static Task<IActionResult> Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req) 
                => Response.Ok(req, Pipeline.Success("Pong!"));

        [Function(nameof(HealthCheck.ExerciseLogger))]
        public static async Task<IActionResult> ExerciseLogger([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ExerciseLogger")] HttpRequest req)
            => await Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineServiceAdminPermissions(req, requestor))
                .Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
                .Bind(_ => InduceException())
                .Finally(error => Response.NoContent(req, error));
        
        private static Result<string, Error> InduceException() => Pipeline.InternalServerError($"From {nameof(ExerciseLogger)}", new System.Exception($"A manually created exception for the ExerciseLogger function."));
    }
}
