using System.Net;
using System.Threading.Tasks;
using API.Middleware;
using NUnit.Framework;

namespace Integration
{
    public class AuthenticationTests : ApiTest
    {
        private static string ValidJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjE2MTA1NTM1MDcsInVzZXJfbmFtZSI6Impob2VyciIsImV4cCI6MTYxMDU5NjcwN30.bCi4vf3FDrtApp0eUFvzqpvxEtfYoZwesJuKhrLM4915j3OZ2Hgwz9805uIltoSCctTdA-NAPDxLwv1fRHG_-ot5Hk04dZT5dfo_EhSnFLv-VPTTPUDgVDHrKjYNKGekKFXljqPBSNfxEisq4iDfQdcYqqHKBaXf4pZDs1mzw8yvXPlJYBoF6ne6uOhhgnuuYQR0alByrKbHddItlao_mGG1moMKMEQxsaDsOQCy4Mv95Ds1isTPul5kydOSFkKLr264euUA_MN0XQ-OEWYv04auR2MFw13Du0_XPkizvfobWyh4qQxe91_W6iaMgPd-lG9F2eQyrIz6cdfoI8RgQg";

        [Test]
        public async Task AuthorizationHeaderRequired()
        {
            var resp = await Http.GetAsync("people");
            AssertStatusCode(resp, HttpStatusCode.BadRequest);
            await AssertStringContent(resp, Security.ErrorRequestMissingAuthorizationHeader);
        }

        // Returns 401 when auth token not present, malformed, expired
        

    }

    public class HealthcheckTests : ApiTest
    {
        [Test]
        public async Task Ping()
        {
            var resp = await Http.GetAsync("ping");
            AssertStatusCode(resp, HttpStatusCode.OK);
            await AssertStringContent(resp, "Pong!");
        }
    }
}