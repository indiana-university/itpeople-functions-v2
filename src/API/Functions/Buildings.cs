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
    public static class Buildings
    {
		[FunctionName(nameof(Buildings.BuildingsGetAll))]
        [OpenApiOperation(nameof(Buildings.BuildingsGetAll), nameof(Buildings), Summary="List all buildings", Description = @"Get a list of university buildings." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Building>))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In=ParameterLocation.Query, Description="filter by building address/code/name, ex: 'ballantine'")]
        public static Task<IActionResult> BuildingsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildings")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => BuildingSearchParameters.Parse(req))
                .Bind(query => BuildingsRepository.GetAll(query))
                .Finally(buildings => Response.Ok(req, buildings));

        [FunctionName(nameof(Buildings.BuildingsGetOne))]
        [OpenApiOperation(nameof(Buildings.BuildingsGetOne), nameof(Buildings), Summary = "Find a building by ID")]
        [OpenApiParameter("buildingId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the building record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Building))]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No building was found with the ID provided.")]
        public static Task<IActionResult> BuildingsGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildings/{buildingId}")] HttpRequest req, int buildingId) 
            => Security.Authenticate(req)
                .Bind(_ => BuildingsRepository.GetOne(buildingId))
                .Finally(result => Response.Ok(req, result));

        [FunctionName(nameof(Buildings.BuildingsGetSupportingUnits))]
        [OpenApiOperation(nameof(Buildings.BuildingsGetSupportingUnits), nameof(Buildings), Summary = "List a building's supporting units", Description = @"A supporting unit provides IT services for the building.")]
        [OpenApiParameter("buildingId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the building record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<BuildingRelationship>))]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No building relationships were found with the buildingId provided.")]
        public static Task<IActionResult> BuildingsGetSupportingUnits(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildings/{buildingId}/supportingUnits")] HttpRequest req, int buildingId) 
            => Security.Authenticate(req)
                .Bind(_ => BuildingsRepository.GetSupportingUnits(buildingId))
                .Finally(result => Response.Ok(req, result));
    }
}