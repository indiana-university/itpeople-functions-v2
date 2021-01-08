using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Models;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using System.Text.Json;

namespace API.Middleware
{
    public static class Response
    {
        /// <summary>Return an HTTP 200 response with content, or an appropriate HTTP error response.</summary>
        public static HttpResponseData Ok<T>(HttpRequestData req, Result<T, Error> result)
        {
            // var logger = Logging.GetApiLogger(req, principal);
            if (result.IsSuccess)
            {
                return new HttpResponseData(HttpStatusCode.OK, JsonSerializer.Serialize(result.Value))
                    {
                        Headers = { { "Content-Type", "application/json; charset=utf-8" } }
                    };            
            }
            else 
            {
                // logger.FailureResult<T>("Fetch", result.Error);
                return result.Error.ToResponse();
            }
        }
    }
}
