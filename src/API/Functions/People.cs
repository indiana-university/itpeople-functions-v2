using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

using API.Data;
using API.Middleware;

namespace API.Functions
{
    public static class People
    {        
        [FunctionName("GetByNetid")]
        public static Task<HttpResponseData> GetByNetid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/getbynetid")] HttpRequestData req) 
            {
                var repo = new PeopleRepository();
                return repo.GetByNetId("")
                    .Finally(result => Response.Ok(req, result));
            }
    }
}
