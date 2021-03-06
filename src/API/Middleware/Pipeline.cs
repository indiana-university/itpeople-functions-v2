using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace API.Middleware
{

    public class Pipeline
    {
        public static Result<T, Error> Success<T>(T value) => Result.Success<T,Error>(value);
    
        public static Error Unauthorized(string message)
            => new Error(HttpStatusCode.Unauthorized, message);

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

        public IActionResult ToResponse(Microsoft.AspNetCore.Http.HttpRequest req)
        {
            var includeStackTrace = !string.IsNullOrWhiteSpace(Utils.Env("IncludeStackTraceInError"));
            var content = new ApiError()
                {
                    StatusCode = (int)StatusCode,
                    Errors = Messages?.ToList(),
                    Details = Exception == null ? "(none)" : includeStackTrace ? Exception.ToString() : Exception.Message
                };
            var json = JsonConvert.SerializeObject(content, Json.JsonSerializerSettings);
            return Response.ContentResponse(req, StatusCode, "application/json", json);
        }
    }
}
