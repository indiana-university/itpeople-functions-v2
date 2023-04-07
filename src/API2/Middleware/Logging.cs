using Microsoft.AspNetCore.Http;
using Serilog;
using Microsoft.ApplicationInsights.Extensibility;
using System.Collections.Generic;
using Serilog.Sinks.PostgreSQL;
using NpgsqlTypes;
using System.Linq;
using System;
using Microsoft.Azure.Functions.Worker.Http;

namespace API.Middleware
{

    public static class LogProps
    {
        public const string Function = "Function";
        public const string RequestMethod = "RequestMethod";
        public const string RequestIPAddress = "RequestIPAddress";
        public const string RequestorNetid = "RequestorNetid";
        public const string RequestQuery = "RequestQuery";
        public const string RequestParameters = "RequestParameters";
        public const string RequestBody = "RequestBody";
        public const string RecordBody = "RecordBody";
        
        public const string ElapsedTime = "ElapsedTime";
        

        public const string ItemId = "ItemId";
        public const string ItemType = "ItemType";
        public const string ItemProperties = "ItemProperties";

        public const string StatusCode = "StatusCode";
        public const string ErrorMessages = "ErrorMessages";
        public const string ErrorStackTrace = "ErrorStackTrace";
    }

    public static class Logging
    {
        private static LoggerConfiguration TryAddAzureAppInsightsSink(this LoggerConfiguration logger)
        {
            var appInsightsKey = Utils.Env("APPINSIGHTS_INSTRUMENTATIONKEY");
            if (!string.IsNullOrWhiteSpace(appInsightsKey))
            {
                logger.WriteTo.ApplicationInsights(
                    TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces);
            }

            return logger;
        }
        
        private static IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
        {
            {"timestamp", new TimestampColumnWriter(NpgsqlDbType.Timestamp) }, 
            {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
            {"elapsed", new SinglePropertyColumnWriter(LogProps.ElapsedTime, PropertyWriteMethod.Raw, NpgsqlDbType.Integer) }, 
            {"status", new SinglePropertyColumnWriter(LogProps.StatusCode, PropertyWriteMethod.Raw, NpgsqlDbType.Integer) },
            {"method", new SinglePropertyColumnWriter(LogProps.RequestMethod, PropertyWriteMethod.Raw, NpgsqlDbType.Text) }, // http method
            {"function", new SinglePropertyColumnWriter(LogProps.Function, PropertyWriteMethod.Raw, NpgsqlDbType.Text) }, // first part of path
            {"parameters", new SinglePropertyColumnWriter(LogProps.RequestParameters, PropertyWriteMethod.Raw, NpgsqlDbType.Text) }, // subsequent parts of path
            {"query", new SinglePropertyColumnWriter(LogProps.RequestQuery, PropertyWriteMethod.Raw, NpgsqlDbType.Text) }, // query string
            {"detail", new SinglePropertyColumnWriter(LogProps.ErrorMessages, PropertyWriteMethod.Raw, NpgsqlDbType.Text) }, // error message details
            {"request", new SinglePropertyColumnWriter(LogProps.RequestBody, PropertyWriteMethod.Raw, NpgsqlDbType.Json) }, // request body
            {"record", new SinglePropertyColumnWriter(LogProps.RecordBody, PropertyWriteMethod.Raw, NpgsqlDbType.Json) }, // existing record body
            {"ip_address", new SinglePropertyColumnWriter(LogProps.RequestIPAddress, PropertyWriteMethod.Raw, NpgsqlDbType.Text) },
            {"netid", new SinglePropertyColumnWriter(LogProps.RequestorNetid, PropertyWriteMethod.Raw, NpgsqlDbType.Text) }, // requestor netid
            {"exception", new ExceptionColumnWriter(NpgsqlDbType.Text) } // exception details
        };

        private static LoggerConfiguration TryAddPostgresqlDatabaseSink(this LoggerConfiguration logger)
        {
            var tableName = "logs";
            var connectionString = Utils.Env("DatabaseConnectionString", required:true);

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                logger.WriteTo.PostgreSQL(
                    connectionString, tableName, columnWriters);
            }
            return logger;
        }

        private static Lazy<ILogger> Logger = new Lazy<ILogger>(() => 
            new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .TryAddAzureAppInsightsSink()
                .TryAddPostgresqlDatabaseSink()
                .CreateLogger());

        public static ILogger GetLogger(HttpRequest req)
        {
            var pathParts = req.Path.HasValue
                ? req.Path.Value.Split("/", System.StringSplitOptions.RemoveEmptyEntries)
                : new[]{string.Empty};

            var elapsed = 
                req.HttpContext.Items.ContainsKey(LogProps.ElapsedTime)
                ? (DateTime.UtcNow - (DateTime)req.HttpContext.Items[LogProps.ElapsedTime]).TotalMilliseconds
                : -1;

            return Logger.Value
                .ForContext(LogProps.ElapsedTime, elapsed)
                .ForContext(LogProps.RequestIPAddress, req.HttpContext.Connection.RemoteIpAddress)
                .ForContext(LogProps.RequestMethod, req.Method)
                .ForContext(LogProps.Function, pathParts.First())
                .ForContext(LogProps.RequestParameters, string.Join('/', pathParts.Skip(1)))
                .ForContext(LogProps.RequestQuery, req.QueryString)
                .ForContext(LogProps.RequestorNetid, req.HttpContext.Items[LogProps.RequestorNetid]);
        }

        public static ILogger GetLogger(HttpRequestData req)
        {
            var pathParts = req.Url.AbsolutePath.Split("/", System.StringSplitOptions.RemoveEmptyEntries);

            // Functions V4 doesn't do HttpContext maybe we can get lucky with the FunctionContext.
            var elapsed = 
                req.FunctionContext.Items.ContainsKey(LogProps.ElapsedTime)
                ? (DateTime.UtcNow - (DateTime)req.FunctionContext.Items[LogProps.ElapsedTime]).TotalMilliseconds
                : -1;

            // You can only get IP's if you're hosted on Azure and it's setting headers for you.
            req.Headers.TryGetValues("x-forwarded-for", out var requestorIpHeaders);
            var requestorIp = requestorIpHeaders?.FirstOrDefault() ?? string.Empty;

            // Again, without the HttpContext I expect this to fall flat on its face.
            var requestorNetid = req.FunctionContext.Items[LogProps.RequestorNetid];

            return Logger.Value
                .ForContext(LogProps.ElapsedTime, elapsed)
                .ForContext(LogProps.RequestIPAddress, requestorIp)
                .ForContext(LogProps.RequestMethod, req.Method)
                .ForContext(LogProps.Function, pathParts.First())
                .ForContext(LogProps.RequestParameters, string.Join('/', pathParts.Skip(1)))
                .ForContext(LogProps.RequestQuery, req.Url.Query)
                .ForContext(LogProps.RequestorNetid, requestorNetid);
        }
    }
}
