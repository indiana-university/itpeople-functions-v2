using NUnit.Framework;
using System.Collections.Generic;

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
}