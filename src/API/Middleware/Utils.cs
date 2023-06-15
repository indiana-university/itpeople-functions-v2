using CSharpFunctionalExtensions;
using System;
using System.Net;

namespace API.Middleware
{
    public static class Utils
    {
        public static string Env(string key, bool required=false)
        {
            var value = System.Environment.GetEnvironmentVariable(key);
            if (required && string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"Missing required environment setting: {key}");
            }
            return value;
        }

        public static Result<int, Error> ConvertParam(string value, string paramName)
        {
            if (!int.TryParse(value, out var result))
            {
                return new Error(HttpStatusCode.BadRequest, $"Expected {paramName} to be an integer value");
            };

            return Pipeline.Success(result);
        }
    }
}
