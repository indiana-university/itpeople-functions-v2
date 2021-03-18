using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using Models.Enums;
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
                Assert.AreEqual(4, actual.Count);
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
                AssertIdsMatchContent(expectedMatches, actual);
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
                AssertIdsMatchContent(expectedMatches, actual);
            }

            [TestCase("Pawnee", new int[]{TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.ServiceAdminId}, Description="full match of Pawnee")]
            [TestCase("Ind", new int[]{TestEntities.People.BWyattId}, Description="start of Indianapolis")]
            [TestCase("Indianapolis", new int[]{TestEntities.People.BWyattId}, Description="full match of Indianapolis")]
            [TestCase("Pawnee, Indian", new int[]{TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId, TestEntities.People.ServiceAdminId}, Description="multiple campus")]
            public async Task CanSearchCampus(string campusName, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?campus={campusName}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                AssertIdsMatchContent(expectedMatches, actual);
            }           
           
            [TestCase("Leader", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.ServiceAdminId }, Description = "Return group Leader(s)")]
            [TestCase("Sublead", new int[]{ TestEntities.People.LKnopeId }, Description = "Return group Subleader(s)")]
            [TestCase("Member", new int[]{ TestEntities.People.BWyattId }, Description = "Return group Member(s)")]
            [TestCase("member", new int[]{ TestEntities.People.BWyattId }, Description = "Return group Member(s) case-insensitive")]
            [TestCase("leader, member", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.BWyattId, TestEntities.People.ServiceAdminId }, Description = "Support list of roles")]
            public async Task CanSearchByRole(string roles, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?role={roles}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                AssertIdsMatchContent(expectedMatches, actual);
            }
            
            [TestCase("Owner", new int[]{ TestEntities.People.RSwansonId })]
            [TestCase("Viewer", new int[]{ TestEntities.People.LKnopeId })]
            [TestCase("ManageMembers", new int[]{ TestEntities.People.BWyattId, TestEntities.People.ServiceAdminId })]
            [TestCase("managemembers", new int[]{ TestEntities.People.BWyattId, TestEntities.People.ServiceAdminId }, Description = "Case insensitive match for Permissions.")]
            [TestCase("ManageTools", new int[0])]
            [TestCase("Viewer, ManageMembers", new int[]{ TestEntities.People.LKnopeId, TestEntities.People.BWyattId, TestEntities.People.ServiceAdminId }, Description = "Multiple Permissions provided.")]
            public async Task CanSearchByPermission(string permissions, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?permission={permissions}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                AssertIdsMatchContent(expectedMatches, actual);
            }
            [TestCase("UITS", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId, TestEntities.People.ServiceAdminId }, Description = "All people in UITS area")]
            [TestCase("uits", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId, TestEntities.People.ServiceAdminId})]
            [TestCase("edge", new int[0])]
            [TestCase("uits,edge", new int[]{ TestEntities.People.RSwansonId, TestEntities.People.LKnopeId, TestEntities.People.BWyattId, TestEntities.People.ServiceAdminId})]
            public async Task CanSearchByArea(string areas, int[] expectedMatches)
            {
                var resp = await GetAuthenticated($"people?area={areas}");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<Person>>();
                AssertIdsMatchContent(expectedMatches, actual);                
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

            [TestCase(ValidRswansonJwt, TestEntities.People.RSwansonId, PermsGroups.GetPut, Description="As Ron I can update my own record")]
            [TestCase(ValidRswansonJwt, TestEntities.People.LKnopeId, PermsGroups.GetPut, Description="As Ron I can update a person in a unit I manage")]
            [TestCase(ValidRswansonJwt, TestEntities.People.BWyattId, EntityPermissions.Get, Description="As Ron I cannot update a person in a unit I don't manage")]
            [TestCase(ValidRswansonJwt, TestEntities.People.ServiceAdminId, EntityPermissions.Get, Description="As Ron I cannot update a person in a unit I don't manage")]
            [TestCase(ValidAdminJwt, TestEntities.People.RSwansonId, PermsGroups.GetPut, Description="As a service admin I can update anyone")]
            [TestCase(ValidAdminJwt, TestEntities.People.LKnopeId, PermsGroups.GetPut, Description="As a service admin I can update anyone")]
            [TestCase(ValidAdminJwt, TestEntities.People.BWyattId, PermsGroups.GetPut, Description="As a service admin I can update anyone")]
            [TestCase(ValidAdminJwt, TestEntities.People.ServiceAdminId, PermsGroups.GetPut, Description="As a service admin I can update anyone")]
            public async Task ResponseHasCorrectXUserPermissionsHeader(string jwt, int personId, EntityPermissions expectedPermissions)
            {
                var resp = await GetAuthenticated($"people/{personId}", jwt);
                AssertPermissions(resp, expectedPermissions);
            }
        }

        public class GetMemberships : ApiTest
        {
            [TestCase(TestEntities.People.RSwansonId, HttpStatusCode.OK)]
            [TestCase(9999, HttpStatusCode.NotFound)]
            public async Task CanGetMemberships(int id, HttpStatusCode expectedStatus)
            {
                var resp = await GetAuthenticated($"people/{id}/memberships");
                AssertStatusCode(resp, expectedStatus);
            }

            [Test]
            public async Task GetRonsMemberships()
            {
                var resp = await GetAuthenticated($"people/{TestEntities.People.RSwansonId}/memberships");
                AssertStatusCode(resp, HttpStatusCode.OK);
                var actual = await resp.Content.ReadAsAsync<List<UnitMember>>();
                var expected = TestEntities.UnitMembers.RSwansonDirector;
                Assert.That(actual.Count, Is.EqualTo(1));
                Assert.That(actual.First().Id, Is.EqualTo(expected.Id));
            }
        }

        public class UpdatePerson : ApiTest
        {
            private static readonly PersonUpdateRequest TestUpdateRequest = new PersonUpdateRequest
            { 
                Location = "Timbuktu", 
                Expertise = "Woodworking; Honor; Managering",
                PhotoUrl = "http://flavorwire.files.wordpress.com/2011/11/ron-swanson-NEW.jpg",
                Responsibilities = Responsibilities.ItLeadership & Responsibilities.ItProjectMgt,
            };

            [TestCase(TestEntities.People.RSwansonId, ValidRswansonJwt, Description="I can update my own record")]
            [TestCase(TestEntities.People.LKnopeId, ValidRswansonJwt, Description="I can update a person in a unit I manage")]
            [TestCase(TestEntities.People.RSwansonId, ValidAdminJwt, Description="Service Admin can edit any person")]
            public async Task AuthorizedUserCanUpdatePerson(int personId, string jwt)
            {
                //Make a request as a Unit Owner

                //Update the user
                var resp = await PutAuthenticated($"people/{personId}", TestUpdateRequest, jwt);
                AssertStatusCode(resp, HttpStatusCode.OK);

                //verify the changes
                var actual = await resp.Content.ReadAsAsync<Person>();
                Assert.That(actual.Id, Is.EqualTo(personId));
                Assert.That(actual.Location, Is.EqualTo(TestUpdateRequest.Location));
                Assert.That(actual.Expertise, Is.EqualTo(TestUpdateRequest.Expertise));
                Assert.That(actual.PhotoUrl, Is.EqualTo(TestUpdateRequest.PhotoUrl));
                Assert.That(actual.Responsibilities, Is.EqualTo(TestUpdateRequest.Responsibilities));
            }

            [Test]
            public async Task UnauthorzedUserCannotUpdatePerson()
            {
                // Ben belongs to a unit that Ron doesn't manage. Ron shouln't be able to modify Ben's record.
                var resp = await PutAuthenticated($"people/{TestEntities.People.BWyattId}", TestUpdateRequest);
                AssertStatusCode(resp, HttpStatusCode.Forbidden);
            }
        }
    }
}