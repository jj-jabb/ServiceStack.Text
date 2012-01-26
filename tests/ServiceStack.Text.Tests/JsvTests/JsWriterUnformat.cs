using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests.JsvTests
{
    [TestFixture]
    public class JsWriterUnformat
    {
        [Test]
        public void Can_unformat_from_formatted_jsv()
        {
            var original = TypeSerializer.SerializeToString(MoviesData.Movies);
            var expected = MoviesData.Movies.Dump();
            var unformatted = JsvFormatter.UnFormat(expected);
            var movies = TypeSerializer.DeserializeFromString<List<Movie>>(unformatted);

            var actual = movies.Dump();
            Assert.AreEqual(expected, actual);
        }
    }
}
