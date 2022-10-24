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

		[FunctionName(nameof(Notifications.NotificationsGetOne))]
		[OpenApiOperation(nameof(Notifications.NotificationsGetOne), nameof(Notifications), Summary = "Find a Notification by ID")]
		[OpenApiParameter("notificationId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the Notification record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(Notification))]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No notification was found with the provided ID.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to get Notifications.")]
		public static Task<IActionResult> NotificationsGetOne(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notifications/{notificationId}")] HttpRequest req, int notificationId)
			=> Security.Authenticate(req)
				.Bind(requestor => AuthorizationRepository.ResolveNotificationPermissions(req, requestor))
				.Bind(perms => AuthorizationRepository.AuthorizeModification(perms))// I know this is out of place, but permissions are weird, you have to have the PUT permissions to view/review Notifications
				.Bind(_ => NotificationsRepository.GetOne(notificationId))
				.Finally(result => Response.Ok(req, result));

		[FunctionName(nameof(Notifications.ReviewNotification))]
		[OpenApiOperation(nameof(Notifications.ReviewNotification), nameof(Notifications), Summary = "Review a Notification", Description = "Mark when a Notification was reviewed and who reviewed it.")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(Notification), Required=true)]
		[OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(Notification))]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided Notification could not be deserialized.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to modify this unit.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError))]
		public static Task<IActionResult> ReviewNotification(
			[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "notifications/{notificationId}")] HttpRequest req, int notificationId)
		{
			string requestorNetId = null;

			return Security.Authenticate(req)
				.Tap(requestor => requestorNetId = requestor)
				.Bind(requestor => AuthorizationRepository.ResolveNotificationPermissions(req, requestor))
				.Bind(perms => AuthorizationRepository.AuthorizeModification(perms))// I know this is out of place, but permissions are weird, you have to have the PUT permissions to view/review Notifications
				.Bind(_ => Request.DeserializeBody<Notification>(req))
				.Bind(body => NotificationsRepository.ReviewNotification(req, requestorNetId, notificationId))
				.Finally(result => Response.Ok(req, result));
		}
	}
}