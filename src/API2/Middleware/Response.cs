using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Models;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace API.Middleware
{
    public static class Response
    {
        public class Headers
        {
            public const string XUserPermissions = "x-user-permissions";
            public const string AccessControlExposeHeaders = "Access-Control-Expose-Headers";
        }
        
        private static async Task<IActionResult> Generate<T>(
            HttpRequest req, 
            Result<T, Error> result, 
            HttpStatusCode statusCode, 
            Func<T,IActionResult> resultGenerator)
        {
            if (result.IsSuccess)
            {
                if (req.Method.ToLower() != "get")
                {
                    await Logging.GetLogger(req).SuccessResult(req, statusCode);
                }
                return resultGenerator(result.Value);
            }
            else 
            {
                await Logging.GetLogger(req).FailureResult<T>(req, result.Error);
                return result.Error.ToResponse(req);
            }
        }


        private static async Task<IActionResult> Generate<T>(
            HttpRequestData req, 
            Result<T, Error> result, 
            HttpStatusCode statusCode, 
            Func<T,IActionResult> resultGenerator)
        {
            if (result.IsSuccess)
            {
                if (req.Method.ToLower() != "get")
                {
                    await Logging.GetLogger(req).SuccessResult(req, statusCode);
                }
                return resultGenerator(result.Value);
            }
            else 
            {
                await Logging.GetLogger(req).FailureResult<T>(req, result.Error);
                return result.Error.ToResponse(req);
            }
        }

        /// <summary>Return an HTTP 200 response with content, or an appropriate HTTP error response.</summary>
        public static Task<IActionResult> Ok<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.OK, val => JsonResponse(req, val, HttpStatusCode.OK));
        
        public static Task<IActionResult> Ok<T>(HttpRequestData req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.OK, val => JsonResponse(req, val, HttpStatusCode.OK));

        /// <summary>Return an HTTP 201 response with content, with the URL for the resource and the resource itself.</summary>
        public static Task<IActionResult> Created<T>(HttpRequest req, Result<T, Error> result) where T : Models.Entity
		    => Generate(req, result, HttpStatusCode.Created, val => JsonResponse(req, val, HttpStatusCode.Created));

        /// <summary>Return an HTTP 204 indicating success, but nothing to return.</summary>
        public static Task<IActionResult> NoContent<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.NoContent, val => StatusCodeResponse(req, HttpStatusCode.NoContent));


        /// <summary>Return an HTTP 200 response with XML content, or an appropriate HTTP error response.</summary>
        public static Task<IActionResult> OkXml<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.OK, val => XmlResponse(req, val, HttpStatusCode.OK));

        private static IActionResult JsonResponse<T>(HttpRequest req, T value, HttpStatusCode status)
        {
            try
            {
                return ContentResponse(req, status, "application/json; charset=utf-8", JsonConvert.SerializeObject(value, Json.JsonSerializerSettings));
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError($"Failed to serialize {typeof(T).Name} response body as JSON", ex).ToResponse(req);
            }
        }

        private static IActionResult JsonResponse<T>(HttpRequestData req, T value, HttpStatusCode status)
        {
            try
            {
                return ContentResponse(req, status, "application/json; charset=utf-8", JsonConvert.SerializeObject(value, Json.JsonSerializerSettings));
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError($"Failed to serialize {typeof(T).Name} response body as JSON", ex).ToResponse(req);
            }
        }

        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        private static IActionResult XmlResponse<T>(HttpRequest req, T value, HttpStatusCode status)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                var writer = new Utf8StringWriter();
                serializer.Serialize(writer, value);
                return ContentResponse(req, status, "application/xml", writer.ToString());
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError($"Failed to serialize {typeof(T).Name} response body as XML", ex).ToResponse(req);
            }
        }

        public static IActionResult ContentResponse(HttpRequest req, HttpStatusCode statusCode, string contentType, string content)
        {
            AddCorsHeaders(req);
            AddEntityPermissionsHeaders(req);
            return new ContentResult()
            {
                StatusCode = (int)statusCode,
                Content = content,
                ContentType = contentType,
            };
        }

        public static IActionResult ContentResponse(HttpRequestData req, HttpStatusCode statusCode, string contentType, string content)
        {
            var resp = AddCorsHeaders(req);
            AddEntityPermissionsHeaders(req, resp);
            // TODO - This definately isn't going to work, it is ignorant of the response headers.
            return new ContentResult()
            {
                StatusCode = (int)statusCode,
                Content = content,
                ContentType = contentType,
            };
        }

        public static IActionResult StatusCodeResponse(HttpRequest req, HttpStatusCode statusCode)
        {
            AddCorsHeaders(req);
            AddEntityPermissionsHeaders(req);
            return new StatusCodeResult((int)statusCode);
        }

        private static void AddCorsHeaders(HttpRequest req)
        {
            // If CorsHosts are specified and the origin matches one of those hosts,
            // add the Cors Headers
            
            var origin = req.Headers.ContainsKey("Origin") ? req.Headers["Origin"].First() : "no origin";
            var corsHosts = Utils.Env("CorsHosts", required: false) ?? "no cors hosts";
            if (corsHosts == "*" || corsHosts.Split(",").Contains(origin))
            {
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", origin);
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "origin, content-type, accept, authorization");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, PUT, POST, DELETE, HEAD");
            }
        }

        private static HttpResponseData AddCorsHeaders(HttpRequestData req)
        {
            // If CorsHosts are specified and the origin matches one of those hosts,
            // add the Cors Headers
            var response = req.CreateResponse();


            req.Headers.TryGetValues("Origin", out var originHeaders);
            var origin = originHeaders?.FirstOrDefault() ?? "no origin";
            var corsHosts = Utils.Env("CorsHosts", required: false) ?? "no cors hosts";
            if (corsHosts == "*" || corsHosts.Split(",").Contains(origin))
            {
                response.Headers.Add("Access-Control-Allow-Origin", origin);
                response.Headers.Add("Access-Control-Allow-Headers", "origin, content-type, accept, authorization");
                response.Headers.Add("Access-Control-Allow-Credentials", "true");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, PUT, POST, DELETE, HEAD");
            }

            return response;
        }

        private static void AddEntityPermissionsHeaders(HttpRequest req)
        {
            if (req.HasEntityPermissions())
            {
                req.HttpContext.Response.Headers.Add(Headers.AccessControlExposeHeaders, Headers.XUserPermissions);
                req.HttpContext.Response.Headers.Add(Headers.XUserPermissions, req.GetEntityPermissions().ToString());
            }
        }

        private static void AddEntityPermissionsHeaders(HttpRequestData req, HttpResponseData response)
        {
            if (req.HasEntityPermissions())
            {
                response.Headers.Add(Headers.AccessControlExposeHeaders, Headers.XUserPermissions);
                response.Headers.Add(Headers.XUserPermissions, req.GetEntityPermissions().ToString());
            }
        }

        private static async Task SuccessResult(this Serilog.ILogger logger, HttpRequest request, HttpStatusCode statusCode) 
        {
            var requestBody = request.ContentLength > 0 ? await request.ReadAsStringAsync() : null;
            logger
                .ForContext(LogProps.StatusCode, (int)statusCode)
                .ForContext(LogProps.RequestBody, requestBody)
                .ForContext(LogProps.RecordBody, request.HttpContext.Items[LogProps.RecordBody])
                .Information($"[{{{LogProps.StatusCode}}}] {{{LogProps.RequestorNetid}}} - {{{LogProps.RequestMethod}}} {{{LogProps.Function}}}{{{LogProps.RequestParameters}}}{{{LogProps.RequestQuery}}}");
        }

        private static async Task SuccessResult(this Serilog.ILogger logger, HttpRequestData request, HttpStatusCode statusCode) 
        {
            var requestBody = request.Body.Length > 0 ? await request.ReadAsStringAsync() : null;
            logger
                .ForContext(LogProps.StatusCode, (int)statusCode)
                .ForContext(LogProps.RequestBody, requestBody)
                .ForContext(LogProps.RecordBody, request.FunctionContext.Items[LogProps.RecordBody])// TODO verify this works
                .Information($"[{{{LogProps.StatusCode}}}] {{{LogProps.RequestorNetid}}} - {{{LogProps.RequestMethod}}} {{{LogProps.Function}}}{{{LogProps.RequestParameters}}}{{{LogProps.RequestQuery}}}");
        }

        private static async Task FailureResult<T>(this Serilog.ILogger logger, HttpRequest request, Error error) 
        {
            var requestBody = request.ContentLength > 0 ? await request.ReadAsStringAsync() : null;
            logger
                .ForContext(LogProps.StatusCode, (int)error.StatusCode)
                .ForContext(LogProps.RequestBody, requestBody)
                .ForContext(LogProps.RecordBody, request.HttpContext.Items[LogProps.RecordBody])
                .ForContext(LogProps.ErrorMessages, JsonConvert.SerializeObject(error.Messages, Json.JsonSerializerSettings))
                .Error(error.Exception, $"[{{{LogProps.StatusCode}}}] {{{LogProps.RequestorNetid}}} - {{{LogProps.RequestMethod}}} {{{LogProps.Function}}}{{{LogProps.RequestParameters}}}{{{LogProps.RequestQuery}}}:\nErrors: {{{LogProps.ErrorMessages}}}.");
        }

        private static async Task FailureResult<T>(this Serilog.ILogger logger, HttpRequestData request, Error error) 
        {
            var requestBody = request.Body.Length > 0 ? await request.ReadAsStringAsync() : null;
            logger
                .ForContext(LogProps.StatusCode, (int)error.StatusCode)
                .ForContext(LogProps.RequestBody, requestBody)
                .ForContext(LogProps.RecordBody, request.FunctionContext.Items[LogProps.RecordBody])// TODO verify this still works
                .ForContext(LogProps.ErrorMessages, JsonConvert.SerializeObject(error.Messages, Json.JsonSerializerSettings))
                .Error(error.Exception, $"[{{{LogProps.StatusCode}}}] {{{LogProps.RequestorNetid}}} - {{{LogProps.RequestMethod}}} {{{LogProps.Function}}}{{{LogProps.RequestParameters}}}{{{LogProps.RequestQuery}}}:\nErrors: {{{LogProps.ErrorMessages}}}.");
        }
    }
}
