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
           
            [TestCase(Role.Leader, new int[]{ TestEntities.People.RSwansonId }, Description = "Return group leader(s)")]
            [TestCase(Role.Sublead, new int[]{ TestEntities.People.LKnopeId }, Description = "Return group Subleader(s)")]
            [TestCase(Role.Member, new int[]{ TestEntities.People.BWyattId }, Description = "Return group Member(s)")]
            public async Task CanSearchByRole(Role role, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?role={role.ToString()}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(expectedMatches.Length, actual.Count);
                Assert.AreEqual(expectedMatches, actual.Select(a => a.Id).ToArray());
            }
            [TestCase(UnitPermissions.Owner, new int[]{ TestEntities.People.RSwansonId })]
            [TestCase(UnitPermissions.Viewer, new int[]{ TestEntities.People.LKnopeId })]
            [TestCase(UnitPermissions.ManageMembers, new int[]{ TestEntities.People.BWyattId })]
            [TestCase(UnitPermissions.ManageTools, new int[0])]
            public async Task CanSearchByPermission(UnitPermissions permissions, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?permission={permissions.ToString()}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                Assert.AreEqual(expectedMatches.Length, actual.Count);
                Assert.AreEqual(expectedMatches, actual.Select(a => a.Id).ToArray());
            }
        }
    }
}