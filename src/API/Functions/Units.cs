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
using Models.Enums;

namespace API.Functions
{
    public static class Units
    {
        private const string NotFoundError = "No unit found with ID (`unitId`).\\\n **or**\\\n No parent unit found with ID (`parentId`).";

        [Function(nameof(Units.UnitsGetAll))]
        [OpenApiOperation(nameof(Units.UnitsGetAll), nameof(Units), Summary = "List all IT units", Description = @"Search for IT units by name and/or description. If no search term is provided, lists all top-level IT units.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In = ParameterLocation.Query, Description = "filter by unit name/description, ex: `Parks`")]
        public static Task<IActionResult> UnitsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units")] HttpRequest req)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineServiceAdminPermissions(req, requestor))// Set headers saying what the requestor can do to these units
                .Bind(_ => UnitSearchParameters.Parse(req))
                .Bind(query => UnitsRepository.GetAll(query))
                .Finally(units => Response.Ok(req, units));

        [Function(nameof(Units.UnitsGetOne))]
        [OpenApiOperation(nameof(Units.UnitsGetOne), nameof(Units), Summary = "Find a unit by ID")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitResponse))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> UnitsGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
                .Bind(id => UnitsGetOneInternal(req, id))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<Unit, Error>> UnitsGetOneInternal(HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestor, unitId, UnitPermissions.Owner, PermsGroups.GetPut))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetOne(unitId));

