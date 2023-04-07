using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using API.Data;
using API.Middleware;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Models;

namespace API.Functions
{
	public static class UnitMemberTools
	{
		public const string Title = "Unit Member Tools";
		public const string CombinedError = UnitMemberToolsRepository.MalformedBody + "\\\n**or**\\\n" + UnitMemberToolsRepository.ArchivedUnit;

		[Function(nameof(UnitMemberTools.UnitMemberToolsGetAll))]
		[OpenApiOperation(nameof(UnitMemberTools.UnitMemberToolsGetAll), Title, Summary = "List all unit member tools")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<MemberToolResponse>), Description = "A collection of unit member tool records")]
		public static Task<HttpResponseData> UnitMemberToolsGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "membertools")] HttpRequestData req)
			=> Security.Authenticate(req)
				.Bind(query => UnitMemberToolsRepository.GetAll())
				.Bind(mt => Pipeline.Success(MemberToolResponse.ConvertList(mt)))
				.Finally(results => Response.Ok(req, results));

		[Function(nameof(UnitMemberTools.UnitMemberToolsGetOne))]
		[OpenApiOperation(nameof(UnitMemberTools.UnitMemberToolsGetOne), Title, Summary = "Find a unit member tool by ID")]
		[OpenApiParameter("memberToolId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit member tool record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(MemberToolResponse), Description = "A unit member tool record")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No member tool record was found with the ID provided.")]
		public static Task<HttpResponseData> UnitMemberToolsGetOne(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "membertools/{memberToolId}")] HttpRequestData req, int memberToolId)
		{
			string requestorNetId = null;
			MemberTool memberTool = null;
			return Security.Authenticate(req) 
				.Tap(requestor => requestorNetId = requestor)
				.Bind(_ => UnitMemberToolsRepository.GetOne(memberToolId))
				.Tap(mt => memberTool = mt)			
				.Bind(mt => AuthorizationRepository.DetermineUnitMemberToolPermissions(req, requestorNetId, mt.MembershipId))
				.Bind(_ => Pipeline.Success(memberTool.ToMemberToolResponse()))
				.Finally(result => Response.Ok(req, result));
		}

		[Function(nameof(UnitMemberTools.UnitMemberToolsCreate))]
		[OpenApiOperation(nameof(UnitMemberTools.UnitMemberToolsCreate), Title, Summary = "Create a unit member tool.", Description = "*Authorization*: Unit tool permissions can be created by any unit member that has either the `Owner` or `ManageTools` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitMembersGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(MemberToolRequest), Required = true)]
		[OpenApiResponseWithBody(HttpStatusCode.Created, MediaTypeNames.Application.Json, typeof(MemberToolResponse), Description = "The newly created unit member tool record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = CombinedError)]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The specified tool does not exist. **or** The specified member does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = UnitMemberToolsRepository.MemberToolConflict)]
		public static Task<HttpResponseData> UnitMemberToolsCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "membertools")] HttpRequestData req)
		{
			string requestorNetId = null;
			MemberToolRequest memberToolRequest = null;
			return Security.Authenticate(req)
			.Tap(requestor => requestorNetId = requestor)
			.Bind(requestor => Request.DeserializeBody<MemberToolRequest>(req))
			.Tap(mtr => memberToolRequest = mtr)
			.Bind(mtr => AuthorizationRepository.DetermineUnitMemberToolPermissions(req, requestorNetId, mtr.MembershipId))// Set headers saying what the requestor can do based on the MemberTool's UnitMember.Unit
			.Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
			.Bind(authorized => UnitMemberToolsRepository.CreateUnitMemberTool(memberToolRequest))
			.Bind(mt => Pipeline.Success(mt.ToMemberToolResponse()))
			.Finally(result => Response.Created(req, result));
		}

		[Function(nameof(UnitMemberTools.UnitMemberToolsUpdate))]
		[OpenApiOperation(nameof(UnitMemberTools.UnitMemberToolsUpdate), Title, Summary = "Update a unit member tool", Description = "*Authorization*: Unit tool permissions can be updated by any unit member that has either the `Owner` or `ManageTools` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitMembersGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(MemberToolRequest), Required = true)]
		[OpenApiParameter("memberToolId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit member tool record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(MemberToolResponse), Description = "The updated unt member tool record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = CombinedError)]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The specified tool does not exist. **or** The specified member does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = UnitMemberToolsRepository.MemberToolConflict)]
		public static Task<HttpResponseData> UnitMemberToolsUpdate(
			[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "membertools/{memberToolId}")] HttpRequestData req, int memberToolId)
		{
			string requestorNetId = null;
			MemberToolRequest memberToolRequest = null;
			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => Request.DeserializeBody<MemberToolRequest>(req))
				.Tap(mtr => memberToolRequest = mtr)
				.Bind(mtr => AuthorizationRepository.DetermineUnitMemberToolPermissions(req, requestorNetId, mtr.MembershipId))// Set headers saying what the requestor can do based on the MemberTool's UnitMember.Unit
				.Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
				.Bind(authorized => UnitMemberToolsRepository.UpdateUnitMemberTool(req, memberToolRequest, memberToolId))
				.Bind(mr => Pipeline.Success(mr.ToMemberToolResponse()))
				.Finally(result => Response.Ok(req, result));
		}

		[Function(nameof(UnitMemberTools.UnitMemberToolDelete))]
		[OpenApiOperation(nameof(UnitMemberTools.UnitMemberToolDelete), Title, Summary = "Delete a unit member tool", Description = "*Authorization*: Unit tool permissions can be deleted by any unit member that has either the `Owner` or `ManageTools` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitMembersGetAll).")]
		[OpenApiParameter("memberToolId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the unit member tool record.")]
		[OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Success.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No unit member tool was found with the ID provided.")]
		public static Task<HttpResponseData> UnitMemberToolDelete(
			[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "membertools/{memberToolId}")] HttpRequestData req, int memberToolId)
		{
			string requestorNetId = null;
			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => UnitMemberToolsRepository.GetOne(memberToolId))
				.Bind(mt => AuthorizationRepository.DetermineUnitMemberToolPermissions(req, requestorNetId, mt.MembershipId))// Set headers saying what the requestor can do based on the MemberTool's UnitMember.Unit
				.Bind(perms => AuthorizationRepository.AuthorizeDeletion(perms))
				.Bind(_ => UnitMemberToolsRepository.DeleteUnitMemberTool(req, memberToolId))
				.Finally(result => Response.NoContent(req, result));
		}
	}
}