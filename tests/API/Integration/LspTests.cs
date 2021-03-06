using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;
using System.Xml.Serialization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Integration
{
    [TestFixture]
    [Category("LSP")]
    public class LspTests : ApiTest
    {        
        [Test]
        public async Task GetLspList()
        {
            var resp = await GetAnonymous("LspdbWebService.svc/LspList");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await DeserializeXml<LspInfoArray>(resp);
            Assert.NotNull(actual);
            Assert.NotNull(actual.LspInfos);
            // Parks and Rec has a support relationship with one or more departments, so all Parks and Rec
            // staffers are considered LSPs. They will have the "LA" flag if they are in the Leader or Sublead roles.
            Assert.AreEqual(1, actual.LspInfos.Length);
            Assert.True(actual.LspInfos.Any(i => i.NetworkID == TestEntities.People.ServiceAdmin.Netid && i.IsLA == true));
        }

        [TestCase("lknope", new string[0])]
        [TestCase("rswanso", new string[0])]
        [TestCase("johndoe", new string[]{TestEntities.Departments.FireName, TestEntities.Departments.ParksName})]
        [TestCase("bwyatt", new string[0])]
        [TestCase("bad username", new string[0])]
        public async Task GetLspDepartments(string netid, string[] expectedDepartments)
        {
            var resp = await GetAnonymous($"LspdbWebService.svc/LspDepartments/{netid}");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await DeserializeXml<LspDepartmentArray>(resp);
            Assert.NotNull(actual);
            Assert.AreEqual(netid, actual.NetworkID);
            Assert.NotNull(actual.DeptCodeList);
            Assert.AreEqual(expectedDepartments.Length, actual.DeptCodeList.A.Count());
            CollectionAssert.AreEquivalent(expectedDepartments, actual.DeptCodeList.A);
        }

        [Test]
        public async Task GetLspDepartments_Properties()
        {
            var resp = await GetAnonymous($"LspdbWebService.svc/LspDepartments/ jOhnDOE  ");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await DeserializeXml<LspDepartmentArray>(resp);
            Assert.NotNull(actual);
            Assert.AreEqual("johndoe", actual.NetworkID);
            Assert.AreEqual(2, actual.DeptCodeList.A.Count());
        }

        [Test]
        public async Task GetLspDepartmentsWellFormedMultipleDepartments()
        {
            var resp = await GetAnonymous($"LspdbWebService.svc/LspDepartments/johndoe");
            AssertStatusCode(resp, HttpStatusCode.OK);
            
            var responseString = await resp.Content.ReadAsStringAsync();
            var deptCodeTag = Regex.Escape("<DeptCodeList>");
            Assert.AreEqual(1, Regex.Matches(responseString, deptCodeTag).Count());
            
            var aTag = Regex.Escape("<a>");
            Assert.AreEqual(2, Regex.Matches(responseString, aTag).Count());
            
            var actual = await DeserializeXml<LspDepartmentArray>(resp);
            Assert.NotNull(actual);
            Assert.AreEqual("johndoe", actual.NetworkID);
            Assert.AreEqual(2, actual.DeptCodeList.A.Count());
        }

        [TestCase(TestEntities.Departments.ParksName, new string[]{"johndoe"})]
        [TestCase(TestEntities.Departments.FireName, new string[]{"johndoe"})]
        [TestCase("  firE DePartMENT  ", new string[]{"johndoe"})]
        [TestCase(TestEntities.Departments.AuditorName, new string[0])]
        [TestCase("bad department", new string[0])]
        public async Task GetDepartmentLSPs(string department, string[] expectedDepartments)
        {
            var resp = await GetAnonymous($"LspdbWebService.svc/LspsInDept/{department}");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await DeserializeXml<LspContactArray>(resp);
            Assert.NotNull(actual);
            Assert.NotNull(actual.LspContacts);
            Assert.AreEqual(expectedDepartments.Length, actual.LspContacts.Count());
            var actualDepartments = actual.LspContacts.Select(c => c.NetworkID);
            CollectionAssert.AreEquivalent(expectedDepartments, actualDepartments);
        }

        private async Task<LspContact> GetParksLsps()
        {
            var resp = await GetAnonymous($"LspdbWebService.svc/LspsInDept/{TestEntities.Departments.ParksName}");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var arr = await DeserializeXml<LspContactArray>(resp);
            Assert.NotNull(arr);
            Assert.NotNull(arr.LspContacts);
            var expected = TestEntities.People.ServiceAdmin;
            return arr.LspContacts.SingleOrDefault(c => c.NetworkID == expected.Netid);
        }

        [Test]
        public async Task GetDepartmentLSPs_Properties()
        {
            var expected = TestEntities.People.ServiceAdmin;
            var actual = await GetParksLsps();
            Assert.AreEqual(expected.CampusPhone, actual.Phone);
            Assert.AreEqual(expected.CampusEmail, actual.Email);
            Assert.AreEqual(TestEntities.Units.CityOfPawnee.Email ?? expected.CampusEmail, actual.PreferredEmail);
            Assert.AreEqual(expected.Name, actual.FullName);
            Assert.AreEqual(TestEntities.Units.CityOfPawnee.Email, actual.GroupInternalEmail);
            Assert.True(actual.IsLSPAdmin);
        }

        [TestCase(null, TestEntities.People.ServiceAdminEmail)]
        [TestCase(TestEntities.Units.CityOfPawneeEmail, TestEntities.Units.CityOfPawneeEmail)]
        public async Task GetsCorrectPreferredEmailForUnitMembers(string unitEmail, string expectedEmail)
        {
            var db = Database.PeopleContext.Create(Database.PeopleContext.LocalDatabaseConnectionString);
            var parks = await db.Units.FindAsync(TestEntities.Units.CityOfPawneeUnitId);
            parks.Email = unitEmail;
            await db.SaveChangesAsync();

            var actual = await GetParksLsps();
            Assert.AreEqual(expectedEmail, actual.PreferredEmail);
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