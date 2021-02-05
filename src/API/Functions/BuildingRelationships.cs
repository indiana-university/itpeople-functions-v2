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
    public static class BuildingRelationships
    {

        public const string BuildingRelationshipsTitle = "Building Relationships";

		[FunctionName(nameof(BuildingRelationships.BuildingRelationshipsGetAll))]
        [OpenApiOperation(nameof(BuildingRelationships.BuildingRelationshipsGetAll), BuildingRelationshipsTitle, Summary="List all unit-building support relationships")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<BuildingRelationship>), Description="A collection of building support relationship records")]
        public static Task<IActionResult> BuildingRelationshipsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "buildingRelationships")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => BuildingRelationshipsRepository.GetAll())
                .Finally(r => Response.Ok(req, r));

    }
}