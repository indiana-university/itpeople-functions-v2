using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net.Http;

namespace API.Functions
{
    public static class Authorization
	{
		[FunctionName(nameof(Authorization.ExchangeOAuthCodeForToken))]
		[OpenApiIgnore]
		public static Task<IActionResult> ExchangeOAuthCodeForToken(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth")] HttpRequest req)
			=> Request.GetRequiredQueryParam(req, "oauth_code")
				.Bind(code => Security.ExhangeOAuthCodeForToken(req, code))
				.Finally(jwt => Response.Ok(req, jwt));

		[FunctionName(nameof(Authorization.Options))]
		[OpenApiIgnore]
		public static IActionResult Options(
			[HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*url}")] HttpRequest req)
			=> Response.Ok(req, Pipeline.Success(string.Empty));

		
	}
}