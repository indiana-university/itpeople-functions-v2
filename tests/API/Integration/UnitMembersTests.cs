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
    }
}