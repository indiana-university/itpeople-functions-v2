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
	/// <Summary> This collection of endpoints exists to provide backwards-compatible
	/// support for some tools used by UIPO and IMS. They are not intended to be used
	/// by API implementers, and so they are excluded from the OpenAPI docs.</Summary>
    public static class Lsp
	{
		[FunctionName(nameof(Lsp.LspList))]
		[OpenApiIgnore]
		/// Fetch a list of all LSPs.
		/// LSPs are defined as any member of a unit that has a support relationship with
        /// one or more departmens. An "LA" is a "local administrator" of LSPs. For IT 
		/// People this corresponds to an LSP in the Leader or Sublead roles.</Summary>
		public static Task<IActionResult> LspList(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "LspdbWebService.svc/LspList")] HttpRequest req)
			=> LspRepository.GetLspList()
				.Finally(result => Response.OkXml(req, result));		

		[FunctionName(nameof(Lsp.LspDepartments))]
		[OpenApiIgnore]
		/// Fetch all departments supported by a given LSP.
		/// For the given netid, collect all the departments supported by all the units of  
		/// which the person is a member.
		public static Task<IActionResult> LspDepartments(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "LspdbWebService.svc/LspDepartments/{netid}")] HttpRequest req, 
			string netid)
			=> LspRepository.GetLspDepartments(netid)
				.Finally(result => Response.OkXml(req, result));		

		[FunctionName(nameof(Lsp.DepartmentLsps))]
		[OpenApiIgnore]
		/// Fetch all LSPs supporting a given department.
		/// For the given department name, collect all the LSPs in all the units supporting
		/// this department. There is typically only one unit supporting a given department.
		public static Task<IActionResult> DepartmentLsps(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "LspdbWebService.svc/LspsInDept/{department}")] HttpRequest req, 
			string department)
			=> LspRepository.GetDepartmentLsps(department)
				.Finally(result => Response.OkXml(req, result));		
	}
}