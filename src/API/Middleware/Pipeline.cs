using CSharpFunctionalExtensions;
using Microsoft.Azure.Functions.Worker;
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
                case HttpStatusCode.BadRequest: return BadRequest(msg);
                case HttpStatusCode.NotFound: return NotFound(msg);
                case HttpStatusCode.Conflict: return Conflict(msg);
                default: return InternalServerError(msg);
            }
        }

        public static Error Unauthorized()
            => new Error(HttpStatusCode.Unauthorized);


        public static Error BadRequest(string message)
            => new Error(HttpStatusCode.BadRequest, message);

        public static Error NotFound(string message)
            => new Error(HttpStatusCode.NotFound, message);
            
        public static Error Conflict(string message)
            => new Error(HttpStatusCode.Conflict, message);

        public static Error InternalServerError(string message)
            => new Error(HttpStatusCode.InternalServerError, message);
    
    }

    public class Error
    {
        internal Error(HttpStatusCode statusCode, string message = null)
        {
            StatusCode = statusCode;
            Message = message;
        }

        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }

        public HttpResponseData ToResponse() 
            => new HttpResponseData(StatusCode, Message);
    }
}
