using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using API.Data;

namespace API.Functions
{
    public static class Lsp
	{
		[FunctionName(nameof(Lsp.LspList))]
		[OpenApiIgnore]
		public static Task<IActionResult> LspList(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "LspdbWebService.svc/LspList")] HttpRequest req)
			=> LspRepository.GetLspList()
				.Finally(result => Response.OkXml(req, result));		

		[FunctionName(nameof(Lsp.LspDepartments))]
		[OpenApiIgnore]
		public static Task<IActionResult> LspDepartments(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "LspdbWebService.svc/LspDepartments/{netid}")] HttpRequest req, 
			string netid)
			=> LspRepository.GetLspDepartments(netid)
				.Finally(result => Response.OkXml(req, result));		
	}
}