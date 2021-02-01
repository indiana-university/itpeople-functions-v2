using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace API.Middleware
{

    public class Pipeline
    {
        public static Result<T, Error> Success<T>(T value) => Result.Success<T,Error>(value);
    
        public static Result<T, Error> Failure<T>(HttpStatusCode statusCode, string msg)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized: return Unauthorized();
                case HttpStatusCode.Forbidden: return Forbidden();
                case HttpStatusCode.BadRequest: return BadRequest(msg);
                case HttpStatusCode.NotFound: return NotFound(msg);
                case HttpStatusCode.Conflict: return Conflict(msg);
                default: return InternalServerError(msg);
            }
        }

        public static Error Unauthorized()
            => new Error(HttpStatusCode.Unauthorized, "You are not authorized to make this request.");

        public static Error Forbidden()
            => new Error(HttpStatusCode.Forbidden, "You are not authorized to make this request.");

        public static Error BadRequest(string message)
            => new Error(HttpStatusCode.BadRequest, message);

        public static Error BadRequest(IEnumerable<string> messages)
            => new Error(HttpStatusCode.BadRequest, messages);

        public static Error NotFound(string message)
            => new Error(HttpStatusCode.NotFound, message);
            
        public static Error Conflict(string message)
            => new Error(HttpStatusCode.Conflict, message);

        public static Error InternalServerError(string message, Exception ex = null)
            => new Error(HttpStatusCode.InternalServerError, message, ex);
    
    }

    public class Error
    {
        internal Error(HttpStatusCode statusCode, string message, Exception ex = null)
            : this(statusCode, new[]{message}, ex)
        {
        }

        internal Error(HttpStatusCode statusCode, IEnumerable<string> messages, Exception ex = null)
        {
            StatusCode = statusCode;
            Messages = messages;
            Exception = ex;
        }

        public HttpStatusCode StatusCode { get; private set; }
        public IEnumerable<string> Messages { get; private set; }
        public Exception Exception { get; private set; }

        public IActionResult ToActionResult()// => new StatusCodeResult((int)StatusCode);        
        {
            var content = new ApiError()
            {
                StatusCode = (int)StatusCode,
                Errors = Messages.ToList(),
                Details = Exception == null ? "(none)" : Exception.ToString()
            };

            switch (StatusCode)
            {
                case HttpStatusCode.Unauthorized: return new UnauthorizedResult();
                case HttpStatusCode.Forbidden: return new StatusCodeResult(403);
                case HttpStatusCode.BadRequest: return new BadRequestObjectResult(content);
                case HttpStatusCode.NotFound: return new NotFoundObjectResult(content);
                case HttpStatusCode.Conflict: return new ConflictObjectResult(content);
                default: return new ContentResult(){StatusCode=500, ContentType="application/json", Content=JsonConvert.SerializeObject(content)};
            }
        }
    }
}
