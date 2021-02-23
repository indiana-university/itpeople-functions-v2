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
using System.Linq;
using Models.Enums;

namespace API.Functions
{
    public static class Units
    {
        private const string NotFoundError = "No unit found with ID (`unitId`).\\\n **or**\\\n No parent unit found with ID (`parentId`).";

        [FunctionName(nameof(Units.UnitsGetAll))]
        [OpenApiOperation(nameof(Units.UnitsGetAll), nameof(Units), Summary="List all IT units", Description = @"Search for IT units by name and/or description. If no search term is provided, lists all top-level IT units." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In=ParameterLocation.Query, Description="filter by unit name/description, ex: `Parks`")]
        public static Task<IActionResult> UnitsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor))// Set headers saying what the requestor can do to these units
                .Bind(_ => UnitSearchParameters.Parse(req))
                .Bind(query => UnitsRepository.GetAll(query))
                .Finally(units => Response.Ok(req, units));

        [FunctionName(nameof(Units.UnitsGetOne))]
        [OpenApiOperation(nameof(Units.UnitsGetOne), nameof(Units), Summary = "Find a unit by ID")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitResponse))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> UnitsGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}")] HttpRequest req, int unitId) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetOne(unitId))
                .Finally(result => Response.Ok(req, result));
        
        [FunctionName(nameof(Units.CreateUnit))]
        [OpenApiOperation(nameof(Units.CreateUnit), nameof(Units), Summary = "Create a unit", Description = "_Authorization_: Unit creation is restricted to service administrators.")]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UnitRequest), Required=true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitResponse))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = UnitRequest.MalformedRequest)]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to create a unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = NotFoundError)]
        public static Task<IActionResult> CreateUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "units")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
                .Bind(_ => Request.DeserializeBody<UnitRequest>(req))
                .Bind(body => UnitsRepository.CreateUnit(body))
                .Finally(result => Response.Created(req, result));
        
        [FunctionName(nameof(Units.UpdateUnit))]
        [OpenApiOperation(nameof(Units.UpdateUnit), nameof(Units), Summary = "Update a unit", Description = "_Authorization_: Units can be modified by any unit member that has either the `Owner` or `ManageMembers` permission on their membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UnitRequest), Required=true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitResponse))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = UnitRequest.MalformedRequest)]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to modify this unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = NotFoundError)]
        public static Task<IActionResult> UpdateUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "units/{unitId}")] HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
                .Bind(_ => Request.DeserializeBody<UnitRequest>(req))
                .Bind(body => UnitsRepository.UpdateUnit(req, body, unitId))
                .Finally(result => Response.Ok(req, result));
        
        [FunctionName(nameof(Units.DeleteUnit))]
        [OpenApiOperation(nameof(Units.DeleteUnit), nameof(Units), Summary = "Delete a unit", Description = "_Authorization_: Unit deletion is restricted to service administrators.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to modify this unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        [OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "Unit `unitId` has child units, with ids: `list of child unitIds`. These must be reassigned prior to deletion.")]
        public static Task<IActionResult> DeleteUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "units/{unitId}")] HttpRequest req, int unitId) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
                .Bind(_ => UnitsRepository.DeleteUnit(req, unitId))
                .Finally(result => Response.NoContent(req, result));

        [FunctionName(nameof(Units.GetUnitChildren))]
        [OpenApiOperation(nameof(Units.GetUnitChildren), nameof(Units), Summary = "List all unit children ", Description = "List all units that fall below this unit in an organizational hierarchy.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> GetUnitChildren(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}/children")] HttpRequest req, int unitId) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetChildren(req, unitId))
                .Finally(result => Response.Ok(req, result));

        [FunctionName(nameof(Units.GetUnitMembers))]
        [OpenApiOperation(nameof(Units.GetUnitMembers), nameof(Units), Summary = "List all unit members", Description = "List all people who do IT work for this unit along with any vacant positions.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitMemberResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> GetUnitMembers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}/members")] HttpRequest req, int unitId) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetMembers(req, unitId))
				.Bind(ms => Pipeline.Success(ms.Select(e => e.ToUnitMemberResponse(req.GetEntityPermissions()))))
                .Finally(result => Response.Ok(req, result));

        [FunctionName(nameof(Units.GetUnitSupportedBuildings))]
        [OpenApiOperation(nameof(Units.GetUnitSupportedBuildings), nameof(Units), Summary = "List all supported buildings", Description = "List all buildings that receive IT support from this unit.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<BuildingRelationshipResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> GetUnitSupportedBuildings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}/supportedBuildings")] HttpRequest req, int unitId) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetSupportedBuildings(req, unitId))
                .Finally(result => Response.Ok(req, result));
    }
}