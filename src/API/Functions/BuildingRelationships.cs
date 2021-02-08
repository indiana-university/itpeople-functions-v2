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

namespace API.Functions
{
    public static class BuildingRelationships
    {

        public const string BuildingRelationshipsTitle = "Building Relationships";

		[FunctionName(nameof(BuildingRelationships.BuildingRelationshipsGetAll))]
        [OpenApiOperation(nameof(BuildingRelationships.BuildingRelationshipsGetAll), BuildingRelationshipsTitle, Summary="List all unit-building support relationships")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<BuildingRelationship>), Description="A collection of building support relationship records")]
        public static Task<IActionResult> BuildingRelationshipsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildingRelationships")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => BuildingRelationshipsRepository.GetAll())
                .Finally(r => Response.Ok(req, r));

        [FunctionName(nameof(BuildingRelationships.BuildingRelationshipsGetOne))]
        [OpenApiOperation(nameof(BuildingRelationships.BuildingRelationshipsGetOne), BuildingRelationshipsTitle, Summary = "Find a unit-building support relationships by ID")]
        [OpenApiParameter("relationshipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the building support relationship record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BuildingRelationship), Description="A building support relationship record")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No support relationship was found with the ID provided.")]
        public static Task<IActionResult> BuildingRelationshipsGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildingRelationships/{relationshipId}")] HttpRequest req, int relationshipId) 
            => Security.Authenticate(req)
                .Bind(_ => BuildingRelationshipsRepository.GetOne(relationshipId))
                .Finally(result => Response.Ok(req, result));
    }
}