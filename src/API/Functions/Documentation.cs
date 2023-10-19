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

        /*
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

    <div style=""text-align: center""><h1 style=""font-size: 1em"">Indiana University</h1><p style=""line-height: 1.5"">Service Management Technologies</p></div>
</body>
</html>
";
        */
        public static string ApiUIMarkup = @"
<!-- HTML for static distribution bundle build -->
<!DOCTYPE html>
<html lang=""en"">
<head>
	<meta charset=""UTF-8"">
	<title>Siteimprove API</title> <!-- SITEIMPROVE EDIT -->
	<link rel=""stylesheet"" type=""text/css"" href=""https://itpeople-test.iu.edu/swagger-ui/swagger-ui.css"" />
	<link rel=""stylesheet"" type=""text/css"" href=""https://itpeople-test.iu.edu/swagger-ui/index.css"" />
	<link rel=""icon"" type=""image/png"" href=""https://itpeople-test.iu.edu/swagger-ui/favicon-32x32.png"" sizes=""32x32"" />
	<link rel=""icon"" type=""image/png"" href=""https://itpeople-test.iu.edu/swagger-ui/favicon-16x16.png"" sizes=""16x16"" />
	<!-- SITEIMPROVE EDIT -->
	<!-- <link href=""siteimprove.css"" type=""text/css"" rel=""stylesheet"" /> -->
	<!-- <link href=""siteimprove-swagger-theme.css"" type=""text/css"" rel=""stylesheet"" /> -->
	<style>
		html {
			box-sizing: border-box;
			overflow: -moz-scrollbars-vertical;
			overflow-y: scroll;
		}

		*,
		*:before,
		*:after {
			box-sizing: inherit;
		}

		body {
			margin: 0;
			background: #fafafa;
		}

		.top-fixed {
			position: fixed;
			top: 0;
			left: 0;
			background-color: #eee;
			padding: 3px 10px;
		}
	</style>
</head>

<body>
	<div id=""swagger-ui""></div>

	<script src=""https://itpeople-test.iu.edu/swagger-ui/swagger-ui-bundle.js"" charset=""UTF-8""></script>
	<script src=""https://itpeople-test.iu.edu/swagger-ui/swagger-ui-standalone-preset.js"" charset=""UTF-8""></script>
	<script src=""https://itpeople-test.iu.edu/swagger-ui/swagger-initializer.js"" charset=""UTF-8""> </script>
	<!-- SITEIMPROVE EDIT -->
	<!-- <script src=""https://itpeople-test.iu.edu/swagger-ui/jquery-3.6.0.min.js"" type=""text/javascript""></script> -->
	<!-- <script src=""https://itpeople-test.iu.edu/swagger-ui/siteimprove.js"" type=""text/javascript""></script> -->

	<script>
		window.onload = function () {
			var url = ""/openapi/v3.json"";

			// Pre load translate...
			if (window.SwaggerTranslator) {
				window.SwaggerTranslator.translate();
			}

			// Begin Swagger UI call region
			const ui = SwaggerUIBundle({
				url: url,
				dom_id: '#swagger-ui',
				deepLinking: true,
				presets: [
					SwaggerUIBundle.presets.apis,
					SwaggerUIStandalonePreset
				],
				plugins: [
					SwaggerUIBundle.plugins.DownloadUrl
				],
				layout: ""StandaloneLayout"",
				onComplete: function (swaggerApi, swaggerUi) {
					if (window.SwaggerTranslator) {
						window.SwaggerTranslator.translate();
					}
				},
				onFailure: function (data) {
					log(""Unable to Load SwaggerUI"");
				},
				docExpansion: ""none"",
				jsonEditor: false,
				// defaultModelRendering: window.siteimprove.optionDefaultModelRendering(""False"" === ""True""), // SITEIMPROVE EDIT
				showRequestHeaders: false,
				// requestInterceptor: function (req) { req.url = req.url.replace(""https://localhost/"", ""https://localhost:443/""); return req; }
			});
			// End Swagger UI call region

			window.ui = ui;
		};
	</script>
</body>
</html>
        ";
    }
}
