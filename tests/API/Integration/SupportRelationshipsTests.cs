using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Linq;
using API.Middleware;
using API.Data;
using Newtonsoft.Json;

namespace Integration
{
	public class SupportRelationshipsTests
	{

		public class GetAll : ApiTest
		{
			[Test]
			public async Task HasCorrectNumber()
			{
				var resp = await GetAuthenticated("supportRelationships");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<SupportRelationship>>();
				Assert.AreEqual(2, actual.Count);
			}
		}
	}
}