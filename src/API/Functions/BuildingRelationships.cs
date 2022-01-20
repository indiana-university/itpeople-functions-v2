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

namespace API.Functions
{
	public static class BuildingRelationships
	{
		public const string BuildingRelationshipsTitle = "Building Relationships";

		[FunctionName(nameof(BuildingRelationships.BuildingRelationshipsGetAll))]
		[OpenApiOperation(nameof(BuildingRelationships.BuildingRelationshipsGetAll), BuildingRelationshipsTitle, Summary = "List all unit-building support relationships")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<BuildingRelationshipResponse>), Description = "A collection of building support relationship records")]
		public static Task<IActionResult> BuildingRelationshipsGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildingRelationships")] HttpRequest req)
			=> Security.Authenticate(req)
				.Bind(_ => BuildingRelationshipsRepository.GetAll())
				.Bind(br => Pipeline.Success(br.Select(brx => new BuildingRelationshipResponse(brx)).ToList()))
				.Finally(r => Response.Ok(req, r));

		[FunctionName(nameof(BuildingRelationships.BuildingRelationshipsGetOne))]
		[OpenApiOperation(nameof(BuildingRelationships.BuildingRelationshipsGetOne), BuildingRelationshipsTitle, Summary = "Find a unit-building support relationships by ID")]
		[OpenApiParameter("relationshipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the building support relationship record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(BuildingRelationshipResponse), Description = "A building support relationship record")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No support relationship was found with the ID provided.")]
		public static Task<IActionResult> BuildingRelationshipsGetOne(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildingRelationships/{relationshipId}")] HttpRequest req, int relationshipId)
			=> Security.Authenticate(req)
				.Bind(_ => BuildingRelationshipsRepository.GetOne(relationshipId))
				.Bind(br => Pipeline.Success(new BuildingRelationshipResponse(br)))
				.Finally(result => Response.Ok(req, result));

		[FunctionName(nameof(BuildingRelationships.CreateBuildingRelationship))]
		[OpenApiOperation(nameof(BuildingRelationships.CreateBuildingRelationship), BuildingRelationshipsTitle, Summary = "Create a unit-building support relationship", Description = "Authorization: Support relationships can be created by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(BuildingRelationshipRequest), Required = true)]
		[OpenApiResponseWithBody(HttpStatusCode.Created, MediaTypeNames.Application.Json, typeof(BuildingRelationshipResponse), Description = "The newly created building support relationship record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The request body was malformed, the unitId and/or buildingId field was missing.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The specified unit and/or building does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided unit already has a support relationship with the provided building.")]

		public static Task<IActionResult> CreateBuildingRelationship(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "buildingRelationships")] HttpRequest req)
		{
			string requestorNetId = null;
			BuildingRelationshipRequest buildingRelationshipRequest = null;
			return Security.Authenticate(req)
			.Tap(requestor => requestorNetId = requestor)
			.Bind(requestor => Request.DeserializeBody<BuildingRelationshipRequest>(req))
			.Tap(brr => buildingRelationshipRequest = brr)
			.Bind(brr => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, brr.UnitId, UnitPermissions.Owner))// Set headers saying what the requestor can do to this unit
			.Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
			.Bind(authorized => BuildingRelationshipsRepository.CreateBuildingRelationship(buildingRelationshipRequest))
			.Bind(br => Pipeline.Success(new BuildingRelationshipResponse(br)))
			.Finally(result => Response.Created(req, result));
		}

		[FunctionName(nameof(BuildingRelationships.DeleteBuildingRelationship))]
		[OpenApiOperation(nameof(BuildingRelationships.DeleteBuildingRelationship), BuildingRelationshipsTitle, Summary = "Delete a unit-building support relationship", Description = "Authorization: Support relationships can be deleted by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiParameter("relationshipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the building support relationship record.")]
		[OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No building support relationship was found with the ID provided.")]
		public static Task<IActionResult> DeleteBuildingRelationship(
			[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "buildingRelationships/{relationshipId}")] HttpRequest req, int relationshipId)
		{
				string requestorNetId = null;
				return Security.Authenticate(req)
					.Tap(requestor => requestorNetId = requestor)
					.Bind(requestor => BuildingRelationshipsRepository.GetOne(relationshipId))
					.Bind(br => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, br.UnitId, UnitPermissions.Owner))// Set headers saying what the requestor can do to this unit
					.Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
					.Bind(_ => BuildingRelationshipsRepository.DeleteBuildingRelationship(req, relationshipId))
					.Finally(result => Response.NoContent(req, result));
		}
	}
}