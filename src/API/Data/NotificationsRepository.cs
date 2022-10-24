using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Functions;
using API.Middleware;
using CSharpFunctionalExtensions;
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
	}
}