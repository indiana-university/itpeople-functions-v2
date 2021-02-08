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
	public static class BuildingRelationships
	{
		public const string BuildingRelationshipsTitle = "Building Relationships";

		[FunctionName(nameof(BuildingRelationships.BuildingRelationshipsGetAll))]
		[OpenApiOperation(nameof(BuildingRelationships.BuildingRelationshipsGetAll), BuildingRelationshipsTitle, Summary = "List all unit-building support relationships")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<BuildingRelationship>), Description = "A collection of building support relationship records")]
		public static Task<IActionResult> BuildingRelationshipsGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildingRelationships")] HttpRequest req)
			=> Security.Authenticate(req)
				.Bind(_ => BuildingRelationshipsRepository.GetAll())
				.Finally(r => Response.Ok(req, r));

		[FunctionName(nameof(BuildingRelationships.BuildingRelationshipsGetOne))]
		[OpenApiOperation(nameof(BuildingRelationships.BuildingRelationshipsGetOne), BuildingRelationshipsTitle, Summary = "Find a unit-building support relationships by ID")]
		[OpenApiParameter("relationshipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the building support relationship record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(BuildingRelationship), Description = "A building support relationship record")]
		[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No support relationship was found with the ID provided.")]
		public static Task<IActionResult> BuildingRelationshipsGetOne(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildingRelationships/{relationshipId}")] HttpRequest req, int relationshipId)
			=> Security.Authenticate(req)
				.Bind(_ => BuildingRelationshipsRepository.GetOne(relationshipId))
				.Finally(result => Response.Ok(req, result));

		[FunctionName(nameof(BuildingRelationships.CreateBuildingRelationship))]
		[OpenApiOperation(nameof(BuildingRelationships.CreateBuildingRelationship), BuildingRelationshipsTitle, Summary = "Create a unit-building support relationship", Description = "Authorization: Support relationships can be created by any unit member that has either the Owner or ManageMembers permission on their unit membership.")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(BuildingRelationshipRequest), Required = true)]
		[OpenApiResponseWithBody(HttpStatusCode.Created, MediaTypeNames.Application.Json, typeof(BuildingRelationship), Description = "The newly created building support relationship record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The request body was malformed, the unitId and/or buildingId field was missing.")]
		[OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "You are not authorized to modify this unit.")]
		[OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "The specified unit and/or building does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided unit already has a support relationship with the provided building.")]

		public static Task<IActionResult> CreateBuildingRelationship(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "buildingRelationships")] HttpRequest req)
		{
			string requestorNetId = null;
			return Security.Authenticate(req)
			.Tap(requestor => requestorNetId = requestor)
			.Bind(requestor => Request.DeserializeBody<BuildingRelationshipRequest>(req))
			.Bind(brr => AuthorizationRepository.DetermineUnitPermissions(req, requestorNetId, brr.UnitId))// Set headers saying what the requestor can do to this unit
			.Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
			.Bind(_ => Request.DeserializeBody<BuildingRelationshipRequest>(req))
			.Bind(body => BuildingRelationshipsRepository.CreateBuildingRelationship(body))
			.Finally(result => Response.Created("buildingRelationships", result));


		}

	}
}