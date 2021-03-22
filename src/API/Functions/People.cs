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
    public static class People
    {
        [FunctionName(nameof(People.PeopleGetAll))]
        [OpenApiOperation(nameof(People.PeopleGetAll), nameof(People), Summary="Search all people", Description = @"Search results are unioned within a filter and intersected across filters. For example, `interest=node, lambda` will return people with an interest in either `node` OR `lambda`, whereas `role=ItLeadership&interest=node` will only return people who are both in `ItLeadership AND have an interest in `node`." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<Person>))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In=ParameterLocation.Query, Description="filter by name/netid, ex: `Ron` or `rswanso`")]
        [OpenApiParameter("class", In=ParameterLocation.Query, Type=typeof(ResponsibilitiesPropDoc), Description="filter by job classification/responsibility, ex: `UserExperience` or `UserExperience, WebAdminDevEng`")]
        [OpenApiParameter("interest", In=ParameterLocation.Query, Description="filter by one interests, ex: `serverless` or `node, lambda`")]
        [OpenApiParameter("campus", In=ParameterLocation.Query, Description="filter by primary campus: `Bloomington`, `Indianapolis`, `Columbus`, `East`, `Fort Wayne`, `Kokomo`, `Northwest`, `South Bend`, `Southeast`")]
        [OpenApiParameter("role", In=ParameterLocation.Query, Type=typeof(Role), Description="filter by unit role, ex: `Leader` or `Leader, Member`")]
        [OpenApiParameter("permission", In=ParameterLocation.Query, Type=typeof(UnitPermissions), Description="filter by unit permissions, ex: `Owner` or `Owner, ManageMembers`")]
        [OpenApiParameter("area", In=ParameterLocation.Query, Type=typeof(Area), Description="filter by unit area, e.g. `uits` or `edge`")]
        public static Task<IActionResult> PeopleGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => PeopleSearchParameters.Parse(req))
                .Bind(query => PeopleRepository.GetAll(query))
                .Finally(people => Response.Ok(req, people));

        [FunctionName(nameof(People.PeopleGetOne))]
        [OpenApiOperation(nameof(People.PeopleGetOne), nameof(People), Summary = "Get a person by ID")]
        [OpenApiParameter("id", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the person record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(Person))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No person was found with the provided ID.")]
        public static Task<IActionResult> PeopleGetOne(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/{id}")] HttpRequest req, int id) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DeterminePersonPermissions(req, requestor, id))
                .Bind(_ => PeopleRepository.GetOne(id))
                .Finally(result => Response.Ok(req, result));

        [FunctionName(nameof(People.PeopleGetMemberships))]
        [OpenApiOperation(nameof(People.PeopleGetMemberships), nameof(People), Summary = "List unit memberships", Description = "List all units for which this person does IT work.")]
        [OpenApiParameter("id", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the person record.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitMemberResponse>))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No person was found with the provided ID.")]
        public static Task<IActionResult> PeopleGetMemberships(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/{id}/memberships")] HttpRequest req, string id) 
            {
               if(int.TryParse(id, out int value))
               {
                   return  Security.Authenticate(req)
                    .Bind(_ => PeopleRepository.GetMemberships(value))
                    .Finally(result => Response.Ok(req, result));
               }
               else
               {
                   return  Security.Authenticate(req)
                    .Bind(_ => PeopleRepository.GetMemberships(id))
                    .Finally(result => Response.Ok(req, result));

               }                
                
            }


        [FunctionName(nameof(People.PeopleUpdate))]
        [OpenApiOperation(nameof(People.PeopleUpdate), nameof(People), Summary = "Update person information", Description = "Update a person's location, expertise, and responsibilities/job classes.\n\n_Authorization_: The JWT must represent either the person whose record is being modified (i.e., a person can modify their own record), or someone who has permissions to manage a unit of which this person is a member (i.e., typically that person's manager/supervisor.)")]
        [OpenApiParameter("id", Type = typeof(int), In = ParameterLocation.Path, Required = true, Description = "The ID of the person record.")]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(PersonUpdateRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(Person))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, MediaTypeNames.Application.Json, typeof(ApiError), Description = "No person was found with the provided ID.")]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description = "The request body was missing or malformed.")]
        [OpenApiResponseWithBody(HttpStatusCode.Forbidden, MediaTypeNames.Application.Json, typeof(ApiError), Description = "You do not have permission to modify this person.")]
        public static Task<IActionResult> PeopleUpdate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "people/{id}")] HttpRequest req, int id) 
            => Security.Authenticate(req)
                .Bind(requestor => AuthorizationRepository.DeterminePersonPermissions(req, requestor, id))
                .Bind(perms => AuthorizationRepository.AuthorizeModification(perms))
                .Bind(_ => Request.DeserializeBody<PersonUpdateRequest>(req))
                .Bind(body => PeopleRepository.Update(req, id, body))
                .Finally(result => Response.Ok(req, result));


        //Check people table first, if no records check HR people
        [FunctionName(nameof(People.PeopleLookup))]
        [OpenApiOperation(nameof(People.PeopleLookup), nameof(People), Summary="Search all staff", Description = @"Search for staff, including IT People, by name or username (netid)." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<PeopleLookupItem>))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, MediaTypeNames.Application.Json, typeof(ApiError), Description="The search query was malformed or incorrect. See response content for additional information.")]
        [OpenApiParameter("q", In=ParameterLocation.Query, Description="filter by name/netid, ex: `Ron` or `rswanso`")]
        [OpenApiParameter("_limit", In=ParameterLocation.Query, Description="Restrict the number of responses to no more than this integer.  ex: `15` or `20`")]
        public static Task<IActionResult> PeopleLookup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people-lookup")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => HrPeopleSearchParameters.Parse(req))
                .Bind(query => Request.ValidateBody(query)) //Validate query params
                .Bind(query => PeopleRepository.GetAllWithHr(query))
                .Finally(people => Response.Ok(req, people));

    }
}
