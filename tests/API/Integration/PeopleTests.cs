using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Integration
{
    public class PeopleTests
    {
        public class GetAll : ApiTest
        {
            [Test]
            public async Task HasCorrectNumber()
            {
                var resp = await GetAuthenticated("people");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(3, actual.Count);
            }

            [TestCase("rswanso", Description="Exact match of netid")]
            [TestCase("RSWANSO", Description="Search is case-insensitive")]
            [TestCase("rSwaN", Description="Partial netid match")]
            public async Task CanSearchByNetid(string netid)
            {
                var resp = await GetAuthenticated($"people?q={netid}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual(TestEntities.People.RSwanson.Id, actual.Single().Id);
            }

            [TestCase("Ron", Description="Name match")]
            [TestCase("Ro", Description="Partial name match")]
            public async Task CanSearchByName(string name)
            {
                var resp = await GetAuthenticated($"people?q={name}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual(TestEntities.People.RSwanson.Id, actual.Single().Id);
                Assert.AreEqual(TestEntities.People.RSwanson.Name, actual.Single().Name);
            }

            [TestCase(
                Responsibilities.ItLeadership, 
                new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId })]
            [TestCase(
                Responsibilities.ItProjectMgt, 
                new int[]{ TestEntities.People.LKnopeId, TestEntities.People.BWyattId })]
            [TestCase(
                Responsibilities.ItLeadership | Responsibilities.ItProjectMgt, 
                new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId })]
            [TestCase(
                Responsibilities.BizSysAnalysis,
                new int[0])]
            [TestCase(
                Responsibilities.BizSysAnalysis | Responsibilities.ItLeadership,
                new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId })]
            public async Task CanSearchByJobClass(Responsibilities jobClass, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?class={jobClass.ToString()}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(expectedMatches.Length, actual.Count);
                Assert.AreEqual(expectedMatches, actual.Select(a => a.Id).ToArray());
            }

            [TestCase("programming", new int[0])]
            [TestCase("Woodworking; Honor", new int[]{TestEntities.People.RSwansonId}, Description="exact match")]
            [TestCase("woodworking; honor", new int[]{TestEntities.People.RSwansonId}, Description="exact match case-insensitive")]
            [TestCase("woodworking", new int[]{TestEntities.People.RSwansonId})]
            [TestCase("working", new int[]{TestEntities.People.RSwansonId})]
            [TestCase("wood", new int[]{TestEntities.People.RSwansonId})]
            [TestCase("programming, woodworking", new int[]{TestEntities.People.RSwansonId})]
            [TestCase("woodworking, waffles", new int[]{TestEntities.People.RSwansonId, TestEntities.People.LKnopeId})]
            [TestCase("woOdworKing, waFFlEs", new int[]{TestEntities.People.RSwansonId, TestEntities.People.LKnopeId})]
            public async Task CanSearchByInterest(string interest, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?interest={interest}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(expectedMatches.Length, actual.Count);
                Assert.AreEqual(expectedMatches, actual.Select(a => a.Id).ToArray());
            }

            [TestCase("Pawnee", new int[]{TestEntities.People.RSwansonId, TestEntities.People.LKnopeId}, Description="full match of Pawnee")]
            [TestCase("Ind", new int[]{TestEntities.People.BWyattId}, Description="start of Indianapolis")]
            [TestCase("Indianapolis", new int[]{TestEntities.People.BWyattId}, Description="full match of Indianapolis")]
            [TestCase("Pawnee, Indian", new int[]{TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId}, Description="multiple campus")]
            public async Task CanSearchCampus(string campusName, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?campus={campusName}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(expectedMatches.Length, actual.Count);
                Assert.AreEqual(expectedMatches, actual.Select(a => a.Id).ToArray());
            }           
           
            [TestCase("Leader", new int[]{ TestEntities.People.RSwansonId }, Description = "Return group Leader(s)")]
            [TestCase("Sublead", new int[]{ TestEntities.People.LKnopeId }, Description = "Return group Subleader(s)")]
            [TestCase("Member", new int[]{ TestEntities.People.BWyattId }, Description = "Return group Member(s)")]
            [TestCase("member", new int[]{ TestEntities.People.BWyattId }, Description = "Return group Member(s) case-insensitive")]
            [TestCase("leader, member", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.BWyattId }, Description = "Support list of roles")]
            public async Task CanSearchByRole(string roles, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?role={roles}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(expectedMatches.Length, actual.Count);
                Assert.AreEqual(expectedMatches, actual.Select(a => a.Id).ToArray());
            }
            
            [TestCase("Owner", new int[]{ TestEntities.People.RSwansonId })]
            [TestCase("Viewer", new int[]{ TestEntities.People.LKnopeId })]
            [TestCase("ManageMembers", new int[]{ TestEntities.People.BWyattId })]
            [TestCase("managemembers", new int[]{ TestEntities.People.BWyattId }, Description = "Case insensitive match for Permissions.")]
            [TestCase("ManageTools", new int[0])]
            [TestCase("Viewer, ManageMembers", new int[]{ TestEntities.People.LKnopeId, TestEntities.People.BWyattId }, Description = "Multiple Permissions provided.")]
            public async Task CanSearchByPermission(string permissions, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?permission={permissions}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(expectedMatches.Length, actual.Count);
                Assert.AreEqual(expectedMatches, actual.Select(a => a.Id).ToArray());
            }
            [TestCase("UITS", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId }, Description = "All people in UITS area")]
            [TestCase("uits", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId})]
            [TestCase("edge", new int[0])]
            [TestCase("uits,edge", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId})]
            public async Task CanSearchByArea(string areas, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?area={areas}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(expectedMatches.Length, actual.Count);
                Assert.AreEqual(expectedMatches, actual.Select(a => a.Id).ToArray());
                
            }
        }

        public class GetOne : ApiTest
        {
            [TestCase(TestEntities.People.RSwansonId, HttpStatusCode.OK)]
            [TestCase(9999, HttpStatusCode.NotFound)]
            public async Task HasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
            {
                var resp = await GetAuthenticated($"people/{id}");
                AssertStatusCode(resp, expectedStatus);
            }

            [Test]
            public async Task ResponseHasCorrectPersonShape()
            {
                var resp = await GetAuthenticated($"people/{TestEntities.People.RSwansonId}");
                var actual = await resp.Content.ReadAsAsync<Person>();
                var expected = TestEntities.People.RSwanson;
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Netid, actual.Netid);
                Assert.AreEqual(expected.DepartmentId, actual.DepartmentId);
            }

            [Test]
            public async Task ResponseIncludesDepartment()
            {
                var resp = await GetAuthenticated($"people/{TestEntities.People.RSwansonId}");
                var actual = await resp.Content.ReadAsAsync<Person>();
                var expected = TestEntities.People.RSwanson.Department;
                Assert.IsNotNull(actual.Department);
                Assert.AreEqual(expected.Id, actual.Department.Id);
                Assert.AreEqual(expected.Name, actual.Department.Name);
            }
        }
    }
}