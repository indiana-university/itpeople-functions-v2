using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Models;
using NUnit.Framework;
using Tasks;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using Npgsql;

namespace Integration
{
    public class TasksTests
    {
		public class Tools : ApiTest
		{
			// A DendoResponse<DenodoBuilding> where one building is missing the building_long_description
			private static string _SampleBuildingJson = @"{""name"": ""building_summary_i"",""elements"": [{""building_code"": ""BL049A"",""site_code"": ""BL"",""building_name"": ""704 E 8TH ST"",""building_long_description"": ""704 E 8TH STREET"",""street"": ""704 E 8TH ST"",""city"": ""BLOOMINGTON"",""state"": ""IN"",""zip"": ""47408-3842"",""latitude"": 39.16930196,""longitude"": -86.52562183,""peoplesoft_building_code"": ""UTI"",""has_peoplesoft_code"": ""0"",""public_flag"": ""Y"",""enabled_for_buyiu"": ""Y"",""building_function_code"": ""A"",""building_function_desc"": ""ACADEMIC"",""year_acquired"": 0,""building_use"": ""PROVOST GUEST HOUSING"",""registrar_code_2"": ""WE"",""registrar_code_4"": ""BLWE"",""orig_architect"": null,""gross_sq_ft"": 3921},{""building_code"": ""IN170X"",""site_code"": ""IN"",""building_name"": ""846 N SENATE AVE"",""building_long_description"": ""846 N. SENATE AVE."",""street"": ""846 N SENATE AVE"",""city"": ""INDIANAPOLIS"",""state"": ""IN"",""zip"": ""46202-4120"",""latitude"": 0,""longitude"": 0,""peoplesoft_building_code"": null,""has_peoplesoft_code"": ""0"",""public_flag"": ""N"",""enabled_for_buyiu"": ""Y"",""building_function_code"": ""A"",""building_function_desc"": ""ACADEMIC"",""year_acquired"": 2004,""building_use"": ""LEASED STORAGE"",""registrar_code_2"": null,""registrar_code_4"": null,""orig_architect"": null,""gross_sq_ft"": 150},{""building_code"": ""BL222M"",""site_code"": ""BL"",""building_name"": ""309 S MITCHELL ST"",""building_long_description"": null,""street"": ""309 S MITCHELL ST"",""city"": ""BLOOMINGTON"",""state"": ""IN"",""zip"": ""47401"",""latitude"": null,""longitude"": null,""peoplesoft_building_code"": null,""has_peoplesoft_code"": ""0"",""public_flag"": ""N"",""enabled_for_buyiu"": ""N"",""building_function_code"": ""J"",""building_function_desc"": ""RENTAL"",""year_acquired"": 2022,""building_use"": null,""registrar_code_2"": null,""registrar_code_4"": null,""orig_architect"": null,""gross_sq_ft"": 0},{""building_code"": ""BL001C"",""site_code"": ""BL"",""building_name"": ""4TH & INDIANA-LEWIS BLDG"",""building_long_description"": ""LEWIS BUILDING"",""street"": ""116 S INDIANA AVE"",""city"": ""BLOOMINGTON"",""state"": ""IN"",""zip"": ""47405"",""latitude"": 39.16575625,""longitude"": -86.5271417,""peoplesoft_building_code"": ""LEWS"",""has_peoplesoft_code"": ""1"",""public_flag"": ""N"",""enabled_for_buyiu"": ""Y"",""building_function_code"": ""A"",""building_function_desc"": ""ACADEMIC"",""year_acquired"": 0,""building_use"": ""LAW CLINIC"",""registrar_code_2"": ""LS"",""registrar_code_4"": ""BLLS"",""orig_architect"": null,""gross_sq_ft"": 10146}],""links"": [{""rel"": ""self"",""href"": ""https://ebidvt-stg.uits.iu.edu/server/iu_vpcpf_spaceinfo/building_summary/views/building_summary_i?%24filter=building_function_code+ne+%27E%27""}]}";
			private static string _SampleProfileApiJson = @"{""page"": {""totalRecords"": 47511,""previousPage"": ""https://prs.apps.iu.edu?affiliationType=employee&page=61&pageSize=250"",""firstPage"": ""https://prs.apps.iu.edu?affiliationType=employee&page=0&pageSize=250"",""lastPage"": ""https://prs.apps.iu.edu?affiliationType=employee&page=190&pageSize=250"",""nextPage"": ""https://prs.apps.iu.edu?affiliationType=employee&page=63&pageSize=250"",""currentPage"": ""https://prs.apps.iu.edu?affiliationType=employee&page=62&pageSize=250""},""employees"": [{""firstName"": ""John"",""lastName"": ""Doe"",""jobs"": [{""jobStatus"": ""P"",""jobDepartmentDesc"": ""TRAVEL MANAGEMENT SERVICES"",""jobDepartmentId"": ""UA-TRMS"",""position"": ""Procurement Rep""}],""email"": ""jdoe@iu.edu"",""contacts"": [{""phoneNumber"": ""812/555-5555"",""campusCode"": ""BL""}],""username"": ""jdoe""},{""firstName"": ""John"",""lastName"": ""Public"",""jobs"": [{""jobStatus"": ""P"",""jobDepartmentDesc"": ""SCHOOL OF SOCIAL WORK"",""jobDepartmentId"": ""IN-SOCW"",""position"": ""Admin Generalist Coord""}],""email"": ""jqpublic@iupui.edu"",""contacts"": [{""phoneNumber"": ""317/555-5555"",""campusCode"": ""IN""}],""username"": ""jqpublic""},{""firstName"": ""Johnathan"",""lastName"": ""Law"",""jobs"": [{""jobStatus"": ""P"",""jobDepartmentDesc"": ""CTSI CLINICAL RESEARCH SERVICE"",""jobDepartmentId"": ""IN-PCIR"",""position"": ""Registered Nurse""}],""email"": ""jlaw@iu.edu"",""contacts"": [{""phoneNumber"": ""317/555-5556"",""campusCode"": ""IN""}],""username"": ""jlaw""}]}";
			
