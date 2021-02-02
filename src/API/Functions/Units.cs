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
    public static class Units
    {
        private const string NotFoundError = "No unit found with ID (`unitId`).\\\n **or**\\\n No parent unit found with ID (`parentId`).";

        [FunctionName(nameof(Units.UnitsGetAll))]
        [OpenApiOperation(nameof(Units.UnitsGetAll), nameof(Units), Summary="List all IT units", Description = @"Search for IT units by name and/or description. If no search term is provided, lists all top-level IT units." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Unit>))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
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
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Unit))]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> UnitsGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}")] HttpRequest req, int unitId) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetOne(unitId))
                .Finally(result => Response.Ok(req, result));
        
        [FunctionName(nameof(Units.CreateUnit))]
        [OpenApiOperation(nameof(Units.CreateUnit), nameof(Units), Summary = "Create a unit", Description = "_Authorization_: Unit creation is restricted to service administrators.")]
        [OpenApiRequestBody("application/json", typeof(UnitRequest), Required=true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Unit))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description = UnitRequest.MalformedRequest)]
        [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "You do not have permission to create a unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiError), Description = NotFoundError)]
        public static Task<IActionResult> CreateUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "units")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
                .Bind(_ => Request.DeserializeBody<UnitRequest>(req))
                .Bind(body => UnitsRepository.CreateUnit(body))
                .Finally(result => Response.Created("units", result));
        
        [FunctionName(nameof(Units.UpdateUnit))]
        [OpenApiOperation(nameof(Units.UpdateUnit), nameof(Units), Summary = "Update a unit", Description = "_Authorization_: Units can be modified by any unit member that has either the `Owner` or `ManageMembers` permission on their membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
        [OpenApiRequestBody("application/json", typeof(UnitRequest), Required=true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Unit))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description = UnitRequest.MalformedRequest)]
        [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "You do not have permission to modify this unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiError), Description = NotFoundError)]
        public static Task<IActionResult> UpdateUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "units/{unitId}")] HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
                .Bind(_ => Request.DeserializeBody<UnitRequest>(req))
                .Bind(body => UnitsRepository.UpdateUnit(body, unitId))
                .Finally(result => Response.Ok(req, result));
        
        [FunctionName(nameof(Units.DeleteUnit))]
        [OpenApiOperation(nameof(Units.DeleteUnit), nameof(Units), Summary = "Delete a unit", Description = "_Authorization_: Unit deletion is restricted to service administrators.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
        [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "You do not have permission to modify this unit.")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No unit was found with the provided ID.")]
        [OpenApiResponseWithBody(HttpStatusCode.Conflict, "application/json", typeof(ApiError), Description = "Unit `unitId` has child units, with ids: `list of child unitIds`. These must be reassigned prior to deletion.")]
        public static Task<IActionResult> DeleteUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "units/{unitId}")] HttpRequest req, int unitId) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
                .Bind(_ => UnitsRepository.DeleteUnit(unitId))
                .Finally(result => Response.NoContent(req, result));
    }
}