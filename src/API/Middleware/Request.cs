using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Microsoft.ApplicationInsights.Extensibility;

namespace API.Middleware
{
    public static class LogProps
    {
        public const string FunctionName = "FunctionName";
        public const string CorrelationId = "CorrelationId";
        public const string IPAddress = "IPAddress";
        public const string UserName = "UserName";

        public const string ItemId = "ItemId";
        public const string ItemType = "ItemType";
        public const string ItemProperties = "ItemProperties";

        public const string StatusCode = "StatusCode";
        public const string ErrorInfo = "ErrorInfo";
        public const string ErrorStackTrace = "ErrorStackTrace";
    }

    public static class Logging
    {
        private static string Env(string key) 
            => System.Environment.GetEnvironmentVariable(key);

        private static void TryAddAzureAppInsightsSink(LoggerConfiguration logger)
        {
            var appInsightsKey = Env("APPINSIGHTS_INSTRUMENTATIONKEY");
            if (!string.IsNullOrWhiteSpace(appInsightsKey))
            {
                logger.WriteTo.ApplicationInsights(
                    TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces);
            }
        }
        
        private static ILogger CreateLogger()
        {
            var logger =
                new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console();

            TryAddAzureAppInsightsSink(logger);

            return logger.CreateLogger();
        }

        public static ILogger GetLogger(HttpRequest req)
        {
            // TODO: pull requestor netid out of the header.
            string requestor = "anonymous";

            return CreateLogger()
                .ForContext(LogProps.IPAddress, req.HttpContext.Connection.RemoteIpAddress)
                .ForContext(LogProps.FunctionName, $"{req.Method} {req.Path}")
                .ForContext(LogProps.UserName, requestor);
        }
    }

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
}
