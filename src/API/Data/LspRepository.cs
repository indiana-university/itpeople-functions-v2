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

        internal static Task<Result<LspDepartmentArray,Error>> GetLspDepartments(string netid)
            => ExecuteDbPipeline("Get legacy LSP departments", db => MapLspDepartments(netid, db));
        internal static Task<Result<LspContactArray,Error>> GetDepartmentLsps(string department)
            => ExecuteDbPipeline("Get department LSPs", db => MapDepartmentLsps(department, db));

        private static async Task<Result<LspInfoArray, Error>> MapLegacyLsps(PeopleContext db)
        {
            var lspList = await db.People
                .FromSqlRaw(GetLspListSql)
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

        private static async Task<Result<LspDepartmentArray, Error>> MapLspDepartments(string netid, PeopleContext db)
        {
            var netidClean = netid.Trim().ToLowerInvariant();
            var deptCodeList = await db.Departments
                .FromSqlRaw(GetLspDepartmentSql, netidClean)
                .Select(d => new DeptCodeList(new List<string>(){d.Name}))
                .AsNoTracking()
                .ToListAsync();
            var result = new LspDepartmentArray(netidClean, deptCodeList);
            return Pipeline.Success(result);
        }

        private const string GetLspDepartmentSql = @"
            SELECT DISTINCT d.name
            FROM departments d
            JOIN support_relationships sr ON sr.department_id = d.id
            JOIN unit_members um ON um.unit_id = sr.unit_id
            JOIN people p ON um.person_id = p.id
            WHERE p.netid ILIKE {0}
                AND um.role <> 1";

        private static async Task<Result<LspContactArray, Error>> MapDepartmentLsps(string department, PeopleContext db)
        {
            var deptClean = department.Trim().ToLowerInvariant();
            var contactList = await db.People
                .FromSqlRaw(GetDepartmentLspsSql, deptClean)
                .Select(p => new LspContact(p))
                .AsNoTracking()
                .ToListAsync();
            var result = new LspContactArray(contactList);
            return Pipeline.Success(result);
        }

        private const string GetDepartmentLspsSql = @"
            SELECT 
                p.id, p.name, p.netid, p.campus, p.position, p.location, p.campus_phone, p.campus_email, 
                p.department_id, p.expertise, p.responsibilities, p.photo_url, u.email as notes,
                p.name_first, p.name_last, MAX(um.role) in (3,4) as is_service_admin
            FROM people p
            JOIN unit_members um on um.person_id = p.id
            JOIN support_relationships sr on sr.unit_id = um.unit_id
            JOIN departments d on sr.department_id = d.id
            JOIN units u on um.unit_id = u.id
            WHERE d.name ILIKE {0} 
                AND um.role <> 1
            GROUP BY 
                p.id, p.name, p.netid, p.campus, p.position, p.location, p.campus_phone, p.campus_email, 
                p.department_id, p.expertise, p.responsibilities, p.photo_url, u.email, 
                p.name_first, p.name_last
            ";
    }
}