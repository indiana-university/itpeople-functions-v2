using System.Collections.Generic;
using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Models;
using Database;
using Microsoft.AspNetCore.Http;
using System;

namespace API.Data
{
	public class UnitMemberToolsRepository : DataRepository
	{
		public const string MalformedBody = "The request body was malformed, the tool was missing, or the member was missing.";
		public const string ToolNotFound = "The specified tool does not exist.";
		public const string MemberNotFound = "The specified member does not exist.";
		public const string MemberToolConflict = "The provided member already has access to the provided tool.";

		internal static Task<Result<List<MemberTool>, Error>> GetAll()
			=> ExecuteDbPipeline("search all unit member tools", async db =>
			{
				var result = await db.MemberTools
					.Include(mt => mt.Tool)
					.Include(mt => mt.UnitMember)
					.AsNoTracking()
					.ToListAsync();
				return Pipeline.Success(result);
			});

		internal static async Task<Result<MemberTool, Error>> CreateUnitMemberTool(MemberToolRequest body)
			=> await ExecuteDbPipeline("create a unit member tool", db =>
				ValidateRequest(db, body)
				.Bind(_ => TryCreateUnitMemberTool(db, body))
				.Bind(created => TryFindUnitMemberTool(db, created.Id))
			);

		private static async Task<Result<MemberToolRequest, Error>> ValidateRequest(PeopleContext db, MemberToolRequest body, int? existingRelationshipId = null)
		{
			if (body.MembershipId == 0 || body.ToolId == 0)
			{
				return Pipeline.BadRequest(MalformedBody);
			}
			//Unit is already being checked/found in the Authorization step
			if (await db.Tools.AnyAsync(t => t.Id == body.ToolId) == false)
			{
				return Pipeline.NotFound(ToolNotFound);
			}
			if (await db.UnitMembers.AnyAsync(um => um.Id == body.MembershipId) == false)
			{
				return Pipeline.NotFound(MemberNotFound);
			}
			if (await db.MemberTools.AnyAsync(mt => mt.MembershipId == body.MembershipId && mt.ToolId == body.ToolId && mt.Id != existingRelationshipId))
			{
				return Pipeline.Conflict(MemberToolConflict);
			}
			return Pipeline.Success(body);
		}

		private static async Task<Result<MemberTool, Error>> TryFindUnitMemberTool(PeopleContext db, int memberToolId)
		{
			var result = await db.MemberTools
				.Include(mt => mt.UnitMember)
				.Include(mt => mt.Tool)
				.SingleOrDefaultAsync(r => r.Id == memberToolId);
			return result == null
				? Pipeline.NotFound("The specified member/tool does not exist.")
				: Pipeline.Success(result);
		}

		private static async Task<Result<MemberTool, Error>> TryCreateUnitMemberTool(PeopleContext db, MemberToolRequest body)
		{
			var memberTool = new MemberTool
			{
				MembershipId = body.MembershipId,
				ToolId = body.ToolId
			};

			await db.MemberTools.AddAsync(memberTool);
			await db.SaveChangesAsync();
			return Pipeline.Success(memberTool);
		}
	}
}