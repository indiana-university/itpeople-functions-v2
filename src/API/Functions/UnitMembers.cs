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
    public static class UnitMembers
    {
        [FunctionName(nameof(UnitMembers.UnitMembersGetAll))]
        [OpenApiOperation(nameof(UnitMembers.UnitMembersGetAll), nameof(UnitMembers), Summary="List all IT unit memberships" )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitMemberResponse>), Description = "A collection of unit membership record")]
        public static Task<IActionResult> UnitMembersGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "memberships")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => UnitMembersRepository.GetAll())
                .Bind(res => Pipeline.Success(res.Select(e=>e.ToUnitMemberResponse())))
                .Finally(dtos => Response.Ok(req, dtos));    
    }
}