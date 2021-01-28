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
    public static class People
    {
        [FunctionName(nameof(People.GetAll))]
        [OpenApiOperation(nameof(People.GetAll), nameof(People), Summary="Search all people", Description = @"Search results are unioned within a filter and intersected across filters. For example, `interest=node, lambda` will return people with an interest in either `node` OR `lambda`, whereas `role=ItLeadership&interest=node` will only return people who are both in `ItLeadership AND have an interest in `node`." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Person>))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In=ParameterLocation.Query, Description="filter by name/netid, ex: `Ron` or `rswanso`")]
        [OpenApiParameter("class", In=ParameterLocation.Query, Type=typeof(ResponsibilitiesPropDoc), Description="filter by job classification/responsibility, ex: `UserExperience` or `UserExperience, WebAdminDevEng`")]
        [OpenApiParameter("interest", In=ParameterLocation.Query, Description="filter by one interests, ex: `serverless` or `node, lambda`")]
        [OpenApiParameter("campus", In=ParameterLocation.Query, Description="filter by primary campus: `Bloomington`, `Indianapolis`, `Columbus`, `East`, `Fort Wayne`, `Kokomo`, `Northwest`, `South Bend`, `Southeast`")]
        [OpenApiParameter("role", In=ParameterLocation.Query, Type=typeof(Role), Description="filter by unit role, ex: `Leader` or `Leader, Member`")]
        [OpenApiParameter("permission", In=ParameterLocation.Query, Type=typeof(UnitPermissions), Description="filter by unit permissions, ex: `Owner` or `Owner, ManageMembers`")]
        [OpenApiParameter("area", In=ParameterLocation.Query, Type=typeof(Area), Description="filter by unit area, e.g. `uits` or `edge`")]
        public static Task<IActionResult> GetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => PeopleSearchParameters.Parse(req))
                .Bind(query => PeopleRepository.GetAll(query))
                .Finally(people => Response.Ok(req, people));

        [FunctionName(nameof(People.GetOne))]
        [OpenApiOperation(nameof(People.GetOne), nameof(People), Summary = "Get a person by ID")]
        [OpenApiParameter("id", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the person record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Person))]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No person was found with the provided ID.")]
        public static Task<IActionResult> GetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/{id}")] HttpRequest req, int id) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DeterminePersonPermissions(req, requestor, id))
                .Bind(_ => PeopleRepository.GetOne(id))
                .Finally(result => Response.Ok(req, result));

        [FunctionName(nameof(People.GetMemberships))]
        [OpenApiOperation(nameof(People.GetMemberships), nameof(People), Summary = "List unit memberships", Description = "List all units for which this person does IT work.")]
        [OpenApiParameter("id", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the person record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<UnitMember>))]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No person was found with the provided ID.")]
        public static Task<IActionResult> GetMemberships(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/{id}/memberships")] HttpRequest req, int id) 
            => Security.Authenticate(req)
                .Bind(_ => PeopleRepository.GetMemberships(id))
                .Finally(result => Response.Ok(req, result));


        [FunctionName(nameof(People.Update))]
        [OpenApiOperation(nameof(People.Update), nameof(People), Summary = "Update person information", Description = "Update a person's location, expertise, and responsibilities/job classes.\n\n_Authorization_: The JWT must represent either the person whose record is being modified (i.e., a person can modify their own record), or someone who has permissions to manage a unit of which this person is a member (i.e., typically that person's manager/supervisor.)")]
        [OpenApiParameter("id", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the person record.")]
        [OpenApiRequestBody("application/json", typeof(PersonUpdateRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<UnitMember>))]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "No person was found with the provided ID.")]
        [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "The request body was missing or malformed.")]
        [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "You do not have permission to modify this person.")]
        public static Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "people/{id}")] HttpRequest req, int id) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DeterminePersonPermissions(req, requestor, id))
                .Bind(_ => Request.DeserializeBody<PersonUpdateRequest>(req))
                .Bind(body => PeopleRepository.Update(id, body))
                .Finally(result => Response.Ok(req, result));

    }
}
