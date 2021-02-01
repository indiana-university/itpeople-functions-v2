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
        [OpenApiParameter("name", Type = typeof(string), In = ParameterLocation.Query, Required = true, Description = "")]
        [OpenApiParameter("description", Type = typeof(string), In = ParameterLocation.Query, Required = false, Description = "")]
        [OpenApiParameter("url", Type = typeof(string), In = ParameterLocation.Query, Required = false, Description = "")]
        [OpenApiParameter("email", Type = typeof(string), In = ParameterLocation.Query, Required = false, Description = "")]
        [OpenApiParameter("parentId", Type = typeof(int), In = ParameterLocation.Query, Required = false, Description = "The Unit Id of the parent Unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Unit))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description = UnitsRepository.MalformedRequest)]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, "application/json", typeof(ApiError), Description = UnitsRepository.Forbidden)]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiError), Description = UnitsRepository.ParentNotFound)]
        public static Task<IActionResult> CreateUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "units")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeCreate(perms))
                .Bind(_ => Request.DeserializeBody<UnitRequest>(req))
                .Bind(body => UnitsRepository.CreateUnit(body))
                .Finally(result => Response.Created("units", result));
        
        [FunctionName(nameof(Units.UpdateUnit))]
        [OpenApiOperation(nameof(Units.UpdateUnit), nameof(Units), Summary = "Update a unit", Description = "_Authorization_: Units can be modified by any unit member that has either the `Owner` or `ManageMembers` permission on their membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiParameter("name", Type = typeof(string), In = ParameterLocation.Query, Required = true, Description = "")]
        [OpenApiParameter("description", Type = typeof(string), In = ParameterLocation.Query, Required = false, Description = "")]
        [OpenApiParameter("url", Type = typeof(string), In = ParameterLocation.Query, Required = false, Description = "")]
        [OpenApiParameter("email", Type = typeof(string), In = ParameterLocation.Query, Required = false, Description = "")]
        [OpenApiParameter("parentId", Type = typeof(int), In = ParameterLocation.Query, Required = false, Description = "The Unit Id of the parent Unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Unit))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description = UnitsRepository.MalformedRequest)]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, "application/json", typeof(ApiError), Description = UnitsRepository.Forbidden)]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiError), Description = UnitsRepository.ParentNotFound)]
        public static Task<IActionResult> UpdateUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "units/{unitId}")] HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
                .Bind(_ => Request.DeserializeBody<UnitRequest>(req))
                .Bind(body => UnitsRepository.UpdateUnit(body, unitId))
                .Finally(result => Response.Ok(req, result));
    }
}