using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware
{
    public static class Response
    {
        public class Headers
        {
            public const string XUserPermissions = "x-user-permissions";
            public const string AccessControlExposeHeaders = "Access-Control-Expose-Headers";
        }
        
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
                System.Console.WriteLine($"[{result.Error.StatusCode}] {result.Error.Messages}");
                return result.Error.ToActionResult();
            }
        }

        /// <summary>Return an HTTP 201 response with content, with the URL for the resource and the resource itself.</summary>
        public static IActionResult Created<T>(string baseUrl, Result<T, Error> result) where T : Models.Entity
		{
            // var logger = Logging.GetApiLogger(req, principal);
            if (result.IsSuccess)
            {
                return new CreatedResult($"{baseUrl}/{result.Value.Id}", result.Value);
            }
            else 
            {
                // logger.FailureResult<T>("Fetch", result.Error);
                System.Console.WriteLine($"[{result.Error.StatusCode}] {result.Error.Messages}");
                return result.Error.ToActionResult();
            }
        }

        /// <summary>Return an HTTP 204 indicating success, but nothing to return.</summary>
        public static IActionResult NoContent<T>(HttpRequest req, Result<T, Error> result)
        {
            // var logger = Logging.GetApiLogger(req, principal);
            if (result.IsSuccess)
            {
                return new NoContentResult();
            }
            else 
            {
                // logger.FailureResult<T>("Fetch", result.Error);
                System.Console.WriteLine($"[{result.Error.StatusCode}] {result.Error.Messages}");
                return result.Error.ToActionResult();
            }
        }
    }
}
