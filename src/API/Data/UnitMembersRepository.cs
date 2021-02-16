using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
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
        
        public static Task<Result<UnitMember, Error>> GetOne(int id) 
            => ExecuteDbPipeline("get a membership by ID", db => 
                TryFindMembership(db, id));

        private static async Task<Result<UnitMember,Error>> TryFindMembership (PeopleContext db, int id)
        {
            var result = await db.UnitMembers
                .SingleOrDefaultAsync(d => d.Id == id);
            return result == null
                ? Pipeline.NotFound("No unit membership was found with the ID provided.")
                : Pipeline.Success(result);
        }
    }
}