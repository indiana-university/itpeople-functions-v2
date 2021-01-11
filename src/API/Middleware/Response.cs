using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware
{
    public static class Response
    {
        /// <summary>Return an HTTP 200 response with content, or an appropriate HTTP error response.</summary>
        public static IActionResult Ok<T>(HttpRequest req, Result<T, Error> result)
        {
            // var logger = Logging.GetApiLogger(req, principal);
            if (result.IsSuccess)
            {
                return new OkObjectResult(result.Value);      
            }
            else 
            {
                // logger.FailureResult<T>("Fetch", result.Error);
                return result.Error.ToActionResult();
            }
        }
    }
}
