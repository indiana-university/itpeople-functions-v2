using Microsoft.Azure.Functions.Worker;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Functions
{
    public static class Documentation
    {
        [Function(nameof(Documentation.RenderOpenApiJson))]
        public static IActionResult RenderOpenApiJson(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi.json")] HttpRequest req) 
                => new RedirectResult("/swagger.json");
        

        // [Function(nameof(Documentation.RenderApiUI))]
        public static IActionResult RenderApiUI(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/")] HttpRequest req)
                => new ContentResult(){ StatusCode=200, Content = ApiUIMarkup, ContentType="text/html" };

        public static string ApiUIMarkup = @"
<!-- HTML for static distribution bundle build -->
<!DOCTYPE html>
<html lang=""en"">
<head>
	<meta charset=""UTF-8"">
	<title>IT People API</title> <!-- SITEIMPROVE EDIT -->
	<link rel=""stylesheet"" type=""text/css"" href=""https://itpeople-test.iu.edu/swagger-ui/swagger-ui.css"" />
	<link rel=""stylesheet"" type=""text/css"" href=""https://itpeople-test.iu.edu/swagger-ui/index.css"" />
	<link rel=""stylesheet"" type=""text/css"" href=""https://itpeople-test.iu.edu/swagger-ui/cssd.css"" />
	<link rel=""icon"" type=""image/png"" href=""https://itpeople-test.iu.edu/swagger-ui/favicon-32x32.png"" sizes=""32x32"" />
	<link rel=""icon"" type=""image/png"" href=""https://itpeople-test.iu.edu/swagger-ui/favicon-16x16.png"" sizes=""16x16"" />
</head>

<body>
	<a href=""#swagger-ui"" class=""skip"">Skip to main content</a>
	<div id=""swagger-ui""></div>

	<script src=""https://itpeople-test.iu.edu/swagger-ui/swagger-ui-bundle.js"" charset=""UTF-8""></script>
	<script src=""https://itpeople-test.iu.edu/swagger-ui/swagger-ui-standalone-preset.js"" charset=""UTF-8""></script>
	<script src=""https://itpeople-test.iu.edu/swagger-ui/swagger-initializer.js"" charset=""UTF-8""> </script>
	<script src=""https://itpeople-test.iu.edu/swagger-ui/cssd.js"" charset=""UTF-8""></script><!-- This is where the swagger-ui is run to populate div#swagger-ui -->
</body>
</html>
        ";
    }
}
