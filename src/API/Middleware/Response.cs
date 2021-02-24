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

namespace API.Middleware
{
    public static class Response
    {
        public class Headers
        {
            public const string XUserPermissions = "x-user-permissions";
            public const string AccessControlExposeHeaders = "Access-Control-Expose-Headers";
        }
        
        private static IActionResult Generate<T>(
            HttpRequest req, 
            Result<T, Error> result, 
            HttpStatusCode statusCode, 
            Func<T,IActionResult> resultGenerator)
        {
            var logger = Logging.GetLogger(req);
            if (result.IsSuccess)
            {
                logger.SuccessResult(req, statusCode);
                return resultGenerator(result.Value);
            }
            else 
            {
                logger.FailureResult<T>(req, result.Error);
                return result.Error.ToActionResult();
            }
        }

        /// <summary>Return an HTTP 200 response with content, or an appropriate HTTP error response.</summary>
        public static IActionResult Ok<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.OK, val => JsonResult(val, HttpStatusCode.OK));

        /// <summary>Return an HTTP 201 response with content, with the URL for the resource and the resource itself.</summary>
        public static IActionResult Created<T>(HttpRequest req, Result<T, Error> result) where T : Models.Entity
		    => Generate(req, result, HttpStatusCode.Created, val => JsonResult(val, HttpStatusCode.Created));

        /// <summary>Return an HTTP 204 indicating success, but nothing to return.</summary>
        public static IActionResult NoContent<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.NoContent, val => new NoContentResult());


        /// <summary>Return an HTTP 200 response with XML content, or an appropriate HTTP error response.</summary>
        public static IActionResult OkXml<T>(HttpRequest req, Result<T, Error> result)
            => Generate(req, result, HttpStatusCode.OK, val => XmlResult(val, HttpStatusCode.OK));

        private static IActionResult JsonResult<T>(T value, HttpStatusCode status)
        {
            try
            {
                return new ContentResult()
                {
                    StatusCode = (int)status,
                    ContentType = "application/json; charset=utf-8",
                    Content = JsonConvert.SerializeObject(value, Json.JsonSerializerSettings),
                };
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError($"Failed to serialize {typeof(T).Name} response body as JSON", ex).ToActionResult();
            }
        }

        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        private static IActionResult XmlResult<T>(T value, HttpStatusCode status)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                var writer = new Utf8StringWriter();
                serializer.Serialize(writer, value);
                return new ContentResult(){
                    StatusCode = (int)status,
                    ContentType="application/xml",
                    Content=writer.ToString(),
                };
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError($"Failed to serialize {typeof(T).Name} response body as XML", ex).ToActionResult();
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
