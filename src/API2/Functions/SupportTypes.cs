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
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace API.Functions
{
	public static class SupportTypes
	{

		[Function(nameof(SupportTypes.SupportTypesGetAll))]
		[OpenApiOperation(nameof(SupportTypes.SupportTypesGetAll), "Support Types", Summary = "List all support types")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<SupportType>), Description = "A collection of support types")]
		public static Task<HttpResponseData> SupportTypesGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supportTypes")] HttpRequestData req)
			=> Security.Authenticate(req)
				.Bind(query => SupportTypesRepository.GetAll())
				.Finally(results => Response.Ok(req, results));

	}
}