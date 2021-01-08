using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using API.Data;
using System.Threading.Tasks;
using System.Text.Json;

namespace API.Functions
{
    public static class People
    {        
        [FunctionName("GetByNetid")]
        public static async Task<HttpResponseData> GetByNetid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/getbynetid")] HttpRequestData req) 
            {
                var repo = new PeopleRepository();
                var result = await repo.GetByNetId("");
                return result.IsSuccess
                    ? new HttpResponseData(HttpStatusCode.OK, JsonSerializer.Serialize(result.Value))
                        {
                            Headers = { { "Content-Type", "application/json; charset=utf-8" } }
                        }
                    : new HttpResponseData(HttpStatusCode.InternalServerError, result.Error);
            }
    }
}
