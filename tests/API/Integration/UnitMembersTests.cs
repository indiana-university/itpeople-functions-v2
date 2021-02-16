using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NUnit.Framework;

namespace Integration
{
    [TestFixture]
    [Category("UnitMembers")]
    public class UnitMembersTests : ApiTest
    {
        [Test]
        public async Task FetchesAllUnitMembers()
        {
            var resp = await GetAuthenticated("memberships");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await resp.Content.ReadAsAsync<List<UnitMemberResponse>>();
            var expectedIds = new []{
                TestEntities.UnitMembers.RSwansonLeaderId,
                TestEntities.UnitMembers.LkNopeSubleadId,
                TestEntities.UnitMembers.BWyattMemberId,
            };
            AssertIdsMatchContent(expectedIds, actual);
        }

        [Test]
        public async Task FetchAllHasExpectedRelations()
        {
            var resp = await GetAuthenticated("memberships");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await resp.Content.ReadAsAsync<List<UnitMemberResponse>>();
            var ron = actual.SingleOrDefault(a => a.Id == TestEntities.UnitMembers.RSwansonLeaderId);
            Assert.NotNull(ron.Person);
            Assert.NotNull(ron.Unit);
            Assert.NotNull(ron.Unit.Parent);
            Assert.NotNull(ron.MemberTools);
        }

        [TestCase(TestEntities.UnitMembers.RSwansonLeaderId, HttpStatusCode.OK)]
        [TestCase(9999, HttpStatusCode.NotFound)]
        public async Task HasCorrectStatusCode(int id, HttpStatusCode expectedStatus)
        {
            var resp = await GetAuthenticated($"memberships/{id}");
            AssertStatusCode(resp, expectedStatus);
        }

        [Test]
        public async Task ResponseHasCorrectShape()
        {
            var resp = await GetAuthenticated($"memberships/{TestEntities.UnitMembers.RSwansonLeaderId}");
            AssertStatusCode(resp, HttpStatusCode.OK);
            var actual = await resp.Content.ReadAsAsync<UnitMemberResponse>();
            var expected = TestEntities.UnitMembers.RSwansonDirector;
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.UnitId, actual.UnitId);
            Assert.AreEqual(expected.Role, actual.Role);
            Assert.AreEqual(expected.Permissions, actual.Permissions);
            Assert.AreEqual(expected.PersonId, actual.PersonId);
            Assert.AreEqual(expected.Title, actual.Title);
            Assert.AreEqual(expected.Percentage, actual.Percentage);
            Assert.AreEqual(expected.Notes, actual.Notes);
            // relations
            Assert.NotNull(actual.Person);
            Assert.AreEqual(expected.Person.Id, actual.Person.Id);
            Assert.NotNull(actual.Unit);
            Assert.AreEqual(expected.Unit.Id, actual.Unit.Id);
            Assert.NotNull(actual.MemberTools);
        }
    }
}