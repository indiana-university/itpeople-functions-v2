using System.Threading.Tasks;
using API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using CSharpFunctionalExtensions;
using API.Data;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using System.Net.Mime;
using Models;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace API.Functions
{
	public static class Notifications
	{
		[FunctionName(nameof(Notifications.NotificationsGetAll))]
		[OpenApiOperation(nameof(Notifications.NotificationsGetAll), nameof(Notifications), Summary="List notifications", Description = @"List all Notifications for system admins." )]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<Notification>))]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to list Notifications.")]
		[OpenApiParameter("ShowReviewed=1", In=ParameterLocation.Query, Description="Include Notifications that have already been reviewed in the results.")]
		public static Task<IActionResult> NotificationsGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notifications")] HttpRequest req)
			=> Security.Authenticate(req)
				.Bind(requestor => AuthorizationRepository.ResolveNotificationPermissions(req, requestor))// Set headers saying what the requestor can do to these notifications
				.Bind(perms => AuthorizationRepository.AuthorizeModification(perms))// I know this is out of place, but permissions are weird, you have to have the PUT permissions to view/review Notifications
				.Bind(_ => NotificationsParameters.Parse(req))
				.Bind(httpQuery => NotificationsRepository.GetAll(httpQuery))
				.Finally(notifications => Response.Ok(req, notifications));
	}
}