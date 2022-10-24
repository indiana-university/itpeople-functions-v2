using NUnit.Framework;
using System.Collections.Generic;

using API.Functions;

namespace Unit
{
    public class PeopleSearchParametersTests
    {
        [TestCase(null, new string[0])]
        [TestCase("", new string[0])]
        [TestCase("  ", new string[0])]
        [TestCase(",,", new string[0])]
        [TestCase(" ,  ,", new string[0])]
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

    public class NotificationsParametersTests
    {
        [TestCase("1", true)]
        [TestCase("0", false)]
        [TestCase("", false)]
        [TestCase("true", true)]
        [TestCase("TRUE", true)]
        [TestCase("Yes", true)]
        [TestCase("Y", true)]
        [TestCase("no", false)]
        [TestCase("No", false)]
        [TestCase("N", false)]
        [TestCase("n", false)]
        public void CanParseBools(string query, bool expected)
        {
            var dict = new Dictionary<string,string> {{"showReviewed", query}};
            var result = NotificationsParameters.Parse(dict);

            Assert.AreEqual(expected, result.Value.ShowReviewed);
        }
    }
}