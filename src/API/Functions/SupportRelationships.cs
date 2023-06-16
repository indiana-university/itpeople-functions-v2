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

        public const string CombinedCreateUpdateError = "The request body was malformed, the unitId, departmentId, and/or supportTypeId field was missing or invalid.\\\n**or**\\\nThe request body was malformed, the provided unit has been archived and is not available for new Support Relationships.";

        [FunctionName(nameof(SupportRelationships.SupportRelationshipsGetAll))]
        [OpenApiOperation(nameof(SupportRelationships.SupportRelationshipsGetAll), SupportRelationshipsTitle, Summary = "List all unit-department support relationships")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<SupportRelationshipResponse>), Description = "A collection of department support relationship records")]
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
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No department support relationship was found with the ID provided.")]
        public static Task<IActionResult> SupportRelationshipsGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supportRelationships/{relationshipId}")] HttpRequest req, string relationshipId)
            => Utils.ConvertParam(relationshipId, nameof(relationshipId))
                .Bind(id => SupportRelationshipsGetOneInternal(req, id))
                .Bind(sr => Pipeline.Success(new SupportRelationshipResponse(sr)))
                .Finally(result => Response.Ok(req, result));

        private static Task<Result<SupportRelationship, Error>> SupportRelationshipsGetOneInternal(HttpRequest req, int relationshipId)
            => Security.Authenticate(req)
                .Bind(_ => SupportRelationshipsRepository.GetOne(relationshipId));

        [FunctionName(nameof(SupportRelationships.CreateSupportRelationship))]
        [OpenApiOperation(nameof(SupportRelationships.CreateSupportRelationship), SupportRelationshipsTitle, Summary = "Create a unit-department support relationship", Description = "Authorization: Support relationships can be created by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(SupportRelationshipRequest), Required = true)]
        [OpenApiResponseWithBody(HttpStatusCode.Created, MediaTypeNames.Application.Json, typeof(SupportRelationshipResponse), Description = "The newly created department support relationship record")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = CombinedCreateUpdateError)]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The specified unit, department, and/or support type does not exist.")]
        [OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided unit already has a support relationship with the provided department.")]

        public static Task<IActionResult> CreateSupportRelationship(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "supportRelationships")] HttpRequest req)
        {
            string requestorNetId = null;
            SupportRelationshipRequest supportRelationshipRequest = null;
            return Security.Authenticate(req)
            .Tap(requestor => requestorNetId = requestor)
            .Bind(requestor => Request.DeserializeBody<SupportRelationshipRequest>(req))
            .Tap(srr => supportRelationshipRequest = srr)
            .Bind(srr => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, srr.UnitId, UnitPermissions.Owner))// Set headers saying what the requestor can do to this unit
            .Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
            .Bind(authorized => SupportRelationshipsRepository.CreateSupportRelationship(supportRelationshipRequest))
            .Bind(sr => Pipeline.Success(new SupportRelationshipResponse(sr)))
            .Finally(result => Response.Created(req, result));
        }

        [FunctionName(nameof(SupportRelationships.DeleteSupportRelationship))]
        [OpenApiOperation(nameof(SupportRelationships.DeleteSupportRelationship), SupportRelationshipsTitle, Summary = "Delete a unit-department support relationship", Description = "Authorization: Support relationships can be deleted by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
        [OpenApiParameter("relationshipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the department support relationship record.")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No department support relationship was found with the ID provided.")]
        public static Task<IActionResult> DeleteSupportRelationship(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "supportRelationships/{relationshipId}")] HttpRequest req, string relationshipId)
            => Utils.ConvertParam(relationshipId, nameof(relationshipId))
                .Bind(id => DeleteSupportRelationshipInternal(req, id))
                .Finally(result => Response.NoContent(req, result));

        private static Task<Result<bool, Error>> DeleteSupportRelationshipInternal(HttpRequest req, int relationshipId)
        {
            string requestorNetId = null;
            return Security.Authenticate(req)
                .Tap(requestor => requestorNetId = requestor)
                .Bind(requestor => SupportRelationshipsRepository.GetOne(relationshipId))
                .Bind(sr => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, sr.UnitId, UnitPermissions.Owner))// Set headers saying what the requestor can do to this unit
                .Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
                .Bind(_ => SupportRelationshipsRepository.DeleteSupportRelationship(req, relationshipId));
        }

        [FunctionName(nameof(SupportRelationships.SsspSupportRelationships))]
        [OpenApiOperation(nameof(SupportRelationships.SsspSupportRelationships), SupportRelationshipsTitle, Summary = "Lists Support Relationships using a query specifically for SSSP's use.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<SsspSupportRelationshipResponse>), Description = "A collection of department support relationship records")]
        [OpenApiParameter("dept", In = ParameterLocation.Query, Description = "Filter results by the department's name. The provided value must exactly match the department's name.")]
        public static Task<IActionResult> SsspSupportRelationships(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "SsspSupportRelationships")] HttpRequest req)
            => Security.Authenticate(req)
                .Bind(_ => SsspSupportRelationshipParameters.Parse(req))
                .Bind(query => SupportRelationshipsRepository.GetSssp(query))
                .Finally(results => Response.Ok(req, results));
    }
}