			[Test]
			public async Task GetToolGranteesHonorsInactiveUnits()
			{
				var testContext = new MockDurableActivityContext();
				System.Environment.SetEnvironmentVariable("DatabaseConnectionString", Database.PeopleContext.LocalDatabaseConnectionString);

				// Generate a member tool of April and Hammer on the inactive Unit.
				var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
				db.MemberTools.Add(new MemberTool
				{
					MembershipId = TestEntities.UnitMembers.ArchivedAprilId,
					ToolId = TestEntities.Tools.HammerId
				});
				await db.SaveChangesAsync();

				// Fetch the grantees.
				var result = await Tasks.Tools.GetToolGrantees(testContext, TestEntities.Tools.Hammer);
				
				var expected = new List<string> { TestEntities.People.ServiceAdmin.Netid, TestEntities.People.RSwanson.Netid };
				CollectionAssert.AreEquivalent(expected, result);
			}
			
			[Test]
			public async Task MapToBuildingBlankDescription()
			{
				// Make a response to feed to Tasks.Utils.DeserializeResponse()
				var resp = new System.Net.Http.HttpResponseMessage();
				resp.RequestMessage = new System.Net.Http.HttpRequestMessage()
				{
					RequestUri = new Uri("https://fake.com/a/fake/path/for/buildings")
				};
				resp.Content = new System.Net.Http.StringContent(_SampleBuildingJson);
				resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
				resp.StatusCode = System.Net.HttpStatusCode.OK;

				var results = await Tasks.Utils.DeserializeResponse<DenodoResponse<DenodoBuilding>>(nameof(MapToBuildingBlankDescription), resp, "Testing what the heck is going on.");
				Assert.AreEqual(4, results.Elements.Count());

				// Validate mapping works, and records are successfully stored in the database.
				// This ensures the mapped building passes valdiation.
				var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
				foreach(var rawBuilding in results.Elements)
				{
					var building = new Building();
					rawBuilding.MapToBuilding(building);
					await db.Buildings.AddAsync(building);
					await db.SaveChangesAsync();
				}
			}

			[Test]
			public async Task LoggingToPostgresSinkWorks()
			{
				// Set an environment variable so the Postgresql logging sink and be setup.
				System.Environment.SetEnvironmentVariable("DatabaseConnectionString", Database.PeopleContext.LocalDatabaseConnectionString);

				var errorMessage = "This is a test error";
				var logger = Logging.GetLogger(nameof(LoggingToPostgresSinkWorks));
				logger.Error(errorMessage);

				var logs = new List<string>();
				
				using var connection = new NpgsqlConnection(Database.PeopleContext.LocalDatabaseConnectionString);
				await connection.OpenAsync();
				
				var query = $"SELECT message FROM logs_automation";

				// Due to our logging implementation we cannot manually "flush" the logger
				// forcing it to write to its sinks. Instead we'll wait a reasonable
				// amount of time for it to write to the DB.
				
				var cutoff = DateTimeOffset.Now.AddSeconds(20);

				while(DateTimeOffset.Now < cutoff && logs.Count == 0)
				{
					await Task.Delay(250);
					using var command = new NpgsqlCommand(query, connection);
					using var reader = await command.ExecuteReaderAsync();
					
					while(await reader.ReadAsync())
					{
						try
						{
							var logMessage = await reader.GetFieldValueAsync<string>(0);
							logs.Add(logMessage);
						}
						catch {}
					}
				}
				
				Assert.AreEqual(1, logs.Count);
				Assert.Contains(errorMessage, logs);
			}
		}

		public class MockDurableActivityContext : IDurableActivityContext
		{
			public string InstanceId => throw new NotImplementedException();
			public string Name => throw new NotImplementedException();

			public T GetInput<T>()
			{
				throw new NotImplementedException();
			}
		}
	}
}