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

namespace API.Middleware
{
    public static class Response
    {
        public class Headers
        {
            public const string XUserPermissions = "x-user-permissions";
            public const string AccessControlExposeHeaders = "Access-Control-Expose-Headers";
        }
        
        private static HttpResponseMessage Generate<T>(
            HttpRequest req, 
            Result<T, Error> result, 
            HttpStatusCode statusCode, 
            Func<T,HttpResponseMessage> resultGenerator)
        {
            var logger = Logging.GetLogger(req);
            if (result.IsSuccess)
            {
                if (statusCode != HttpStatusCode.OK)
                    logger.SuccessResult(req, statusCode);
                return resultGenerator(result.Value);
            }
            else 
            {
                logger.FailureResult<T>(req, result.Error);
                return result.Error.ToResponse(req);
            }
        }

        /// <summary>Return an HTTP 200 response with content, or an appropriate HTTP error response.</summary>
        public static HttpResponseMessage Ok<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.OK, val => JsonResponse(req, val, HttpStatusCode.OK));

        /// <summary>Return an HTTP 201 response with content, with the URL for the resource and the resource itself.</summary>
        public static HttpResponseMessage Created<T>(HttpRequest req, Result<T, Error> result) where T : Models.Entity
		    => Generate(req, result, HttpStatusCode.Created, val => JsonResponse(req, val, HttpStatusCode.Created));

        /// <summary>Return an HTTP 204 indicating success, but nothing to return.</summary>
        public static HttpResponseMessage NoContent<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.NoContent, val => HttpResponse(req, HttpStatusCode.NoContent));


        /// <summary>Return an HTTP 200 response with XML content, or an appropriate HTTP error response.</summary>
        public static HttpResponseMessage OkXml<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.OK, val => XmlResponse(req, val, HttpStatusCode.OK));

        private static HttpResponseMessage JsonResponse<T>(HttpRequest req, T value, HttpStatusCode status)
        {
            try
            {
                return HttpResponse(req, status, "application/json", JsonConvert.SerializeObject(value, Json.JsonSerializerSettings));
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

        private static HttpResponseMessage XmlResponse<T>(HttpRequest req, T value, HttpStatusCode status)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                var writer = new Utf8StringWriter();
                serializer.Serialize(writer, value);
                return HttpResponse(req, status, "application/xml", writer.ToString());
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError($"Failed to serialize {typeof(T).Name} response body as XML", ex).ToResponse(req);
            }
        }

        public static HttpResponseMessage HttpResponse(HttpRequest req, HttpStatusCode statusCode, string contentType, string content)
        {
            var resp = HttpResponse(req, statusCode);
            resp.Content = new StringContent(content, Encoding.UTF8, contentType);
            return resp;
        }

        public static HttpResponseMessage HttpResponse(HttpRequest req, HttpStatusCode statusCode)
        {
            var resp = new HttpResponseMessage(statusCode);
            AddCorsHeaders(req, resp);
            AddEntityPermissionsHeaders(req, resp);
            return resp;
        }

        private static void AddCorsHeaders(HttpRequest req, HttpResponseMessage resp)
        {
            // If CorsHosts are specified and the origin matches one of those hosts,
            // add the Cors Headers
            var origin = req.Headers.ContainsKey("Origin") ? req.Headers["Origin"].First() : "no origin";
            var corsHosts = Utils.Env("CorsHosts", required: false) ?? "";
            if (corsHosts == "*" || corsHosts.Split(",").Contains(origin))
            {
                resp.Headers.Add("Access-Control-Allow-Origin", origin);
                resp.Headers.Add("Access-Control-Allow-Headers", "origin, content-type, accept, authorization");
                resp.Headers.Add("Access-Control-Allow-Credentials", "true");
                resp.Headers.Add("Access-Control-Allow-Methods", "GET, PUT, POST, DELETE, HEAD");
            }
        }

        private static void AddEntityPermissionsHeaders(HttpRequest req, HttpResponseMessage resp)
        {
            if (req.HasEntityPermissions())
            {
                resp.Headers.Add(Headers.AccessControlExposeHeaders, Headers.XUserPermissions);
                resp.Headers.Add(Headers.XUserPermissions, req.GetEntityPermissions().ToString());
            }
        }

        private static void SuccessResult(this Serilog.ILogger logger, HttpRequest request, HttpStatusCode statusCode) 
            => logger
                .ForContext(LogProps.StatusCode, (int)statusCode)
                .ForContext(LogProps.RequestBody, request.ContentLength > 0 ? request.ReadAsStringAsync().Result : null)
                .ForContext(LogProps.RecordBody, request.HttpContext.Items[LogProps.RecordBody])
                .Information($"[{{{LogProps.StatusCode}}}] {{{LogProps.RequestorNetid}}} - {{{LogProps.RequestMethod}}} {{{LogProps.Function}}}{{{LogProps.RequestParameters}}}{{{LogProps.RequestQuery}}}");

        private static void FailureResult<T>(this Serilog.ILogger logger, HttpRequest request, Error error) 
            => logger
                .ForContext(LogProps.StatusCode, (int)error.StatusCode)
                .ForContext(LogProps.RequestBody, request.ContentLength > 0 ? request.ReadAsStringAsync().Result : null)
                .ForContext(LogProps.RecordBody, request.HttpContext.Items[LogProps.RecordBody])
                .ForContext(LogProps.ErrorMessages, JsonConvert.SerializeObject(error.Messages))
                .Error(error.Exception, $"[{{{LogProps.StatusCode}}}] {{{LogProps.RequestorNetid}}} - {{{LogProps.RequestMethod}}} {{{LogProps.Function}}}{{{LogProps.RequestParameters}}}{{{LogProps.RequestQuery}}}:\nErrors: {{{LogProps.ErrorMessages}}}.");
    }
}
