using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
    public class UnitMembersRepository : DataRepository
    {
        internal static Task<Result<List<UnitMember>, Error>> GetAll()
            => ExecuteDbPipeline("get all unit members", async db => {
                    var result = await db.UnitMembers
                        .Include(u => u.Unit)
                        .Include(u => u.Unit.Parent)
                        .Include(u => u.Person)
                        .Include(u => u.MemberTools)
                        .AsNoTracking()
                        .ToListAsync();
                    return Pipeline.Success(result);
                });
    }
}