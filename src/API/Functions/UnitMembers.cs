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
        [OpenApiOperation(nameof(UnitMembers.UnitMembersGetAll), nameof(UnitMembers), Summary="List all IT units", Description = @"Search for IT units by name and/or description. If no search term is provided, lists all top-level IT units." )]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<UnitMemberResponse>))]
        public static Task<IActionResult> UnitMembersGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "memberships")] HttpRequest req) 
            => Security.Authenticate(req)
                .Bind(_ => UnitMembersRepository.GetAll())
                .Finally(members => Response.Ok(req, members));        
    }
}