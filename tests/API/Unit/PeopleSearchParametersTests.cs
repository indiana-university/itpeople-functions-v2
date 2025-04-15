using NUnit.Framework;
using System.Collections.Generic;

using API.Functions;

namespace Unit
{
    public static class Assert3
    {
        public static void AreEqual(object expected, object actual) => Assert.That(actual, Is.EqualTo(expected));
        public static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string? message) => Assert.That(actual, Is.EquivalentTo(expected), message);
        public static void AreNotEqual(object expected, object actual) => Assert.That(actual, Is.Not.EqualTo(expected));
        public static void IsTrue(object actual) => Assert.That(actual, Is.True);
        public static void NotNull(object actual) => Assert.That(actual, Is.Not.Null);
        public static void IsNull(object actual) => Assert.That(actual, Is.Null);
        public static void NotZero(object actual) => Assert.That(actual, Is.Not.Zero);
        public static void IsEmpty(IEnumerable<object> actual) => Assert.That(actual, Is.Empty);
    }

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
            var dict = new Dictionary<string,Microsoft.Extensions.Primitives.StringValues> {{"interest", query}};
            var result = PeopleSearchParameters.Parse(dict);
            Assert3.IsTrue(result.IsSuccess);
            Assert3.AreEqual(expected, result.Value.Expertise);
        }
    }
}