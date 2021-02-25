using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using API.Data;
using API.Middleware;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Models;

namespace API.Functions
{
	public static class UnitMemberTools
	{
		public const string Title = "Unit Member Tools";

		[FunctionName(nameof(UnitMemberTools.UnitMemberToolsGetAll))]
		[OpenApiOperation(nameof(SupportRelationships.SupportRelationshipsGetAll), Title, Summary = "List all unit member tools")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<MemberToolResponse>), Description = "A collection of unit member tool records")]
		public static Task<IActionResult> UnitMemberToolsGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "membertools")] HttpRequest req)
			=> Security.Authenticate(req)
				.Bind(query => UnitMemberToolsRepository.GetAll())
				.Bind(mt => Pipeline.Success(MemberToolResponse.ConvertList(mt)))
				.Finally(results => Response.Ok(req, results));

		[FunctionName(nameof(UnitMemberTools.UnitMemberToolsCreate))]
		[OpenApiOperation(nameof(UnitMemberTools.UnitMemberToolsCreate), Title, Summary = "Create a unit member tool.", Description = "*Authorization*: Unit tool permissions can be created by any unit member that has either the `Owner` or `ManageTools` permission on their unit membership. See also: [Units - List all unit members](#operation/UnitMembersGetAll).")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(MemberToolRequest), Required = true)]
		[OpenApiResponseWithBody(HttpStatusCode.Created, MediaTypeNames.Application.Json, typeof(MemberToolResponse), Description = "The newly created unit member tool record")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = UnitMemberToolsRepository.MalformedBody)]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You are not authorized to make this request.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The specified tool does not exist. **or** The specified member does not exist.")]
		[OpenApiResponseWithBody(HttpStatusCode.Conflict, MediaTypeNames.Application.Json, typeof(ApiError), Description = UnitMemberToolsRepository.MemberToolConflict)]
		public static Task<IActionResult> UnitMemberToolsCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "membertools")] HttpRequest req)
		{
			string requestorNetId = null;
			MemberToolRequest memberToolRequest = null;
			return Security.Authenticate(req)
			.Tap(requestor => requestorNetId = requestor)
			.Bind(requestor => Request.DeserializeBody<MemberToolRequest>(req))
			.Tap(mtr => memberToolRequest = mtr)
			.Bind(mtr => AuthorizationRepository.DetermineUnitMemberToolPermissions(req, requestorNetId, mtr.MembershipId))// Set headers saying what the requestor can do to the spec
			.Bind(perms => AuthorizationRepository.AuthorizeCreation(perms))
			.Bind(authorized => UnitMemberToolsRepository.CreateUnitMemberTool(memberToolRequest))
			.Bind(mt => Pipeline.Success(mt.ToMemberToolResponse()))
			.Finally(result => Response.Created(req, result));
		}
	}
}