using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace API.Functions
{
    // TODO Revisit open API once the rest is working.
    public static class Documentation
    {
        [Function(nameof(Documentation.RenderOpenApiJson))]
        public static HttpResponseData RenderOpenApiJson([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi.json")] HttpRequestData req) 
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.MovedPermanently);
            response.Headers.Add("Location", "/swagger.json");

            return response;
        }
        

        // TODO - Figure out why the "/" route doesn't work.
        [Function(nameof(Documentation.RenderApiUI))]
        public static HttpResponseData RenderApiUI([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "docs")] HttpRequestData req)
         {
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("content-type", "text/html");

            var buffer = System.Text.Encoding.UTF8.GetBytes(ApiUIMarkup);
            response.Body.Write(buffer);

            return response;
         }

        public static string ApiUIMarkup = @"
<!DOCTYPE html>
<html>
<head>
    <title>IT People API</title>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <link href=""https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700"" rel=""stylesheet"">
    <style> body { margin: 0; padding: 0; }</style>
</head>
<body>
    <redoc spec-url='openapi/v3.json'></redoc>
    <script src=""https://cdn.jsdelivr.net/npm/redoc@next/bundles/redoc.standalone.js""> </script>
</body>
</html>
";
    }
}
