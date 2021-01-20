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
using System;

namespace API.Functions
{
    public static class People
    {
        [FunctionName(nameof(People.GetAll))]
        [OpenApiOperation(nameof(People.GetAll), Summary="Searches all people.")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Person>))]
        public static Task<IActionResult> GetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => ResolveSearchQueryParameters(req))
                .Bind(query => PeopleRepository.GetAllAsync(query))
                .Finally(people => Response.Ok(req, people));

        public class PeopleSearchQueryParameters
        {
            public PeopleSearchQueryParameters(string q, Responsibilities responsibilities )
            {
                Q = q;
                Responsibilities = responsibilities;
            }
            
            public string Q { get; }
            public Responsibilities Responsibilities { get; }
        }

        private static Result<PeopleSearchQueryParameters, Error> ResolveSearchQueryParameters(HttpRequest req)
        { 
            var queryParms = req.GetQueryParameterDictionary(); 
            queryParms.TryGetValue("q", out string q);
            queryParms.TryGetValue("class", out string jobClass);
            var responsibilities = string.IsNullOrWhiteSpace(jobClass) 
                ? Responsibilities.None 
                : (Responsibilities)Enum.Parse(typeof(Responsibilities), jobClass);
            var result = new PeopleSearchQueryParameters(q, responsibilities);
            return Pipeline.Success(result);
        }

        [FunctionName(nameof(People.GetByNetid))]
        [OpenApiOperation(nameof(People.GetByNetid))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Person))]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Summary="No person was found matching the provided netid.")]
        public static Task<IActionResult> GetByNetid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people/getbynetid")] HttpRequest req) 
            {
                var repo = new PeopleRepository();
                return repo.GetByNetId("")
                    .Finally(result => Response.Ok(req, result));
            }
    }
}
