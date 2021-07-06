using System.Collections.Generic;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
	public class SupportTypesRepository : DataRepository
	{
		internal static Task<Result<List<SupportType>, Error>> GetAll()
			=> ExecuteDbPipeline("list all support types", async db =>
			{
				var result = await db.SupportTypes
					.AsNoTracking()
					.ToListAsync();
				return Pipeline.Success(result);
			});

	}
}