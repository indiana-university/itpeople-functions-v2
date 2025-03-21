using Serilog;
using Microsoft.ApplicationInsights.Extensibility;
using System.Collections.Generic;
using Serilog.Sinks.PostgreSQL;
using NpgsqlTypes;
using Newtonsoft.Json;
using Serilog.Events;
using System;
using Microsoft.DurableTask;

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
            try
            {
                var appInsightsKey = Utils.Env("APPLICATIONINSIGHTS_CONNECTION_STRING");
                if (!string.IsNullOrWhiteSpace(appInsightsKey))
                {
                    logger.WriteTo.ApplicationInsights(
                        TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces, minLevel);
                }
            }
            catch {}
            
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
            try
            {
                var tableName = "logs_automation";
                var connectionString = Utils.Env("DatabaseConnectionString");

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    logger.WriteTo.PostgreSQL(
                        connectionString, tableName, columnWriters, minLevel);
                }
            }
            catch {}
            
            return logger;
        }

        public static ILogger GetLogger(TaskOrchestrationContext ctx, object properties = null) 
            => GetLogger(ctx.Name, properties);

        private static Lazy<ILogger> Logger = new Lazy<ILogger>(() => 
            new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(LogEventLevel.Information)
                .TryAddAzureAppInsightsSink(LogEventLevel.Information)
                .TryAddPostgresqlDatabaseSink(LogEventLevel.Debug)
                .CreateLogger());

        public static ILogger GetLogger(string function, object properties = null) 
            => Logger.Value
                .ForContext(LogProps.InvocationId, System.Guid.Parse("00000000-0000-0000-0000-000000000000"))
                .ForContext(LogProps.Function, function)
                .ForContext(LogProps.Properties, properties == null ? null : JsonConvert.SerializeObject(properties, Models.Json.JsonSerializerSettings));        
    }
}
