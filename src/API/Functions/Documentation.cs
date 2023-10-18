using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Functions
{
    public static class Documentation
    {
        [FunctionName(nameof(Documentation.RenderOpenApiJson))]
        public static IActionResult RenderOpenApiJson(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi.json")] HttpRequest req) 
                => new RedirectResult("/swagger.json");
        

        [FunctionName(nameof(Documentation.RenderApiUI))]
        public static IActionResult RenderApiUI(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/")] HttpRequest req)
                => new ContentResult(){ StatusCode=200, Content = ApiUIMarkup, ContentType="text/html" };

        public static string ApiUIMarkup = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <title>IT People API</title>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <link href=""https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700"" rel=""stylesheet"">
    <style>
        body {
            margin: 0;
            padding: 0;
        }
        
        a.skip {
            position: absolute;
            left: -10000px;
            top: auto;
            width: 1px;
            height: 1px;
            overflow: hidden;
        }

        a.skip:focus {
            position: static;
            width: auto;
            height: auto;
        }
    </style>
</head>
<body>
    <a href=""#section/Description"" class=""skip"">Skip to main content</a>
    <redoc spec-url='openapi/v3.json'></redoc>
    <script src=""https://cdn.jsdelivr.net/npm/redoc@next/bundles/redoc.standalone.js""> </script>
</body>
</html>
";
    }
}
