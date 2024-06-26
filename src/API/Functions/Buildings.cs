using Microsoft.Azure.Functions.Worker;

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
using System.Linq;

namespace API.Functions
{
    public static class Buildings
    {
        [Function(nameof(Buildings.BuildingsGetAll))]
        [OpenApiOperation(nameof(Buildings.BuildingsGetAll), nameof(Buildings), Summary = "List all buildings", Description = @"Get a list of university buildings.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Building>), Description = "A collection of building records")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description = "The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In = ParameterLocation.Query, Description = "filter by building address/code/name, ex: 'ballantine'")]
        public static Task<IActionResult> BuildingsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildings")] HttpRequest req)
            => Security.Authenticate(req)
                .Bind(_ => BuildingSearchParameters.Parse(req))
                .Bind(query => BuildingsRepository.GetAll(query))
                .Finally(buildings => Response.Ok(req, buildings));

        [Function(nameof(Buildings.BuildingsGetOne))]
        [OpenApiOperation(nameof(Buildings.BuildingsGetOne), nameof(Buildings), Summary = "Find a building by ID")]
        [OpenApiParameter("buildingId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the building record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Building), Description = "A building record")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No building was found with the ID provided.")]
        public static Task<IActionResult> BuildingsGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildings/{buildingId}")] HttpRequest req, string buildingId)
            => Utils.ConvertParam(buildingId, nameof(buildingId))
                .Bind(id => BuildingsGetOneInternal(req, id))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<Building, Error>> BuildingsGetOneInternal(HttpRequest req, int buildingId)
            => Security.Authenticate(req)
                .Bind(_ => BuildingsRepository.GetOne(buildingId));

        [Function(nameof(Buildings.BuildingsGetSupportingUnits))]
        [OpenApiOperation(nameof(Buildings.BuildingsGetSupportingUnits), nameof(Buildings), Summary = "List a building's supporting units", Description = @"A supporting unit provides IT services for the building.")]
        [OpenApiParameter("buildingId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the building record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<BuildingRelationshipResponse>), Description = "A collection of building relationship records")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No building relationships were found with the buildingId provided.")]
        public static Task<IActionResult> BuildingsGetSupportingUnits(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildings/{buildingId}/supportingUnits")] HttpRequest req, string buildingId)
            => Utils.ConvertParam(buildingId, nameof(buildingId))
                .Bind(id => BuildingsGetSupportingUnitsInternal(req, id)) //make sure the building exists before trying to get its supporting units
                .Bind(br => Pipeline.Success(br.Select(brx => new BuildingRelationshipResponse(brx)).ToList()))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<List<BuildingRelationship>, Error>> BuildingsGetSupportingUnitsInternal(HttpRequest req, int buildingId)
            => Security.Authenticate(req)
                .Bind(_ => BuildingsRepository.GetOne(buildingId)) //make sure the building exists before trying to get its supporting units
                .Bind(_ => BuildingsRepository.GetSupportingUnits(buildingId));
    }
}