using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace API.Functions
{
    public static class Authorization
	{
		[Function(nameof(Authorization.ExchangeOAuthCodeForToken))]
		[OpenApiIgnore]
		public static Task<HttpResponseData> ExchangeOAuthCodeForToken(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth")] HttpRequestData req)
			=> Request.GetRequiredQueryParam(req, "oauth_code")
				.Bind(code => Security.ExhangeOAuthCodeForToken(req, code))
				.Finally(jwt => Response.Ok(req, jwt));

		[Function(nameof(Authorization.Options))]
		[OpenApiIgnore]
		public static Task<HttpResponseData> Options(
			[HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*url}")] HttpRequestData req)
			=> Response.Ok(req, Pipeline.Success(string.Empty));

		
	}
}