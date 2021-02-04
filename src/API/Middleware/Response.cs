using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Models;
using Microsoft.Azure.WebJobs.Extensions.Http;

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
            var logger = Logging.GetLogger(req);
            if (result.IsSuccess)
            {
                // TODO:
                // if a get, don't log the value.
                // if a put, do log the value.
                logger.SuccessResult(req, HttpStatusCode.OK);
                return new OkObjectResult(result.Value);      
            }
            else 
            {
                logger.FailureResult<T>(req, result.Error);
                return result.Error.ToActionResult();
            }
        }

        /// <summary>Return an HTTP 201 response with content, with the URL for the resource and the resource itself.</summary>
        public static IActionResult Created<T>(HttpRequest req, Result<T, Error> result) where T : Models.Entity
		{
            var logger = Logging.GetLogger(req);
            if (result.IsSuccess)
            {
                logger.SuccessResult(req, HttpStatusCode.Created);
                return new CreatedResult($"{req.Path}/{result.Value.Id}", result.Value);
            }
            else 
            {
                logger.FailureResult<T>(req, result.Error);
                return result.Error.ToActionResult();
            }
        }

        /// <summary>Return an HTTP 204 indicating success, but nothing to return.</summary>
        public static IActionResult NoContent<T>(HttpRequest req, Result<T, Error> result)
        {
            var logger = Logging.GetLogger(req);
            if (result.IsSuccess)
            {
                logger.SuccessResult(req, HttpStatusCode.NoContent);
                return new NoContentResult();
            }
            else 
            {
                logger.FailureResult<T>(req, result.Error);
                return result.Error.ToActionResult();
            }
        }

        private static void SuccessResult(this Serilog.ILogger logger, HttpRequest request, HttpStatusCode statusCode) 
            => logger
                .ForContext(LogProps.StatusCode, (int)statusCode)
                .ForContext(LogProps.RequestBody, request.ContentLength > 0 ? request.ReadAsStringAsync().Result : null)
                .ForContext(LogProps.RecordBody, request.HttpContext.Items[LogProps.RecordBody])
                .Information($"[{{{LogProps.StatusCode}}}] {{{LogProps.RequestorNetid}}} - {{{LogProps.RequestMethod}}} {{{LogProps.Function}}}{{{LogProps.RequestParameters}}}{{{LogProps.RequestQuery}}}");

        private static void FailureResult<T>(this Serilog.ILogger logger, HttpRequest request, Error error) 
            => logger
                .ForContext(LogProps.StatusCode, (int)error.StatusCode)
                .ForContext(LogProps.RequestBody, request.ContentLength > 0 ? request.ReadAsStringAsync().Result : null)
                .ForContext(LogProps.RecordBody, request.HttpContext.Items[LogProps.RecordBody])
                .ForContext(LogProps.ErrorMessages, JsonConvert.SerializeObject(error.Messages))
                .Error(error.Exception, $"[{{{LogProps.StatusCode}}}] {{{LogProps.RequestorNetid}}} - {{{LogProps.RequestMethod}}} {{{LogProps.Function}}}{{{LogProps.RequestParameters}}}{{{LogProps.RequestQuery}}}:\nErrors: {{{LogProps.ErrorMessages}}}.");
    }
}
