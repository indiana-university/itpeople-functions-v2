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
        [OpenApiOperation(nameof(People.GetAll), Summary="Searches all people.", Description = @"Search results are unioned within a filter and intersected across filters. For example, `interest=node, lambda` will return people with an interest in either `node` OR `lambda`, whereas `role=ItLeadership&interest=node` will only return people who are both in `ItLeadership AND have an interest in `node`." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Person>))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In=ParameterLocation.Query, Description="filter by name/netid, ex: `Ron` or `rswanso`")]
        [OpenApiParameter("class", In=ParameterLocation.Query, Type=typeof(Responsibilities), Description="filter by job classification/responsibility, ex: `UserExperience` or `UserExperience, WebAdminDevEng`")]
        [OpenApiParameter("interest", In=ParameterLocation.Query, Description="filter by one interests, ex: `serverless` or `node, lambda`")]
        [OpenApiParameter("campus", In=ParameterLocation.Query, Description="filter by primary campus: `Bloomington`, `Indianapolis`, `Columbus`, `East`, `Fort Wayne`, `Kokomo`, `Northwest`, `South Bend`, `Southeast`")]
        [OpenApiParameter("role", In=ParameterLocation.Query, Type=typeof(Role), Description="filter by unit role, ex: `Leader` or `Leader, Member`")]
        [OpenApiParameter("permission", In=ParameterLocation.Query, Type=typeof(UnitPermissions), Description="filter by unit permissions, ex: `Owner` or `Owner, ManageMembers`")]
        [OpenApiParameter("area", In=ParameterLocation.Query, Type=typeof(Area), Description="filter by unit area, e.g. `uits` or `edge`")]
        public static Task<IActionResult> GetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/people")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => PeopleSearchParameters.Parse(req))
                .Bind(query => PeopleRepository.GetAllAsync(query))
                .Finally(people => Response.Ok(req, people));

        [FunctionName(nameof(People.GetByNetid))]
        [OpenApiOperation(nameof(People.GetByNetid))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Person))]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Summary="No person was found matching the provided netid.")]
        public static Task<IActionResult> GetByNetid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/people/getbynetid")] HttpRequest req) 
            {
                var repo = new PeopleRepository();
                return repo.GetByNetId("")
                    .Finally(result => Response.Ok(req, result));
            }
    }
}
