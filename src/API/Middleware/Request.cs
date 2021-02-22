using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Models.Enums;

namespace API.Middleware
{
    public static class Request
    {
        public static Task<Result<T,Error>> DeserializeBody<T>(HttpRequest req)
            => TryDeserializeBody<T>(req)
                .Bind(body => ValidateBody<T>(body));

        private static async Task<Result<T,Error>> TryDeserializeBody<T>(HttpRequest req)
        {
            try
            {
                var json = await req.ReadAsStringAsync();
                var body = JsonConvert.DeserializeObject<T>(json);
                return Pipeline.Success(body);
            }
            catch (Exception ex)
            {
                return Pipeline.BadRequest($"Failed to deserialize request body: {ex.Message}");
            }
        }

        /// <summary>Deserialize the request body to an instance of specified type and validate all properties. If valid, the instance is returned.</summary>
        private static Result<T, Error> ValidateBody<T>(T body)
        {
            var validationContext = new ValidationContext(body, null, null);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(body, validationContext, results, true);

            return results.Count > 0
            ? Pipeline.BadRequest(results.Select(r => r.ErrorMessage))
            : Pipeline.Success(body);
        }
    }

    public static class HttpRequestExtensions
    {
        public static void SetEntityPermissions(this HttpRequest req, EntityPermissions permissions)
        {
            req.HttpContext.Items[Response.Headers.XUserPermissions] = permissions;
            req.HttpContext.Response.Headers[Response.Headers.XUserPermissions] = permissions.ToString();
            // TODO: CORS stuff...
            req.HttpContext.Response.Headers[Response.Headers.AccessControlExposeHeaders] = Response.Headers.XUserPermissions;
        }

        public static EntityPermissions GetEntityPermissions(this HttpRequest req) 
            => (EntityPermissions)req.HttpContext.Items[Response.Headers.XUserPermissions];
    }
}
