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
using System.Net.Mime;

namespace API.Functions
{
	public static class Departments
	{
		[FunctionName(nameof(Departments.DepartmentsGetAll))]
		[OpenApiOperation(nameof(Departments.DepartmentsGetAll), nameof(Departments), Summary = "List all Departments", Description = @"Get a list of university Departments.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<DepartmentResponse>), Description = "A collection of department records")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description = "The search query was malformed or incorrect. See response content for additional information.")]
		[OpenApiParameter("q", In = ParameterLocation.Query, Description = "filter by department name/description, ex: 'Parks' or 'PA-PARK'")]
		[OpenApiParameter("_limit", In = ParameterLocation.Query, Description = "Restrict the number of responses to no more than this integer. ex: `15` or `20`.  `0` or less will return all records.")]
		public static Task<IActionResult> DepartmentsGetAll(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "departments")] HttpRequest req)
			=> Security.Authenticate(req)
				.Bind(_ => DepartmentSearchParameters.Parse(req))
				.Bind(query => DepartmentsRepository.GetAll(query))
				.Finally(Departments => Response.Ok(req, Departments));

		[FunctionName(nameof(Departments.DepartmentsGetOne))]
		[OpenApiOperation(nameof(Departments.DepartmentsGetOne), nameof(Departments), Summary = "Find a department by ID")]
		[OpenApiParameter("departmentId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the department record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(DepartmentResponse), Description = "A department record")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No department was found with the ID provided.")]
		public static Task<IActionResult> DepartmentsGetOne(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "departments/{departmentId}")] HttpRequest req, int departmentId)
			=> Security.Authenticate(req)
				.Bind(_ => DepartmentsRepository.GetOne(departmentId))
				.Finally(result => Response.Ok(req, result));
		
        [FunctionName(nameof(Departments.DepartmentsGetMemberUnits))]
		[OpenApiOperation(nameof(Departments.DepartmentsGetMemberUnits), nameof(Departments), Summary = "List a department's member units", Description="A member unit contains people that have an HR relationship with the department.")]
		[OpenApiParameter("departmentId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the department record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<UnitResponse>), Description = "A collection of unit records")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No department was found with the ID provided.")]
		public static Task<IActionResult> DepartmentsGetMemberUnits(
					[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "departments/{departmentId}/memberUnits")] HttpRequest req, int departmentId)
					=> Security.Authenticate(req)
						.Bind(_ => DepartmentsRepository.GetOne(departmentId))
						.Bind(_ => DepartmentsRepository.GetMemberUnits(departmentId))
						.Finally(result => Response.Ok(req, result));

		[FunctionName(nameof(Departments.DepartmentsGetSupportingUnits))]
		[OpenApiOperation(nameof(Departments.DepartmentsGetSupportingUnits), nameof(Departments), Summary = "List a department's supporting units", Description="A supporting unit provides IT services for the department.")]
		[OpenApiParameter("departmentId", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the department record.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<SupportRelationshipResponse>), Description = "A collection of support relationship records")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No department was found with the ID provided.")]
		public static Task<IActionResult> DepartmentsGetSupportingUnits(
					[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "departments/{departmentId}/supportingUnits")] HttpRequest req, int departmentId)
					=> Security.Authenticate(req)
						.Bind(_ => DepartmentsRepository.GetOne(departmentId))
						.Bind(_ => DepartmentsRepository.GetSupportingUnits(departmentId))
						.Bind(sr => Pipeline.Success(SupportRelationshipResponse.ConvertList(sr)))
						.Finally(result => Response.Ok(req, result));
		
		[FunctionName(nameof(Departments.SetDepartmentReportSupportingUnit))]
		[OpenApiOperation(nameof(Departments.SetDepartmentReportSupportingUnit), nameof(Departments), Summary = "Set a department's Report Supporting Unit", Description="Placeholder")]
		[OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(DepartmentResponse))]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(DepartmentResponse), Description = "The updated department.")]
		[OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The requestor is not an admin.")]
		[OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No matching department or unit were found.")]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The provided unit is not a valid Report Supporting Unit for the department based on its existing support relationships.")]
		public static Task<IActionResult> SetDepartmentReportSupportingUnit([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "SetDepartmentReportSupportingUnit")] HttpRequest req)
		{
			string requestor = null;
			return Security.Authenticate(req)
				.Tap(netId => requestor = netId)
				.Bind(netId => AuthorizationRepository.DetermineServiceAdminPermissions(req, netId))
				.Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
				.Bind(_ => Request.DeserializeBody<DepartmentResponse>(req))
				.Bind(body => DepartmentsRepository.SetDepartmentReportSupportingUnit(req, body, requestor))
				.Finally(result => Response.Ok(req, result));
		}
	}
}