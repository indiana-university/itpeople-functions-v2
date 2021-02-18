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
	public static class UnitMembers
	{
		public const string UnitMembersTitle = "Unit Memberships";


		[FunctionName(nameof(UnitMembers.UnitMembersGetAll))]
		[OpenApiOperation(nameof(UnitMembers.UnitMembersGetAll), UnitMembersTitle, Summary = "List all unit memberships")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitMemberResponse>), Description = "A collection of unit membership record")]
		public static Task<IActionResult> UnitMembersGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "memberships")] HttpRequest req)
			=> Security.Authenticate(req)
				.Bind(_ => UnitMembersRepository.GetAll())
				.Bind(res => Pipeline.Success(res.Select(e => e.ToUnitMemberResponse())))
				.Finally(dtos => Response.Ok(req, dtos));

		[FunctionName(nameof(UnitMembers.UnitMembersGetOne))]
		[OpenApiOperation(nameof(UnitMembers.UnitMembersGetOne), UnitMembersTitle, Summary = "Find a unit membership by ID")]
		[OpenApiParameter("membershipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit membership record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UnitMemberResponse), Description = "A unit membership record")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit membership was found with the ID provided.")]
		public static Task<IActionResult> UnitMembersGetOne(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "memberships/{membershipId}")] HttpRequest req, int membershipId)
			=> Security.Authenticate(req)
				.Bind(_ => UnitMembersRepository.GetOne(membershipId))
				.Bind(res => Pipeline.Success(res.ToUnitMemberResponse()))
				.Finally(result => Response.Ok(req, result));

		[FunctionName(nameof(UnitMembers.CreateUnitMembers))]
		[OpenApiOperation(nameof(UnitMembers.CreateUnitMembers), UnitMembersTitle, Summary = "Create a unit membership", Description = "Authorization: Unit memberships can be created by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UnitMemberRequest), Required = true)]
		[OpenApiResponseWithBody(HttpStatusCode.Created, MediaTypeNames.Application.Json, typeof(UnitMemberResponse), Description = "The newly created unit membership record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The request body was malformed or the unitId field was missing.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to modify this unit.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The specified unit and/or person does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided person is already a member of the provided unit.")]
		public static Task<IActionResult> CreateUnitMembers(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "memberships")] HttpRequest req)
		{
			string requestorNetId = null;
			UnitMemberRequest unitMemberRequest = null;
			return Security.Authenticate(req)
			.Tap(requestor => requestorNetId = requestor)
			.Bind(requestor => Request.DeserializeBody<UnitMemberRequest>(req))
			.Tap(umr => unitMemberRequest = umr)
			.Bind(umr => AuthorizationRepository.DetermineUnitPermissions(req, requestorNetId, umr.UnitId))// Set headers saying what the requestor can do to this unit
			.Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
			.Bind(authorized => UnitMembersRepository.CreateMembership(unitMemberRequest))
			.Bind(res => Pipeline.Success(res.ToUnitMemberResponse()))
			.Finally(result => Response.Created(req, result));
		}

		[FunctionName(nameof(UnitMembers.UpdateUnitMember))]
		[OpenApiOperation(nameof(UnitMembers.UpdateUnitMember), UnitMembersTitle, Summary = "Update a unit membership.", Description = "Authorization: Unit memberships can be modified by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UnitMemberRequest), Required = true)]
		[OpenApiParameter("membershipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit membership record")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitMemberResponse), Description = "The updated unit membership record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The request body was malformed or the unitId field was missing.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No membership was found with the ID provided or the specified unit and/or person does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided person is already a member of the provided unit.")]
		public static Task<IActionResult> UpdateUnitMember(
			[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "memberships/{membershipId}")] HttpRequest req, int membershipId)
		{
			string requestorNetId = null;
			UnitMemberRequest unitMemberRequest = null;
			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => Request.DeserializeBody<UnitMemberRequest>(req))
				.Tap(umr => unitMemberRequest = umr)
				.Bind(umr => AuthorizationRepository.DetermineUnitPermissions(req, requestorNetId, umr.UnitId))// Set headers saying what the requestor can do to this unit
				.Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
				.Bind(authorized => UnitMembersRepository.UpdateMembership(req, unitMemberRequest, membershipId))
				.Bind(um => Pipeline.Success(um.ToUnitMemberResponse()))
				.Finally(result => Response.Ok(req, result));
		}

		[FunctionName(nameof(UnitMembers.DeleteUnitMembership))]
		[OpenApiOperation(nameof(UnitMembers.DeleteUnitMembership), UnitMembersTitle, Summary = "Delete a unit membership", Description = "Authorization: Unit memberships can be deleted by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiParameter("membershipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit membership record.")]
		[OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No membership was found with the ID provided.")]
		public static Task<IActionResult> DeleteUnitMembership(
	[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "memberships/{membershipId}")] HttpRequest req, int membershipId)
		{
			string requestorNetId = null;
			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => UnitMembersRepository.GetOne(membershipId))
				.Bind(um => AuthorizationRepository.DetermineUnitPermissions(req, requestorNetId, um.UnitId))// Set headers saying what the requestor can do to this unit
				.Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
				.Bind(_ => UnitMembersRepository.DeleteMembership(req, membershipId))
				.Finally(result => Response.NoContent(req, result));
		}
	}
}