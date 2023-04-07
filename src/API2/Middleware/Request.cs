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
using Microsoft.Azure.Functions.Worker.Http;

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
        public static Result<T, Error> ValidateBody<T>(T body)
        {
            var validationContext = new ValidationContext(body, null, null);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(body, validationContext, results, true);

            return results.Count > 0
            ? Pipeline.BadRequest(results.Select(r => r.ErrorMessage))
            : Pipeline.Success(body);
        }

        internal static Result<string,Error> GetRequiredQueryParam(HttpRequest req, string key)
        {
            var dict = req.GetQueryParameterDictionary();
            return dict.ContainsKey(key)
                ? Pipeline.Success(dict[key])
                : Pipeline.BadRequest($"Missing required query parameter: {key}");
        }

        internal static Result<string,Error> GetRequiredQueryParam(HttpRequestData req, string key)
        {
            var dict = req.GetQueryParameterDictionary();
            return dict.ContainsKey(key)
                ? Pipeline.Success(dict[key])
                : Pipeline.BadRequest($"Missing required query parameter: {key}");
        }
    }

    public static class HttpRequestExtensions
    {
        public static void SetEntityPermissions(this HttpRequest req, EntityPermissions permissions)
        {
            req.HttpContext.Items[Response.Headers.XUserPermissions] = permissions;
        }

        public static EntityPermissions GetEntityPermissions(this HttpRequest req) 
            => (EntityPermissions)req.HttpContext.Items[Response.Headers.XUserPermissions];

        public static bool HasEntityPermissions(this HttpRequest req) 
            => req.HttpContext.Items.ContainsKey(Response.Headers.XUserPermissions);
    }

    public static class HttpRequestDataExtensions
    {
        // TODO - Make sure this still works.
        public static bool HasEntityPermissions(this HttpRequestData req) 
            => req.FunctionContext.Items.ContainsKey(Response.Headers.XUserPermissions);
        
        //TO - Make sure this still works.
        public static EntityPermissions GetEntityPermissions(this HttpRequestData req) 
            => (EntityPermissions)req.FunctionContext.Items[Response.Headers.XUserPermissions];
        
        public static IDictionary<string, string> GetQueryParameterDictionary(this HttpRequestData req)
        {
            var result = new Dictionary<string, string>();

            var queryCollection = req.Url.ParseQueryString();

            var keys = queryCollection.AllKeys?
                .Select(a => a == null ? string.Empty : a as string)
                .Where(a => string.IsNullOrWhiteSpace(a) == false)
                .ToArray()
                ?? new string[] {};

            foreach(string key in keys)
            {
                var value = queryCollection[key] ?? string.Empty;
                result.Add(key, value);
            }

            return result;
        }
    }
}
