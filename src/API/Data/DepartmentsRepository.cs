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
    public class DepartmentsRepository : DataRepository
    {
		internal static Task<Result<List<Department>, Error>> GetAll(DepartmentSearchParameters query)
            => ExecuteDbPipeline("search all departments", async db => {
                    var result = await db.Departments.Where(d =>
                        string.IsNullOrWhiteSpace(query.Q) 
                        || EF.Functions.ILike(d.Name, $"%{query.Q}%")
                        || EF.Functions.ILike(d.Description, $"%{query.Q}%"))
                        .OrderBy(d => d.Name)
                        .Take(25)
                        .AsNoTracking()
                        .ToListAsync();
                    return Pipeline.Success(result);
                });

        public static Task<Result<Department, Error>> GetOne(int id) 
            => ExecuteDbPipeline("get a department by ID", db => 
                TryFindDepartment(db, id));

        
        private static async Task<Result<Department,Error>> TryFindDepartment (PeopleContext db, int id)
        {
            var result = await db.Departments
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
                .Include(r => r.Unit.Parent)
                .Where(r => r.DepartmentId == departmentId)
                .AsNoTracking().ToListAsync();
            return Pipeline.Success(result);
        }
	}
}