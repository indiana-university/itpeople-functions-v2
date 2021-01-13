using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

using API.Data;
using API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace API.Functions
{
    public static class People
    {
        [FunctionName("PeopleGetAll")]
        public static Task<IActionResult> PeopleGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => PeopleRepository.GetAllAsync())
                .Finally(people => Response.Ok(req, people));

        [FunctionName("GetByNetid")]
        public static Task<IActionResult> GetByNetid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/getbynetid")] HttpRequest req) 
            {
                var repo = new PeopleRepository();
                return repo.GetByNetId("")
                    .Finally(result => Response.Ok(req, result));
            }
    }
}
