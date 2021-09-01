using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Models;
using NUnit.Framework;

namespace Integration
{
    public class TasksTests
    {
		public class Tools : ApiTest
		{
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
			
		}

		public class MockDurableActivityContext : IDurableActivityContext
		{
			public string InstanceId => throw new NotImplementedException();

			public T GetInput<T>()
			{
				throw new NotImplementedException();
			}
		}
	}
}