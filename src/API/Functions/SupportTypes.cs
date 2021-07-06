using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

using API.Data;
using API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using System.Collections.Generic;
using Models;
using Microsoft.OpenApi.Models;
using System.Net.Mime;

namespace API.Functions
{
	public static class SupportTypes
	{

		[FunctionName(nameof(SupportTypes.SupportTypesGetAll))]
		[OpenApiOperation(nameof(SupportTypes.SupportTypesGetAll), "Support Types", Summary = "List all support types")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<SupportType>), Description = "A collection of support types")]
		public static Task<IActionResult> SupportTypesGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supportTypes")] HttpRequest req)
			=> Security.Authenticate(req)
				.Bind(query => SupportTypesRepository.GetAll())
				.Finally(results => Response.Ok(req, results));

	}
}