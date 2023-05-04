using API.Middleware;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;

namespace Unit
{
    public class SecurityTests
    {
        
        [SetUp]
        public void BeforeEach(){
            // the hard-coded logger wants a DB connection string
            // set as an environment variable. This provides that.
            Environment.SetEnvironmentVariable("DatabaseConnectionString", "invalid");
        }

        // note: ranges contains spaces and ipv6 ranges, but still should work ok
        private const string AddressRanges = "127.0.0.1/8, 149.159.0.0/16, 2001:4860:4860::8888/32, 145.78.27.254/32, 145.78.27.45/32";

        [TestCase("149.159.10.10")]
        [TestCase("127.0.0.1")]
        [TestCase("2001:4860:4860:0000:0000:0000:0000:8888")]
        [TestCase("145.78.27.254")]
        [TestCase("145.78.27.45")]
        public void WhenAddressIsInRange(string ipAddress)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
            Environment.SetEnvironmentVariable("LspFuncsAllowedIpRanges", AddressRanges);

            var result = Security.ValidateIpAddress(context.Request);

            Assert.That(result.IsSuccess, Is.True);
        }

        [TestCase("188.159.10.10")]
        [TestCase("10.0.10.10")]
        [TestCase("2002:4860:4860:0000:0000:0000:0000:8888")]
        [TestCase("145.78.27.253")]
        [TestCase("145.78.27.46")]
        public void WhenAddressIsNotInRange(string ipAddress)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
            Environment.SetEnvironmentVariable("LspFuncsAllowedIpRanges", AddressRanges);

            var result = Security.ValidateIpAddress(context.Request);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(result.Error.Messages.First(),
                Is.EqualTo($"The ip address {context.Connection.RemoteIpAddress} is not allowed to access this resource."));
        }

        [TestCase("149.159.10.10")]
        [TestCase("127.0.0.1")]
        [TestCase("2001:4860:4860:0000:0000:0000:0000:8888")]
        public void WhenAddressRangeNotSet(string ipAddress)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
            Environment.SetEnvironmentVariable("LspFuncsAllowedIpRanges", null);

            var result = Security.ValidateIpAddress(context.Request);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(result.Error.Messages.First(),
                Is.EqualTo($"The ip address {context.Connection.RemoteIpAddress} is not allowed to access this resource."));

        }

        [Test]
        public void WhenAddressRangeContainsInvalidEntry() {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("149.159.10.10");

            // note: second entry is invalid
            Environment.SetEnvironmentVariable("LspFuncsAllowedIpRanges", "127.0.0.1/8,149.1590.0/16");

            var result = Security.ValidateIpAddress(context.Request);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(result.Error.Messages.First(),
                Is.EqualTo($"The ip address {context.Connection.RemoteIpAddress} is not allowed to access this resource."));
        }

        // testing logic when range string contains single entry
        [Test]
        public void WhenAddressRangeContainsSingleEntry()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

            // note: second entry is invalid
            Environment.SetEnvironmentVariable("LspFuncsAllowedIpRanges", "127.0.0.1/8");

            var result = Security.ValidateIpAddress(context.Request);

            Assert.That(result.IsSuccess, Is.True);
        }


    }
}
