using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Xml.Serialization;
using System.Linq;

namespace Integration
{
    [TestFixture]
    [Category("LSP")]
    public class LspTests : ApiTest
    {        
        [Test]
        public async Task GetLspList()
        {
            var resp = await GetAuthenticated("LspdbWebService.svc/LspList");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await DeserializeXml<LspInfoArray>(resp);
            Assert.NotNull(actual);
            Assert.NotNull(actual.LspInfos);
            // Parks and Rec has a support relationship with one or more departments, so all Parks and Rec
            // staffers are considered LSPs. They will have the "LA" flag if they are in the Leader or Sublead roles.
            Assert.AreEqual(2, actual.LspInfos.Length);
            Assert.True(actual.LspInfos.Any(i => i.NetworkID == TestEntities.People.RSwanson.Netid && i.IsLA == true));
            Assert.True(actual.LspInfos.Any(i => i.NetworkID == TestEntities.People.LKnope.Netid && i.IsLA == true));
        }

        [TestCase("lknope", new string[]{TestEntities.Departments.ParksName, TestEntities.Departments.FireName})]
        [TestCase("rswanson", new string[]{TestEntities.Departments.ParksName, TestEntities.Departments.FireName})]
        [TestCase("bwyatt", new string[0])]
        public async Task GetLspDepartments(string netid, string[] expectedDepartments)
        {
            var resp = await GetAuthenticated($"LspdbWebService.svc/LspDepartments/{netid}");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await DeserializeXml<LspDepartmentArray>(resp);
            Assert.NotNull(actual);
            Assert.AreEqual(netid, actual.NetworkID);
            Assert.NotNull(actual.DeptCodeLists);
            Assert.AreEqual(expectedDepartments.Length, actual.DeptCodeLists.Count());
            var actualDepartments = actual.DeptCodeLists.SelectMany(d => d.Values);
            CollectionAssert.AreEquivalent(expectedDepartments, actualDepartments);
        }

        [Test]
        public async Task GetLspDepartments_StringFormatting()
        {
            var resp = await GetAuthenticated($"LspdbWebService.svc/LspDepartments/ lKnOPe  ");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await DeserializeXml<LspDepartmentArray>(resp);
            Assert.NotNull(actual);
            Assert.AreEqual("lknope", actual.NetworkID);
            Assert.AreEqual(2, actual.DeptCodeLists.Count());
        }

        private static async Task<T> DeserializeXml<T>(HttpResponseMessage resp) where T : class
        {
            var str = await resp.Content.ReadAsStringAsync();
            System.Console.Out.WriteLine(str); // will only appear if the test fails
            var stream = await resp.Content.ReadAsStreamAsync();
            var result = new XmlSerializer(typeof(T)).Deserialize(stream);
            return result as T;
        }
    }
}