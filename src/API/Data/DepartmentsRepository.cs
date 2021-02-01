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

    }
}