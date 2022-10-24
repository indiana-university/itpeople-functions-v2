using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Functions;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
	public class NotificationsRepository : DataRepository
	{
		internal static Task<Result<List<Notification>, Error>> GetAll(NotificationsParameters httpQuery) =>
			ExecuteDbPipeline("Fetch all Notifications", async db => {
				var queryable = db.Notifications.AsQueryable();
				if(httpQuery.ShowReviewed == false)
				{
					queryable = queryable.Where(n => n.Reviewed == null);
				}

				var result = await queryable
					.AsNoTracking()
					.OrderByDescending(n => n.Reviewed)
					.ThenByDescending(n => n.Created)
					.ToListAsync();
				return Pipeline.Success(result);
			});
		
		public static Task<Result<Notification, Error>> GetOne(int id) 
			=> ExecuteDbPipeline("get a notification by ID", db => 
				TryFindNotification(db, id));
		

		internal static async Task<Result<Notification, Error>> ReviewNotification(HttpRequest req, string requestorNetId, int notificationId)
        {
            return await ExecuteDbPipeline($"review notification {notificationId}", db =>
                TryFindNotification(db, notificationId)
				.Tap(existing => LogPrevious(req, existing))
				.Bind(notification => TryReviewNotification(db, req, notification, requestorNetId))
				.Bind(_ => TryFindNotification(db, notificationId)));
        }

		private static async Task<Result<Notification, Error>> TryReviewNotification(PeopleContext db, HttpRequest req, Notification notification, string requestorNetId)
		{
			if(notification.Reviewed.HasValue == false)
			{
				LogPrevious(req, notification);

				notification.Reviewed = System.DateTime.Now;
				notification.Netid = requestorNetId;
				await db.SaveChangesAsync();
			}

			return Pipeline.Success(notification);
		}

		private static async Task<Result<Notification,Error>> TryFindNotification (PeopleContext db, int id)
		{
			var notification = await db.Notifications
				.SingleOrDefaultAsync(n => n.Id == id);
			return notification == null
				? Pipeline.NotFound($"No Notification found with ID ({id}).")
				: Pipeline.Success(notification);
		}
	}
}