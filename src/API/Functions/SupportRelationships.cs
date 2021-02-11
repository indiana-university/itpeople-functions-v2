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
    public static class SupportRelationships
    {
        public const string SupportRelationshipsTitle = "Support Relationships";

		[FunctionName(nameof(SupportRelationships.SupportRelationshipsGetAll))]
        [OpenApiOperation(nameof(SupportRelationships.SupportRelationshipsGetAll), SupportRelationshipsTitle, Summary="List all unit-department support relationships")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<SupportRelationship>), Description="A collection of department support relationship records")]
        public static Task<IActionResult> SupportRelationshipsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supportRelationships")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(query => SupportRelationshipsRepository.GetAll())
                .Finally(results => Response.Ok(req, results));

    }
}