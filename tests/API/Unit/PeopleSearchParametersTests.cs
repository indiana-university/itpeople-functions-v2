using NUnit.Framework;
using System.Collections.Generic;

using API.Functions;

namespace Unit
{
    public class PeopleSearchParametersTests
    {
        [TestCase(null, new string[0])]
        [TestCase("", new string[0])]
        [TestCase(",,", new string[0])]
        [TestCase("foo", new[]{"foo"})]
        [TestCase("foo,bar", new[]{"foo","bar"})]
        [TestCase("foo,,bar", new[]{"foo","bar"})]
        public void CanParseInterests(string query, string[] expected)
        {
            var dict = new Dictionary<string,string> {{"interest", query}};
            var result = PeopleSearchParameters.Parse(dict);
            Assert.True(result.IsSuccess);
            Assert.AreEqual(expected, result.Value.Expertise);
        }
    }
}