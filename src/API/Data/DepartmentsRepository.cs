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
                    var result = await db.Departments.Where(b =>
                        string.IsNullOrWhiteSpace(query.Q) 
                        || EF.Functions.ILike(b.Name, $"%{query.Q}%")
                        || EF.Functions.ILike(b.Description, $"%{query.Q}%"))
                        .OrderBy(b => b.Name)
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
                .SingleOrDefaultAsync(b => b.Id == id);
            return result == null
                ? Pipeline.NotFound("No department was found with the ID provided.")
                : Pipeline.Success(result);
        }

    }
}