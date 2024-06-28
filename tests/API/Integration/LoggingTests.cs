using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Integration
{
	public class LoggingTests : ApiTest
	{
		private class Log
		{
			public DateTime Timestamp { get; set; }
			public string Level { get; set; }
			public int Elapsed { get; set; }
			public int Status { get; set; }
			public string Method { get; set; }
			public string Function { get; set; }
			public string Parameters { get; set; }
			public string Query { get; set; }
			public string Detail { get; set; }
			public string Exception { get; set; }
			[Column("ip_address")]
			public string IpAddress { get; set; }
			[Column("netid")]
			public string NetId { get; set; }
			public string Content { get; set; }
			public string Request { get; set; }
			public string Record { get; set; }
		}

		private PeopleContext GetDb() => PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
		private async Task<List<Log>> GetAllLogs()
		{
			using var db = GetDb();
			var logs = await db.Database
				.SqlQuery<Log>($"SELECT \"timestamp\", \"level\", elapsed, \"status\", method, \"function\", parameters, query, detail, \"exception\", ip_address, netid, content, request, record FROM logs")
				.AsNoTracking()
				.ToListAsync();
			
			return logs;
		}

		private T DeepCopy<T>(T UpdatedEntity)
		{
			return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(UpdatedEntity, Json.JsonSerializerSettings), Json.JsonSerializerSettings);
		}

		[Test]
		public async Task MakingUnitInactiveIsLoggedCorrectly()
		{
			// Make a new Unit.
			var uc = new UnitsTests.UnitCreate();
			await uc.CreateMayorsOffice();

			var logs = await GetAllLogs();
			Assert.That(logs.Count, Is.EqualTo(1));

			// Get the unit from the DB and then de-activate it.
			using var db = GetDb();
			var unit = db.Units.OrderBy(u => u.Id).AsNoTracking().Last();
			Assert.That(unit.Active, Is.True);
			
			var resp = await DeleteAuthenticated($"units/{unit.Id}/archive", ValidAdminJwt);
			AssertStatusCode(resp, HttpStatusCode.OK);

			// Confirm unit is not active
			await db.Entry(unit).ReloadAsync();
			Assert.That(unit.Active, Is.False);

			// Refresh the logs
			logs = await GetAllLogs();
			Assert.That(logs.Count, Is.EqualTo(2));

			var lastLog = logs.OrderBy(l => l.Timestamp).Last();

			Assert.That(lastLog.Function, Is.EqualTo("units"));
			Assert.That(lastLog.Method, Is.EqualTo("DELETE"));
			Assert.That(lastLog.NetId, Is.EqualTo("johndoe"));
			Assert.That(lastLog.Parameters, Is.EqualTo($"{unit.Id}/archive"));
			Assert.That(lastLog.Query, Is.Empty);
			Assert.That(lastLog.Record, Is.Not.Empty);
			Assert.That(lastLog.Request, Is.Null);
			Assert.That(lastLog.Status, Is.EqualTo(200));
			Assert.That(lastLog.Content, Is.Null);
			Assert.That(lastLog.Exception, Is.Null);
		}

		[Test]
		public async Task ErrorsAreWellLogged()
		{
			// Make a request to POST a unit hat fails.
			var malformedUnit = DeepCopy(TestEntities.Units.CityOfPawnee);
			malformedUnit.Name = null;
			var resp = await PostAuthenticated("units", malformedUnit, ValidAdminJwt);
			AssertStatusCode(resp, HttpStatusCode.BadRequest);

			// Confirm the logs were generated.
			var logs = await GetAllLogs();
			Assert.That(logs.Count, Is.EqualTo(1));

			var log = logs.Single();

			Assert.That(log.Content, Is.Null);
			Assert.That(log.Detail, Contains.Substring("The request body is malformed or missing. The Name field is required."));
			Assert.That(log.Exception, Is.Null);
			Assert.That(log.Function, Is.EqualTo("units"));
			Assert.That(log.Level, Is.EqualTo("Error"));
			Assert.That(log.Method, Is.EqualTo("POST"));
			Assert.That(log.NetId, Is.EqualTo("johndoe"));
			Assert.That(log.Parameters, Is.Empty);
			Assert.That(log.Query, Is.Empty);
			Assert.That(log.Record, Is.Null);
			Assert.That(log.Request, Is.Not.Null);
			Assert.That(log.Status, Is.EqualTo(400));
		}

		[Test]
		public async Task ExceptionsAreLogged()
		{
			// Try to induce an error that should record an exception
			var resp = await GetAuthenticated("ExerciseLogger", ValidAdminJwt);

			// Confirm the logs were generated.
			var logs = await GetAllLogs();
			Assert.That(logs.Count, Is.EqualTo(1));

			var log = logs.Single();

			Assert.That(log.Exception, Is.Not.Null);
		}
	}
}

