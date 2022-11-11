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
using Microsoft.AspNetCore.Http;
using Models.Enums;

namespace API.Data
{
    public class DepartmentsRepository : DataRepository
    {
		internal static Task<Result<List<Department>, Error>> GetAll(DepartmentSearchParameters query)
            => ExecuteDbPipeline("search all departments", async db => {
                    IQueryable<Department> dbQuery = db.Departments
                        .Include(d => d.ReportSupportingUnit)
                        .Where(d =>
                            string.IsNullOrWhiteSpace(query.Q) 
                            || EF.Functions.ILike(d.Name, $"%{query.Q}%")
                            || EF.Functions.ILike(d.Description, $"%{query.Q}%"))
                        .OrderBy(d => d.Name);

                    if(query.Limit > 0)
                    {
                        dbQuery = dbQuery.Take(query.Limit);
                    }

                    var result = await dbQuery
                        .AsNoTracking()
                        .ToListAsync();
                    return Pipeline.Success(result);
                });

        public static Task<Result<Department, Error>> GetOne(int id) 
            => ExecuteDbPipeline("get a department by ID", db => 
                TryFindDepartment(db, id));

        public static Task<Result<Department, Error>> SetDepartmentReportSupportingUnit(HttpRequest req, DepartmentResponse input, string requestorNetId)
            => ExecuteDbPipeline("Set Department Report Supporting Unit", async db => {
                // I tried doing this as a pipeline, but it was a disaster.
                var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == input.Id);
                if(department == null)
                {
                    return Pipeline.NotFound($"No department with id {input.Id} was found.");
                }
                
                var unit = await db.Units.SingleOrDefaultAsync(u => u.Id == input.ReportSupportingUnit.Id);
                if(unit == null)
                {
                    return Pipeline.NotFound($"no unit with id {input.ReportSupportingUnit.Id} was found.");
                }

                var canChange = await SupportRelationshipsRepository.CanUpdateDepartmentReportSupportingUnit(db, PermsGroups.All, requestorNetId, 0, department.Id, unit.Id);
                if(canChange != SupportRelationshipsRepository.CanChangeReportSupportingUnit.Yes)
                {
                    return Pipeline.BadRequest($"The unit {unit.Id} is not a valid Report Supporting Unit based on the department's existing support relationships.");
                }

                department.ReportSupportingUnit = unit;
                await db.SaveChangesAsync();
                
                return Pipeline.Success(department);
            });

        private static async Task<Result<Department,Error>> TryFindDepartment (PeopleContext db, int id)
        {
            var result = await db.Departments
                .Include(d => d.ReportSupportingUnit)
                .SingleOrDefaultAsync(d => d.Id == id);
            return result == null
                ? Pipeline.NotFound("No department was found with the ID provided.")
                : Pipeline.Success(result);
        }

        public static Task<Result<List<Unit>, Error>> GetMemberUnits(int departmentId) 
            => ExecuteDbPipeline("fetch units", db =>
                TryFindMemberUnits(db, departmentId)
                .Bind(d => Pipeline.Success(d)));

        private static async Task<Result<List<Unit>,Error>> TryFindMemberUnits (PeopleContext db, int departmentId)
        {
            var result = await db.UnitMembers
                .Include(m => m.Person)
                .Include(m => m.Unit.Parent)
                .Where(m => m.Person.DepartmentId == departmentId)
                .Select(m => m.Unit)
                .Distinct()
                .AsNoTracking().ToListAsync();
            return Pipeline.Success(result);
        }

        public static Task<Result<List<SupportRelationship>, Error>> GetSupportingUnits(int departmentId) 
            => ExecuteDbPipeline("fetch supporting relationships", db =>
                TryFindSupportingUnits(db, departmentId)
                .Bind(d => Pipeline.Success(d)));
                
        private static async Task<Result<List<SupportRelationship>,Error>> TryFindSupportingUnits (PeopleContext db, int departmentId)
        {
            var result = await db.SupportRelationships
                .Include(r => r.Department)
                    .ThenInclude(r => r.ReportSupportingUnit)
                .Include(r => r.Unit.Parent)
                .Include(r => r.SupportType)
                .Where(r => r.DepartmentId == departmentId)
                .AsNoTracking().ToListAsync();
            return Pipeline.Success(result);
        }
	}
}