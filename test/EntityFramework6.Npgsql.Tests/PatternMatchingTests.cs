using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using NpgsqlTypes;
using System.Text.RegularExpressions;

namespace EntityFramework6.Npgsql.Tests
{
    class PatternMatchingTests : EntityFrameworkTestBase
    {
        [Test]
        [TestCase("blog", "blog", "BLOG", TestName = "Case-sensitive")]
        [TestCase("^blog$", "blog", "some \nblog\n name", TestName = "^ and $ match beginning and end")]
        [TestCase("some .* name", "some blog name", "some \n name", TestName = ". matches all except \\n")]
        [TestCase("some blog name", "some blog name", "someblogname", TestName = "Whitespace not ignored in pattern")]
        public void MatchRegex(string pattern, string matchingInput, string mismatchingInput)
        {
            // Arrange
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                context.Blogs.Add(new Blog() { Name = matchingInput });
                context.Blogs.Add(new Blog() { Name = mismatchingInput });
                context.SaveChanges();
            }

            // Act
            // Ensure correctness of a test case
            var netMatchResult = Regex.IsMatch(matchingInput, pattern);
            var netMismatchResult = Regex.IsMatch(mismatchingInput, pattern);

            List<string> pgMatchResults;
            List<string> pgMismatchResults;
            List<string> pgMatchWithOptionsResults;
            List<string> pgMismatchWithOptionsResults;
            using (var context = new BloggingContext(ConnectionString))
            {
                pgMatchResults = (from b in context.Blogs
                                  where NpgsqlTextFunctions.MatchRegex(b.Name, pattern)
                                  select b.Name).ToList();

                pgMismatchResults = (from b in context.Blogs
                                     where !NpgsqlTextFunctions.MatchRegex(b.Name, pattern)
                                     select b.Name).ToList();

                pgMatchWithOptionsResults = (from b in context.Blogs
                                     where NpgsqlTextFunctions.MatchRegex(b.Name, pattern, RegexOptions.None)
                                     select b.Name).ToList();

                pgMismatchWithOptionsResults = (from b in context.Blogs
                                        where !NpgsqlTextFunctions.MatchRegex(b.Name, pattern, RegexOptions.None)
                                        select b.Name).ToList();
            }

            // Assert
            Assert.That(netMatchResult, Is.True);
            Assert.That(netMismatchResult, Is.False);

            Assert.That(pgMatchResults.Count, Is.EqualTo(1));
            Assert.That(pgMatchResults[0], Is.EqualTo(matchingInput));
            Assert.That(pgMismatchResults.Count, Is.EqualTo(1));
            Assert.That(pgMismatchResults[0], Is.EqualTo(mismatchingInput));

            Assert.That(pgMatchWithOptionsResults.Count, Is.EqualTo(1));
            Assert.That(pgMatchWithOptionsResults[0], Is.EqualTo(matchingInput));
            Assert.That(pgMismatchWithOptionsResults.Count, Is.EqualTo(1));
            Assert.That(pgMismatchWithOptionsResults[0], Is.EqualTo(mismatchingInput));
        }

        [Test]
        [TestCase(RegexOptions.IgnoreCase, "some", "SOME", "placeholder", TestName = "IgnoreCase")]
        [TestCase(RegexOptions.IgnorePatternWhitespace, "s o m e", "some", "s o m e", TestName = "IgnorePatternWhitespace")]
        [TestCase(RegexOptions.Multiline, "^blog$", "some \nblog\n name", "placeholder", TestName = "Multiline")]
        [TestCase(RegexOptions.Singleline, "some .* name", "some \n name", "placeholder", TestName = "Singleline")]
        public void MatchRegexOptions(RegexOptions options, string pattern, string matchingInput, string mismatchingInput)
        {
            // Arrange
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                context.Blogs.Add(new Blog() { Name = matchingInput });
                context.Blogs.Add(new Blog() { Name = mismatchingInput });
                context.SaveChanges();
            }

            // Act
            // Ensure correctness of a test case
            var netMatchResult = Regex.IsMatch(matchingInput, pattern, options);
            var netMismatchResult = Regex.IsMatch(mismatchingInput, pattern, options);

            List<string> pgMatchResults;
            List<string> pgMismatchResults;
            using (var context = new BloggingContext(ConnectionString))
            {
                pgMatchResults = (from b in context.Blogs
                                  where NpgsqlTextFunctions.MatchRegex(b.Name, pattern, options)
                                  select b.Name).ToList();

                pgMismatchResults = (from b in context.Blogs
                                     where !NpgsqlTextFunctions.MatchRegex(b.Name, pattern, options)
                                     select b.Name).ToList();
            }

            // Assert
            Assert.That(netMatchResult, Is.True);
            Assert.That(netMismatchResult, Is.False);

            Assert.That(pgMatchResults.Count, Is.EqualTo(1));
            Assert.That(pgMatchResults[0], Is.EqualTo(matchingInput));
            Assert.That(pgMismatchResults.Count, Is.EqualTo(1));
            Assert.That(pgMismatchResults[0], Is.EqualTo(mismatchingInput));
        }

        [Test]
        [TestCase(RegexOptions.RightToLeft)]
        [TestCase(RegexOptions.ECMAScript)]
        public void MatchRegex_NotSupportedOption(RegexOptions options)
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                Assert.That(() =>
                {
                    var results = (from b in context.Blogs
                                   where NpgsqlTextFunctions.MatchRegex(b.Name, "Some pattern", options)
                                   select b.Name).ToList();
                }, Throws.InnerException.TypeOf<NotSupportedException>());
            }
        }
    }
}