        [Function(nameof(Units.CreateUnit))]
        [OpenApiOperation(nameof(Units.CreateUnit), nameof(Units), Summary = "Create a unit", Description = "_Authorization_: Unit creation is restricted to service administrators.")]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UnitRequest), Required = true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitResponse))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = UnitRequest.MalformedRequest)]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to create a unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = NotFoundError)]
        public static Task<IActionResult> CreateUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "units")] HttpRequest req)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineServiceAdminPermissions(req, requestor))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
                .Bind(_ => Request.DeserializeBody<UnitRequest>(req))
                .Bind(body => UnitsRepository.CreateUnit(body))
                .Finally(result => Response.Created(req, result));

        [Function(nameof(Units.UpdateUnit))]
        [OpenApiOperation(nameof(Units.UpdateUnit), nameof(Units), Summary = "Update a unit", Description = "_Authorization_: Units can be modified by any unit member that has either the `Owner` or `ManageMembers` permission on their membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UnitRequest), Required = true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitResponse))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = UnitRequest.MalformedRequest)]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to modify this unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = NotFoundError)]
        public static Task<IActionResult> UpdateUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "units/{unitId}")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
                .Bind(id => UpdateUnitInternal(req, id))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<Unit, Error>> UpdateUnitInternal(HttpRequest req, int unitId) => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestor, unitId, UnitPermissions.Owner))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
                .Bind(_ => Request.DeserializeBody<UnitRequest>(req))
                .Bind(body => UnitsRepository.UpdateUnit(req, body, unitId));

        [Function(nameof(Units.DeleteUnit))]
        [OpenApiOperation(nameof(Units.DeleteUnit), nameof(Units), Summary = "Delete a unit", Description = "_Authorization_: Unit deletion is restricted to service administrators.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to modify this unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        [OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "Unit `unitId` has child units, with ids: `list of child unitIds`. These must be reassigned prior to deletion.")]
        public static Task<IActionResult> DeleteUnit(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "units/{unitId}")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
                .Bind(id => DeleteUnitInternal(req, id))
                .Finally(result => Response.NoContent(req, result));

        private static Task<Result<bool, Error>> DeleteUnitInternal(HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineServiceAdminPermissions(req, requestor))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
                .Bind(_ => UnitsRepository.DeleteUnit(req, unitId));

        [Function(nameof(Units.ArchiveUnit))]
        [OpenApiOperation(nameof(Units.ArchiveUnit), nameof(Units), Summary = "Archive a unit", Description = "_Authorization_: Unit archival is restricted to service administrators.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitResponse))]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to modify this unit.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        [OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "Unit `unitId` has child units, with ids: `list of child unitIds`. These must be reassigned, deleted, or archived before this request can be completed.\\\n **or**\\\n Unit `unitId` has a parent unit `parentUnitId` that is archived. This parent unit must be unarchived before this request can be completed.")]
        public static Task<IActionResult> ArchiveUnit([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "units/{unitId}/archive")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
                .Bind(id => ArchiveUnitInternal(req, id))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<Unit, Error>> ArchiveUnitInternal(HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineServiceAdminPermissions(req, requestor))
                .Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
                .Bind(_ => UnitsRepository.ChangeActive(req, unitId));

        [Function(nameof(Units.GetUnitChildren))]
        [OpenApiOperation(nameof(Units.GetUnitChildren), nameof(Units), Summary = "List all unit children ", Description = "List all units that fall below this unit in an organizational hierarchy.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> GetUnitChildren(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}/children")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
                .Bind(id => GetUnitChildrenInternal(req, id))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<List<Unit>, Error>> GetUnitChildrenInternal(HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineServiceAdminPermissions(req, requestor))
                .Bind(_ => UnitsRepository.GetChildren(req, unitId));

        [Function(nameof(Units.GetUnitMembers))]
        [OpenApiOperation(nameof(Units.GetUnitMembers), nameof(Units), Summary = "List all unit members", Description = "List all people who do IT work for this unit along with any vacant positions.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitMemberResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> GetUnitMembers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}/members")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
                .Bind(id => GetUnitMembersInternal(req, id))
                .Bind(ms => Pipeline.Success(ms.Select(e => e.ToUnitMemberResponse(req.GetEntityPermissions()))))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<List<UnitMember>, Error>> GetUnitMembersInternal(HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestor, unitId))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetMembers(req, unitId));

        [Function(nameof(Units.GetUnitSupportedBuildings))]
        [OpenApiOperation(nameof(Units.GetUnitSupportedBuildings), nameof(Units), Summary = "List all supported buildings", Description = "List all buildings that receive IT support from this unit.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<BuildingRelationshipResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> GetUnitSupportedBuildings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}/supportedBuildings")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
                .Bind(id => GetUnitSupportedBuildingsInternal(req, id))
                .Bind(sr => Pipeline.Success(sr.Select(srx => new BuildingRelationshipResponse(srx)).ToList()))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<List<BuildingRelationship>, Error>> GetUnitSupportedBuildingsInternal(HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestor, unitId, UnitPermissions.Owner))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetSupportedBuildings(req, unitId));

        [Function(nameof(Units.GetUnitSupportedDepartments))]
        [OpenApiOperation(nameof(Units.GetUnitSupportedDepartments), nameof(Units), Summary = "List all supported departments", Description = "List all departments that receive IT support from this unit.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<SupportRelationshipResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> GetUnitSupportedDepartments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}/supportedDepartments")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
                .Bind(id => GetUnitSupportedDepartmentsInternal(req, id))
                .Bind(sr => Pipeline.Success(sr.Select(srx => new SupportRelationshipResponse(srx)).ToList()))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<List<SupportRelationship>, Error>> GetUnitSupportedDepartmentsInternal(HttpRequest req, int unitId)
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestor, unitId, UnitPermissions.Owner))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetSupportedDepartments(req, unitId));

        [Function(nameof(Units.GetUnitTools))]
        [OpenApiOperation(nameof(Units.GetUnitTools), nameof(Units), Summary = "List all unit tools", Description = "List all tools that are available to this unit.")]
        [OpenApiParameter("unitId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<ToolResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit was found with the provided ID.")]
        public static Task<IActionResult> GetUnitTools(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "units/{unitId}/tools")] HttpRequest req, string unitId)
            => Utils.ConvertParam(unitId, nameof(unitId))
            .Bind(id => GetUnitToolsInternal(req, id))
                .Bind(t => Pipeline.Success(ToolResponse.ConvertList(t)))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<List<Tool>, Error>> GetUnitToolsInternal(HttpRequest req, int unitId)
        {
            string rni = null;
            return Security.Authenticate(req)
                .Tap(requestor => rni = requestor)
                .Bind(requestor => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestor, unitId, new List<UnitPermissions> { UnitPermissions.ManageMembers, UnitPermissions.ManageTools }))// Set headers saying what the requestor can do to this unit
                .Bind(_ => UnitsRepository.GetTools(req, unitId));
        }

    }
}
