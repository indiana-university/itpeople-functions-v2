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
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supportRelationships/{relationshipId}")] HttpRequest req, int relationshipId)
			=> Security.Authenticate(req)
				.Bind(_ => SupportRelationshipsRepository.GetOne(relationshipId))
				.Bind(sr => Pipeline.Success(new SupportRelationshipResponse(sr)))
				.Finally(result => Response.Ok(req, result));

		[FunctionName(nameof(SupportRelationships.CreateSupportRelationship))]
		[OpenApiOperation(nameof(SupportRelationships.CreateSupportRelationship), SupportRelationshipsTitle, Summary = "Create a unit-department support relationship", Description = "Authorization: Support relationships can be created by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(SupportRelationshipRequest), Required = true)]
		[OpenApiResponseWithBody(HttpStatusCode.Created, MediaTypeNames.Application.Json, typeof(SupportRelationshipResponse), Description = "The newly created department support relationship record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The request body was malformed, the unitId and/or departmentId field was missing.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The specified unit and/or department does not exist.")]
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
			.Bind(srr => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, srr.UnitId))// Set headers saying what the requestor can do to this unit
			.Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
			.Bind(authorized => SupportRelationshipsRepository.CreateSupportRelationship(supportRelationshipRequest))
			.Bind(sr => Pipeline.Success(new SupportRelationshipResponse(sr)))
			.Finally(result => Response.Created(req, result));
		}

		[FunctionName(nameof(SupportRelationships.UpdateSupportRelationship))]
		[OpenApiOperation(nameof(SupportRelationships.UpdateSupportRelationship), SupportRelationshipsTitle, Summary = "Update a unit-department support relationship", Description = "Authorization: Support relationships can be modified by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(SupportRelationshipRequest), Required = true)]
		[OpenApiParameter("relationshipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the department support relationship record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(SupportRelationshipResponse), Description = "The updated department support relationship record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The request body was malformed, the unitId and/or departmentId field was missing.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No department support relationship was found with the ID provided, or the specified unit and/or department does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided unit already has a support relationship with the provided department.")]
		public static Task<IActionResult> UpdateSupportRelationship(
			[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "supportRelationships/{relationshipId}")] HttpRequest req, int relationshipId)
		{
			string requestorNetId = null;
			SupportRelationshipRequest supportRelationshipRequest = null;
			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => Request.DeserializeBody<SupportRelationshipRequest>(req))
				.Tap(srr => supportRelationshipRequest = srr)
				.Bind(srr => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, srr.UnitId))// Set headers saying what the requestor can do to this unit
				.Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
				.Bind(authorized => SupportRelationshipsRepository.UpdateSupportRelationship(req, supportRelationshipRequest, relationshipId))
				.Bind(sr => Pipeline.Success(new SupportRelationshipResponse(sr)))
				.Finally(result => Response.Ok(req, result));
		}

		[FunctionName(nameof(SupportRelationships.DeleteSupportRelationship))]
		[OpenApiOperation(nameof(SupportRelationships.DeleteSupportRelationship), SupportRelationshipsTitle, Summary = "Delete a unit-department support relationship", Description = "Authorization: Support relationships can be deleted by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiParameter("relationshipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the department support relationship record.")]
		[OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No department support relationship was found with the ID provided.")]
		public static Task<IActionResult> DeleteSupportRelationship(
			[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "supportRelationships/{relationshipId}")] HttpRequest req, int relationshipId)
		{
			string requestorNetId = null;
			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => SupportRelationshipsRepository.GetOne(relationshipId))
				.Bind(sr => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, sr.UnitId))// Set headers saying what the requestor can do to this unit
				.Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
				.Bind(_ => SupportRelationshipsRepository.DeleteSupportRelationship(req, relationshipId))
				.Finally(result => Response.NoContent(req, result));
		}
	}
}