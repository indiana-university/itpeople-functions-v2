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
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace API.Functions
{
	public static class UnitMembers
	{
		public const string UnitMembersTitle = "Unit Memberships";
		public const string PostPutBadResponseDescription = "The request body was malformed or the unitId field was missing.\\\n**or**\\\nThe field Percentage must be between 0 and 100.\\\n**or**\\\nThe provided unit has been archived and is not available for new Unit Members.";


		[Function(nameof(UnitMembers.UnitMembersGetAll))]
		[OpenApiOperation(nameof(UnitMembers.UnitMembersGetAll), UnitMembersTitle, Summary = "List all unit memberships")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitMemberResponse>), Description = "A collection of unit membership record")]
		public static Task<HttpResponseData> UnitMembersGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "memberships")] HttpRequestData req)
			=> Security.Authenticate(req)
				.Bind(_ => UnitMembersRepository.GetAll())
				.Bind(res => Pipeline.Success(res.Where(e => e.Unit.Active).Select(e => e.ToUnitMemberResponse(EntityPermissions.Get))))
				.Finally(dtos => Response.Ok(req, dtos));

		[Function(nameof(UnitMembers.UnitMembersGetOne))]
		[OpenApiOperation(nameof(UnitMembers.UnitMembersGetOne), UnitMembersTitle, Summary = "Find a unit membership by ID")]
		[OpenApiParameter("membershipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit membership record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UnitMemberResponse), Description = "A unit membership record")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit membership was found with the ID provided.")]
		public static Task<HttpResponseData> UnitMembersGetOne(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "memberships/{membershipId}")] HttpRequestData req, int membershipId)
		{
			string requestorNetId = null;
			UnitMember unitMember = null;
			return Security.Authenticate(req)
			.Tap(requestor => requestorNetId = requestor)
			.Bind(_ => UnitMembersRepository.GetOne(membershipId))
			.Tap(um => unitMember = um)
			.Bind(um => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, um.UnitId))
			.Bind(perms => Pipeline.Success(unitMember.ToUnitMemberResponse(perms)))
			.Finally(result => Response.Ok(req, result));
		}

		[Function(nameof(UnitMembers.CreateUnitMembers))]
		[OpenApiOperation(nameof(UnitMembers.CreateUnitMembers), UnitMembersTitle, Summary = "Create a unit membership", Description = "Authorization: Unit memberships can be created by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UnitMemberRequest), Required = true)]
		[OpenApiResponseWithBody(HttpStatusCode.Created, MediaTypeNames.Application.Json, typeof(UnitMemberResponse), Description = "The newly created unit membership record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = PostPutBadResponseDescription)]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to modify this unit.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The specified unit and/or person does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided person is already a member of the provided unit.")]
		public static Task<HttpResponseData> CreateUnitMembers(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "memberships")] HttpRequestData req)
		{
			string requestorNetId = null;
			UnitMemberRequest unitMemberRequest = null;
			return Security.Authenticate(req)
			.Tap(requestor => requestorNetId = requestor)
			.Bind(requestor => Request.DeserializeBody<UnitMemberRequest>(req))
			.Tap(umr => unitMemberRequest = umr)
			.Bind(umr => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, umr.UnitId))// Set headers saying what the requestor can do to this unit
			.Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
			.Bind(authorized => UnitMembersRepository.CreateMembership(unitMemberRequest))
			.Bind(res => Pipeline.Success(res.ToUnitMemberResponse(EntityPermissions.Post)))
			.Finally(result => Response.Created(req, result));
		}

		[Function(nameof(UnitMembers.UpdateUnitMember))]
		[OpenApiOperation(nameof(UnitMembers.UpdateUnitMember), UnitMembersTitle, Summary = "Update a unit membership.", Description = "Authorization: Unit memberships can be modified by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UnitMemberRequest), Required = true)]
		[OpenApiParameter("membershipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit membership record")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UnitMemberResponse), Description = "The updated unit membership record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = PostPutBadResponseDescription)]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No membership was found with the ID provided or the specified unit and/or person does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided person is already a member of the provided unit.")]
		public static Task<HttpResponseData> UpdateUnitMember(
			[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "memberships/{membershipId}")] HttpRequestData req, int membershipId)
		{
			string requestorNetId = null;
			UnitMemberRequest unitMemberRequest = null;
			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => Request.DeserializeBody<UnitMemberRequest>(req))
				.Tap(umr => unitMemberRequest = umr)
				.Bind(umr => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, umr.UnitId))// Set headers saying what the requestor can do to this unit
				.Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
				.Bind(authorized => UnitMembersRepository.UpdateMembership(req, unitMemberRequest, membershipId))
				.Bind(um => Pipeline.Success(um.ToUnitMemberResponse(EntityPermissions.Put)))
				.Finally(result => Response.Ok(req, result));
		}

		[Function(nameof(UnitMembers.DeleteUnitMembership))]
		[OpenApiOperation(nameof(UnitMembers.DeleteUnitMembership), UnitMembersTitle, Summary = "Delete a unit membership", Description = "Authorization: Unit memberships can be deleted by any unit member that has either the `Owner` or `ManageMembers` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitsGetAll).")]
		[OpenApiParameter("membershipId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit membership record.")]
		[OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No membership was found with the ID provided.")]
		public static Task<HttpResponseData> DeleteUnitMembership(
			[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "memberships/{membershipId}")] HttpRequestData req, int membershipId)
		{
			string requestorNetId = null;
			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => UnitMembersRepository.GetOne(membershipId))
				.Bind(um => AuthorizationRepository.DetermineUnitManagementPermissions(req, requestorNetId, um.UnitId))// Set headers saying what the requestor can do to this unit
				.Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
				.Bind(_ => UnitMembersRepository.DeleteMembership(req, membershipId))
				.Finally(result => Response.NoContent(req, result));
		}
	}
}