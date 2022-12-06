using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Models;
using NUnit.Framework;

namespace Integration
{
	public class NotificationsTests
	{
		private static Database.PeopleContext GetDb() => Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
		private static async Task<Notification> AddNotificationToDb(PeopleContext db, string message, DateTime? created = null, DateTime? reviewed = null, string reviewedBy = null)
		{
			var notification = new Notification
			{
				Created = created ?? DateTime.Now,
				Message = message,
				Reviewed = reviewed,
				Netid = reviewedBy
			};

			var startCnt = db.Notifications.Count();
			await db.Notifications.AddAsync(notification);
			await db.SaveChangesAsync();
			Assert.NotZero(notification.Id);
			Assert.AreEqual(startCnt + 1, db.Notifications.Count());

			return notification;
		}

		public class NotificationsListing : ApiTest
		{
			private async Task<(Notification NotReviewed, Notification Reviewed)> GenerateListOfNotifications(PeopleContext db)
			{
				var unreadNotification = await AddNotificationToDb(db, "This one has not been reviewed, yet.", DateTime.Now.AddHours(-1));
				var reviewedNotification = await AddNotificationToDb(db, "This one has been reviewed.", DateTime.Now.AddDays(-1), DateTime.Now.AddHours(-12), "johndoe");

				return (unreadNotification, reviewedNotification);
			}

			[Test]
			public async Task OnlyAdminsCanListNotifications()
			{
				var db = GetDb();
				await GenerateListOfNotifications(db);
				var resp = await GetAuthenticated("Notifications", ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[Test]
			public async Task ListNotificationsDefaultsToNotReviewed()
			{
				var db = GetDb();
				var notifications = await GenerateListOfNotifications(db);

				var resp = await GetAuthenticated("Notifications", ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Notification>>();

				Assert.AreEqual(1, actual.Count);
				Assert.AreEqual(notifications.NotReviewed.Id, actual.First().Id, "Did we get the expected Notification back?");
			}

			[Test]
			public async Task ListNotificationsAcceptsParameter()
			{
				var db = GetDb();
				var notifications = await GenerateListOfNotifications(db);
				// Add one more, much older, reviewed notification.
				var olderReviewedNotification = await AddNotificationToDb(db, "Old as dirt.", DateTime.Now.AddMonths(-5), DateTime.Now.AddMonths(-5).AddDays(1), "johndoe");

				var resp = await GetAuthenticated("Notifications?showReviewed=1", ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<Notification>>();

				Assert.AreEqual(3, actual.Count);
				Assert.AreEqual(notifications.NotReviewed.Id, actual.First().Id, "Notifications that have not been reviewed should be at the start of the list.");
				Assert.AreEqual(olderReviewedNotification.Id, actual.Last().Id, "Reviewed Notifications go below that, sorted by reviewe date descending.");
			}
		}

		public class NotificationsGetOne : ApiTest
		{
			[Test]
			public async Task OnlyAdminsCanGetNotifications()
			{
				// Add a notification
				var db = GetDb();
				var notification = await AddNotificationToDb(db, "Only Admins will be able to see this.");

				// A user gets 403 Forbidden.
				var userResp = await GetAuthenticated($"Notifications/{notification.Id}", ValidRswansonJwt);//Ron is a unit leader, but not an admin.
				AssertStatusCode(userResp, HttpStatusCode.Forbidden);

				// An admin get 200 OK, and the actual notification.
				var adminResp = await GetAuthenticated($"Notifications/{notification.Id}", ValidAdminJwt);
				AssertStatusCode(adminResp, HttpStatusCode.OK);
				
				var actual = await adminResp.Content.ReadAsAsync<Notification>();
				Assert.AreEqual(notification.Id, actual.Id);
			}

			[Test]
			public async Task NotificationNotFound()
			{
				var resp = await GetAuthenticated("Notifications/999999", ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.NotFound);
			}
		}

		public class NotificationsPut : ApiTest
		{
			[Test]
			public async Task OnlyAdminsCanPutNotifications()
			{
				// Add a notification
				var db = GetDb();
				var notification = await AddNotificationToDb(db, "Tough luck, pleb.");

				var resp = await PutAuthenticated($"Notifications/{notification.Id}", notification, ValidRswansonJwt);
				AssertStatusCode(resp, HttpStatusCode.Forbidden);
			}

			[Test]
			public async Task PutNotification()
			{
				// Add a notification
				var db = GetDb();
				var notification = await AddNotificationToDb(db, "rswanso requested to change the Primary Support Unit for the Parks Department to City of Pawnee.");
				Assert.IsNull(notification.Reviewed);
				Assert.IsNull(notification.Netid);

				var resp = await PutAuthenticated($"Notifications/{notification.Id}", notification, ValidAdminJwt);
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<Notification>();
				Assert.AreEqual(notification.Id, actual.Id);
				Assert.AreEqual(notification.Message, actual.Message);
				Assert.NotNull(actual.Reviewed);
				Assert.AreEqual("johndoe", actual.Netid);
			}

		}
	}
}