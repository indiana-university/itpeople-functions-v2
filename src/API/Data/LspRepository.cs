using System;
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
    public class LspRepository : DataRepository
    {
        /// <Remarks> LSPs are any member of a unit that has a support relationship with
        /// one or more departmens. "LA" = "local administrator" = unit leader.
        /// Exclude "related" people from result. </Remarks>
        internal static Task<Result<LspInfoArray,Error>> GetLspList()
            => ExecuteDbPipeline("Get legacy LSP list", MapLegacyLsps);

        private static async Task<Result<LspInfoArray, Error>> MapLegacyLsps(PeopleContext db)
        {
            var lspList = await db.People.FromSqlRaw(GetLspListSql)
                .Select(p => new LspInfo(p.Netid, p.IsServiceAdmin))
                .AsNoTracking()
                .ToArrayAsync();
            var result = new LspInfoArray(lspList);
            return Pipeline.Success(result);
        }

        private const string GetLspListSql = @"
            SELECT p.netid, MAX(um.Role) in (3,4) as is_service_admin 
            FROM people p
            JOIN unit_members um ON um.person_id = p.id
            WHERE um.unit_id IN (SELECT sr.unit_id from support_relationships sr)
                AND um.role <> 1
            GROUP BY p.netid
            ORDER BY p.netid";
    }
}