using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;
using API.Functions;
using System;

namespace API.Data
{
    public class BuildingRelationshipsRepository : DataRepository
    {
		internal static Task<Result<List<BuildingRelationship>, Error>> GetAll()
            => ExecuteDbPipeline("search all building relationships", async db => {
                    var result = await db.BuildingRelationships
                        .AsNoTracking()
                        .ToListAsync();
                    return Pipeline.Success(result);
                });

    }
}