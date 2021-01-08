using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using System.Collections.Generic;

namespace API.Functions
{
    public static class People
    {        
        [FunctionName("GetByNetid")]
        public static HttpResponseData GetByNetid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/getbynetid")] HttpRequestData req) 
            {
                var response = new HttpResponseData(HttpStatusCode.OK);
                var headers = new Dictionary<string, string>();
                headers.Add("Date", "Mon, 18 Jul 2016 16:06:00 GMT");
                headers.Add("Content", "Content - Type: text / plain; charset = utf-8");
                response.Headers = headers;
                response.Body = "Pong!";
                return response;
            }
    }
}
