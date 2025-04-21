using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Integration
{
    public static class Assert3
    {
        public static void AreEqual(object expected, object actual, string message = null) => Assert.That(actual, Is.EqualTo(expected), message);
        public static void AreEquivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null) => Assert.That(actual, Is.EquivalentTo(expected), message);
        public static void AreNotEqual(object expected, object actual) => Assert.That(actual, Is.Not.EqualTo(expected));
        public static void IsTrue(object actual) => Assert.That(actual, Is.True);
        public static void IsFalse(object actual) => Assert.That(actual, Is.False);
        public static void NotNull(object actual, string message = null) => Assert.That(actual, Is.Not.Null, message);
        public static void IsNull(object actual) => Assert.That(actual, Is.Null);
        public static void NotZero(object actual) => Assert.That(actual, Is.Not.Zero);
        public static void Contains(object expected, IEnumerable<object> actual) => Assert.That(actual.Contains(expected), Is.True);
        public static void IsEmpty(IEnumerable<object> actual) => Assert.That(actual, Is.Empty);
        public static void LessOrEqual(object actual, object expected) => Assert.That(actual, Is.LessThanOrEqualTo(expected));
    }
}
