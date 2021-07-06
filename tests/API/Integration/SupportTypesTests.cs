using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;

namespace Integration
{
	public class SupportTypesTests
	{

		public class GetAll : ApiTest
		{
			[Test]
			public async Task SupportTypesGetAllHasCorrectNumber()
			{
				var resp = await GetAuthenticated("supportTypes");
				AssertStatusCode(resp, HttpStatusCode.OK);
				var actual = await resp.Content.ReadAsAsync<List<SupportType>>();
				Assert.AreEqual(4, actual.Count);
			}
		}
	}
}