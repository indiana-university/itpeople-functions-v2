using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

using API.Data;
using API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using System.Collections.Generic;
using Models;
using Microsoft.OpenApi.Models;

namespace API.Functions
{
    public static class Departments
    {
		[FunctionName(nameof(Departments.DepartmentsGetAll))]
        [OpenApiOperation(nameof(Departments.DepartmentsGetAll), nameof(Departments), Summary="List all Departments", Description = @"Get a list of university Departments." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Department>), Description="A collection of department records")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In=ParameterLocation.Query, Description="filter by department name/description, ex: 'Parks' or 'PA-PARK'")]
        public static Task<IActionResult> DepartmentsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "departments")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => DepartmentSearchParameters.Parse(req))
                .Bind(query => DepartmentsRepository.GetAll(query))
                .Finally(Departments => Response.Ok(req, Departments));

        [FunctionName(nameof(Departments.DepartmentsGetOne))]
        [OpenApiOperation(nameof(Departments.DepartmentsGetOne), nameof(Departments), Summary = "Find a department by ID")]
        [OpenApiParameter("departmentId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the department record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Department), Description="A department record")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No department was found with the ID provided.")]
        public static Task<IActionResult> DepartmentsGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "departments/{departmentId}")] HttpRequest req, int departmentId) 
            => Security.Authenticate(req)
                .Bind(_ => DepartmentsRepository.GetOne(departmentId))
                .Finally(result => Response.Ok(req, result));

    }
}