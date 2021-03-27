using Serilog;
using Microsoft.ApplicationInsights.Extensibility;
using System.Collections.Generic;
using Serilog.Sinks.PostgreSQL;
using NpgsqlTypes;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using Serilog.Events;

namespace Tasks
{

    public static class LogProps
    {
        public const string Function = "Function";
        public const string InvocationId = "InvocationId";
        public const string Message = "Message";
        public const string Properties = "Properties";
    }

    public static class Logging
    {
        private static LoggerConfiguration TryAddAzureAppInsightsSink(this LoggerConfiguration logger, LogEventLevel minLevel)
        {
            var appInsightsKey = Utils.Env("APPINSIGHTS_INSTRUMENTATIONKEY");
            if (!string.IsNullOrWhiteSpace(appInsightsKey))
            {
                logger.WriteTo.ApplicationInsights(
                    TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces, minLevel);
            }

            return logger;
        }
        
        private static IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
        {
            {"timestamp", new TimestampColumnWriter(NpgsqlDbType.Timestamp) }, 
            {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
            {"invocation_id", new SinglePropertyColumnWriter(LogProps.InvocationId, PropertyWriteMethod.Raw, NpgsqlDbType.Uuid) }, 
            {"function_name", new SinglePropertyColumnWriter(LogProps.Function, PropertyWriteMethod.Raw, NpgsqlDbType.Text) }, // first part of path
            {"message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
            {"properties", new SinglePropertyColumnWriter(LogProps.Properties, PropertyWriteMethod.Raw, NpgsqlDbType.Json) },
            {"exception", new ExceptionColumnWriter(NpgsqlDbType.Text) } // exception details
        };

        private static LoggerConfiguration TryAddPostgresqlDatabaseSink(this LoggerConfiguration logger, LogEventLevel minLevel)
        {
            var tableName = "logs_automation";
            var connectionString = Utils.Env("DatabaseConnectionString", required:true);

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                logger.WriteTo.PostgreSQL(
                    connectionString, tableName, columnWriters, minLevel);
            }
            return logger;
        }

        public static ILogger GetLogger(string functionName, object properties = null)  
            => GetLogger("00000000-0000-0000-0000-000000000000", functionName, properties);

        public static ILogger GetLogger(IDurableOrchestrationContext ctx, object properties = null) 
            => GetLogger(ctx.InstanceId, ctx.Name, properties);

        private static ILogger Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(LogEventLevel.Information)
                .TryAddAzureAppInsightsSink(LogEventLevel.Information)
                .TryAddPostgresqlDatabaseSink(LogEventLevel.Debug)
                .CreateLogger();

        public static ILogger GetLogger(string instanceId, string function, object properties = null) 
            => Logger
                .ForContext(LogProps.InvocationId, System.Guid.Parse(instanceId))
                .ForContext(LogProps.Function, function)
                .ForContext(LogProps.Properties, properties == null ? null : JsonConvert.SerializeObject(properties, Models.Json.JsonSerializerSettings));        
    }
}
