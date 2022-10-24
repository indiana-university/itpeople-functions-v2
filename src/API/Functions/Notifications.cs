using System.Threading.Tasks;
using API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using CSharpFunctionalExtensions;
using API.Data;

namespace API.Functions
{
	public static class Notifications
	{
		[FunctionName(nameof(Notifications.NotificationsGetAll))]
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