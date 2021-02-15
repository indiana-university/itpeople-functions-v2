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
    public static class SupportRelationships
    {
        public const string SupportRelationshipsTitle = "Support Relationships";

		[FunctionName(nameof(SupportRelationships.SupportRelationshipsGetAll))]
        [OpenApiOperation(nameof(SupportRelationships.SupportRelationshipsGetAll), SupportRelationshipsTitle, Summary="List all unit-department support relationships")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<SupportRelationshipResponse>), Description="A collection of department support relationship records")]
        public static Task<IActionResult> SupportRelationshipsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supportRelationships")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(query => SupportRelationshipsRepository.GetAll())                
                .Bind(sr => Pipeline.Success(SupportRelationshipResponse.ConvertList(sr)))
                .Finally(results => Response.Ok(req, results));

        [FunctionName(nameof(SupportRelationships.SupportRelationshipsGetOne))]
		[OpenApiOperation(nameof(SupportRelationships.SupportRelationshipsGetOne), SupportRelationshipsTitle, Summary = "Find a unit-department support relationships by ID")]
		[OpenApiParameter("relationshipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the department support relationship record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(SupportRelationshipResponse), Description = "A department support relationship record")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No support relationship was found with the ID provided.")]
		public static Task<IActionResult> SupportRelationshipsGetOne(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supportRelationships/{relationshipId}")] HttpRequest req, int relationshipId)
			=> Security.Authenticate(req)
				.Bind(_ => SupportRelationshipsRepository.GetOne(relationshipId))
                .Bind(sr => Pipeline.Success(new SupportRelationshipResponse(sr)))
				.Finally(result => Response.Ok(req, result));

    }
}