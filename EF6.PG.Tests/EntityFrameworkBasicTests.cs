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
using System.Diagnostics.CodeAnalysis;
using NpgsqlTypes;

namespace EntityFramework6.Npgsql.Tests
{
    public class EntityFrameworkBasicTests : EntityFrameworkTestBase
    {
        [Test]
        public void InsertAndSelect()
        {
            var varbitVal = "10011";

            using (var context = new BloggingContext(ConnectionString))
            {
                var blog = new Blog()
                {
                    Name = "Some blog name"
                };
                blog.Posts = new List<Post>();
                for (int i = 0; i < 5; i++)
                    blog.Posts.Add(new Post()
                    {
                        Content = "Some post content " + i,
                        Rating = (byte)i,
                        Title = "Some post Title " + i,
                        VarbitColumn = varbitVal
                    });
                context.Blogs.Add(blog);
                context.NoColumnsEntities.Add(new NoColumnsEntity());
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                var posts = from p in context.Posts
                            select p;
                Assert.AreEqual(5, posts.Count());
                foreach (var post in posts)
                {
                    StringAssert.StartsWith("Some post Title ", post.Title);
                    Assert.AreEqual(varbitVal, post.VarbitColumn);
                }
                var someParameter = "Some";
                Assert.IsTrue(context.Posts.Any(p => p.Title.StartsWith(someParameter)));
                Assert.IsTrue(context.Posts.All(p => p.Title != null));
                Assert.IsTrue(context.Posts.Any(p => someParameter != null));
                Assert.IsTrue(context.Posts.Select(p => p.VarbitColumn == varbitVal).First());
                Assert.IsTrue(context.Posts.All(p => p.VarbitColumn != null));
                Assert.IsTrue(context.Posts.Any(p => varbitVal != null));
                Assert.IsTrue(context.Posts.Select(p => p.VarbitColumn == "10011").First());
                Assert.AreEqual(1, context.NoColumnsEntities.Count());
            }
        }

        [Test]
        public void InsertAndSelectSchemaless()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.NoColumnsEntities.Add(new NoColumnsEntity());
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                Assert.AreEqual(1, context.NoColumnsEntities.Count());
            }
        }

        [Test]
        public void SelectWithWhere()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                var blog = new Blog()
                {
                    Name = "Some blog name"
                };
                blog.Posts = new List<Post>();
                for (int i = 0; i < 5; i++)
                    blog.Posts.Add(new Post()
                    {
                        Content = "Some post content " + i,
                        Rating = (byte)i,
                        Title = "Some post Title " + i
                    });
                context.Blogs.Add(blog);
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                var posts = from p in context.Posts
                            where p.Rating < 3
                            select p;
                Assert.AreEqual(3, posts.Count());
                foreach (var post in posts)
                {
                    Assert.Less(post.Rating, 3);
                }
            }
        }

        [Test]
        public void SelectWithWhere_Ef_TruncateTime()
        {
            DateTime createdOnDate = new DateTime(2014, 05, 08);
            using (var context = new BloggingContext(ConnectionString))
            {
                var blog = new Blog()
                {
                    Name = "Some blog name"
                };
                blog.Posts = new List<Post>();

                for (int i = 0; i < 5; i++)
                    blog.Posts.Add(new Post()
                    {
                        Content = "Some post content " + i,
                        Rating = (byte)i,
                        Title = "Some post Title " + i,
                        CreationDate = createdOnDate.AddHours(i)
                    });
                context.Blogs.Add(blog);
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                var posts = from p in context.Posts
                            let datePosted = DbFunctions.TruncateTime(p.CreationDate)
                            where p.Rating < 3 && datePosted == createdOnDate
                            select p;
                Assert.AreEqual(3, posts.Count());
                foreach (var post in posts)
                {
                    Assert.Less(post.Rating, 3);
                }
            }
        }

        [Test]
        public void Select_Ef_Timezone()
        {
            var createdOnDate = new DateTimeOffset(2020, 12, 03, 22, 23, 0, TimeSpan.Zero);
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Logs.Add(new Log()
                {
                    CreationDate = createdOnDate
                });
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.ExecuteSqlCommand("SET TIMEZONE='UTC';");
                var query = context.Logs.Select(p => NpgsqlDateTimeFunctions.Timezone("Pacific/Honolulu", p.CreationDate));
                var createdOnDateInTimeZone = query.FirstOrDefault();
                Assert.AreEqual(new DateTime(2020, 12, 03, 12, 23, 0), createdOnDateInTimeZone);
            }
        }

        [Test]
        public void Select_Ef_StringAgg()
        {
            DateTime createdOnDate = new DateTime(2014, 05, 08);
            using (var context = new BloggingContext(ConnectionString))
            {
                var blog = new Blog()
                {
                    Name = "Blog 1"
                };
                blog.Posts = new List<Post>();

                blog.Posts.Add(new Post()
                {
                    Content = "Content 1",
                    Rating = 1,
                    Title = "Title 1",
                    CreationDate = createdOnDate
                });
                blog.Posts.Add(new Post()
                {
                    Content = "Content 2",
                    Rating = 2,
                    Title = "Title 2",
                    CreationDate = createdOnDate
                });
                blog.Posts.Add(new Post()
                {
                    Content = "Content 3",
                    Rating = 3,
                    Title = "Title 3",
                    CreationDate = createdOnDate
                });

                context.Blogs.Add(blog);
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Initialize(true);
                var query = context.Posts
                    .GroupBy(p => p.BlogId)
                    .Select(g => g.Select(x => x.Title).StringAgg());
                var result = query.FirstOrDefault();
                Assert.AreEqual("Title 1, Title 2, Title 3", result);
            }
        }

		[Test]
		public void SelectWithLike_SpecialCharacters()
		{
			DateTime createdOnDate = new DateTime(2014, 05, 08);
			using (var context = new BloggingContext(ConnectionString))
			{
				var blog = new Blog()
				{
					Name = "Special Characters Test"
				};
				blog.Posts = new List<Post>();

				blog.Posts.Add(new Post()
				{
					Content = "C:\\blog\\Some_post_title%",
					Rating = (byte)1,
					Title = "Some post Title ",
					CreationDate = createdOnDate.AddHours(1)
				});
				blog.Posts.Add(new Post()
				{
					Content = "C:\\blog\\Some_post_title\\",
					Rating = (byte)2,
					Title = "Some post Title ",
					CreationDate = createdOnDate.AddHours(2)
				});
				blog.Posts.Add(new Post()
				{
					Content = "%Test",
					Rating = (byte)3,
					Title = "Some post Title ",
					CreationDate = createdOnDate.AddHours(3)
				});
				context.Blogs.Add(blog);
				context.SaveChanges();
			}

			using (var context = new BloggingContext(ConnectionString))
			{
				var posts1 = from p in context.Posts
				             where p.Content.Contains("_")
				             select p;
				Assert.AreEqual(2, posts1.Count());

				var posts2 = from p in context.Posts
				             where p.Content.EndsWith("\\")
				             select p;
				Assert.AreEqual(1, posts2.Count());

				var posts3 = from p in context.Posts
				             where p.Content.StartsWith("%")
				             select p;
				Assert.AreEqual(1, posts3.Count());
			}
		}

        [Test]
        public void OrderBy()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                Random random = new Random();
                var blog = new Blog()
                {
                    Name = "Some blog name"
                };

                blog.Posts = new List<Post>();
                for (int i = 0; i < 10; i++)
                    blog.Posts.Add(new Post()
                    {
                        Content = "Some post content " + i,
                        Rating = (byte)random.Next(0, 255),
                        Title = "Some post Title " + i
                    });
                context.Blogs.Add(blog);
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                var posts = from p in context.Posts
                            orderby p.Rating
                            select p;
                Assert.AreEqual(10, posts.Count());
                byte previousValue = 0;
                foreach (var post in posts)
                {
                    Assert.GreaterOrEqual(post.Rating, previousValue);
                    previousValue = post.Rating;
                }
            }
        }

        [Test]
        public void OrderByThenBy()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                Random random = new Random();
                var blog = new Blog()
                {
                    Name = "Some blog name"
                };

                blog.Posts = new List<Post>();
                for (int i = 0; i < 10; i++)
                    blog.Posts.Add(new Post()
                    {
                        Content = "Some post content " + i,
                        Rating = (byte)random.Next(0, 255),
                        Title = "Some post Title " + (i % 3)
                    });
                context.Blogs.Add(blog);
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                var posts = context.Posts.AsQueryable<Post>().OrderBy((p) => p.Title).ThenByDescending((p) => p.Rating);
                Assert.AreEqual(10, posts.Count());
                foreach (var post in posts)
                {
                    //TODO: Check outcome
                    Console.WriteLine(post.Title + " " + post.Rating);
                }
            }
        }

        [Test]
        public void TestComputedValue()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                var blog = new Blog()
                {
                    Name = "Some blog name"
                };

                context.Blogs.Add(blog);
                context.SaveChanges();

                Assert.Greater(blog.BlogId, 0);
                Assert.Greater(blog.IntComputedValue, 0);
            }

        }

        [Test]
        public void Operators()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                int one = 1, two = 2, three = 3, four = 4;
                bool True = true, False = false;
                bool[] boolArr = { true, false };
                IQueryable<int> oneRow = context.Posts.Where(p => false).Select(p => 1).Concat(new int[] { 1 });
                Assert.AreEqual(oneRow.Select(p => one & (two ^ three)).First(), 1);
                Assert.AreEqual(oneRow.Select(p => ~(one & two)).First(), ~(one & two));
                Assert.AreEqual(oneRow.Select(p => one + ~(two * three) + ~(two ^ ~three) - one ^ three * ~two / three | four).First(),
                                                   one + ~(two * three) + ~(two ^ ~three) - one ^ three * ~two / three | four);
                Assert.AreEqual(oneRow.Select(p => one - (two - three) - four - (- one - two) - (- three)).First(),
                                                   one - (two - three) - four - (- one - two) - (- three));
                Assert.AreEqual(oneRow.Select(p => one <= (one & one)).First(),
                                                   one <= (one & one));
                Assert.AreEqual(oneRow.Select(p => boolArr.Contains(True == true)).First(), true);
                Assert.AreEqual(oneRow.Select(p => !boolArr.Contains(False == true)).First(), false);
                Assert.AreEqual(oneRow.Select(p => !boolArr.Contains(False != true)).First(), false);
            }
        }

        [Test]
        public void DataTypes()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                IQueryable<int> oneRow = context.Posts.Where(p => false).Select(p => 1).Concat(new int[] { 1 });

                Assert.AreEqual((byte)1, oneRow.Select(p => (byte)1).First());
                Assert.AreEqual((short)1, oneRow.Select(p => (short)1).First());
                Assert.AreEqual((long)1, oneRow.Select(p => (long)1).First());
                Assert.AreEqual(1.25M, oneRow.Select(p => 1.25M).First());
                Assert.AreEqual(double.NaN, oneRow.Select(p => double.NaN).First());
                Assert.AreEqual(double.PositiveInfinity, oneRow.Select(p => double.PositiveInfinity).First());
                Assert.AreEqual(double.NegativeInfinity, oneRow.Select(p => double.NegativeInfinity).First());
                Assert.AreEqual(1.12e+12, oneRow.Select(p => 1.12e+12).First());
                Assert.AreEqual(1.12e-12, oneRow.Select(p => 1.12e-12).First());
                Assert.AreEqual(float.NaN, oneRow.Select(p => float.NaN).First());
                Assert.AreEqual(float.PositiveInfinity, oneRow.Select(p => float.PositiveInfinity).First());
                Assert.AreEqual(float.NegativeInfinity, oneRow.Select(p => float.NegativeInfinity).First());
                Assert.AreEqual(1.12e+12f, oneRow.Select(p => 1.12e+12f).First());
                Assert.AreEqual(1.12e-12f, oneRow.Select(p => 1.12e-12f).First());
                Assert.AreEqual((short)-32768, oneRow.Select(p => (short)-32768).First());
                Assert.IsTrue(new byte[] { 1, 2 }.SequenceEqual(oneRow.Select(p => new byte[] { 1, 2 }).First()));

                byte byteVal = 1;
                short shortVal = -32768;
                long longVal = 1L << 33;
                decimal decimalVal = 1.25M;
                double doubleVal = 1.12;
                float floatVal = 1.22f;
                byte[] byteArrVal = new byte[] { 1, 2 };

                Assert.AreEqual(byteVal, oneRow.Select(p => byteVal).First());
                Assert.AreEqual(shortVal, oneRow.Select(p => shortVal).First());
                Assert.AreEqual(longVal, oneRow.Select(p => longVal).First());
                Assert.AreEqual(decimalVal, oneRow.Select(p => decimalVal).First());
                Assert.AreEqual(doubleVal, oneRow.Select(p => doubleVal).First());
                Assert.AreEqual(floatVal, oneRow.Select(p => floatVal).First());
                Assert.IsTrue(byteArrVal.SequenceEqual(oneRow.Select(p => byteArrVal).First()));

                // A literal TimeSpan is written as an interval
                Assert.AreEqual(new TimeSpan(1, 2, 3, 4), oneRow.Select(p => new TimeSpan(1, 2, 3, 4)).First());
                var val1 = new TimeSpan(1, 2, 3, 4);
                Assert.AreEqual(val1, oneRow.Select(p => new TimeSpan(1, 2, 3, 4)).First());
                Assert.AreEqual(val1, oneRow.Select(p => val1).First());

                // DateTimeOffset -> timestamptz
                Assert.AreEqual(new DateTimeOffset(2014, 2, 3, 4, 5, 6, 0, TimeSpan.Zero), oneRow.Select(p => new DateTimeOffset(2014, 2, 3, 4, 5, 6, 0, TimeSpan.Zero)).First());
                var val2 = new DateTimeOffset(2014, 2, 3, 4, 5, 6, 0, TimeSpan.Zero);
                Assert.AreEqual(val2, oneRow.Select(p => new DateTimeOffset(2014, 2, 3, 4, 5, 6, 0, TimeSpan.Zero)).First());
                Assert.AreEqual(val2, oneRow.Select(p => val2).First());

                // DateTime -> timestamp
                Assert.AreEqual(new DateTime(2014, 2, 3, 4, 5, 6, 0), oneRow.Select(p => new DateTime(2014, 2, 3, 4, 5, 6, 0)).First());
                var val3 = new DateTime(2014, 2, 3, 4, 5, 6, 0);
                Assert.AreEqual(val3, oneRow.Select(p => new DateTime(2014, 2, 3, 4, 5, 6, 0)).First());
                Assert.AreEqual(val3, oneRow.Select(p => val3).First());

                var val4 = new Guid("1234567890abcdef1122334455667788");
                Assert.AreEqual(val4, oneRow.Select(p => new Guid("1234567890abcdef1122334455667788")).First());
                Assert.AreEqual(val4, oneRow.Select(p => val4).First());

                // String
                Assert.AreEqual(@"a'b\c", oneRow.Select(p => @"a'b\c").First());
            }
        }

        [Test]
        public void SByteTest()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                IQueryable<int> oneRow = context.Posts.Where(p => false).Select(p => 1).Concat(new int[] { 1 });

                sbyte sbyteVal = -1;
                Assert.AreEqual(sbyteVal, oneRow.Select(p => sbyteVal).First());
                Assert.AreEqual((sbyte)1, oneRow.Select(p => (sbyte)1).First());
            }
        }

        [Test]
        public void DateFunctions()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                IQueryable<int> oneRow = context.Posts.Where(p => false).Select(p => 1).Concat(new int[] { 1 });

                var dateAdds = oneRow.Select(p => new List<DateTime?>
                {
                    DbFunctions.AddDays(new DateTime(2014, 2, 28), 1),
                    DbFunctions.AddHours(new DateTime(2014, 2, 28, 23, 0, 0), 1),
                    DbFunctions.AddMinutes(new DateTime(2014, 2, 28, 23, 59, 0), 1),
                    DbFunctions.AddSeconds(new DateTime(2014, 2, 28, 23, 59, 59), 1),
                    DbFunctions.AddMilliseconds(new DateTime(2014, 2, 28, 23, 59, 59, 999), 2 - p),
                    DbFunctions.AddMicroseconds(DbFunctions.AddMicroseconds(new DateTime(2014, 2, 28, 23, 59, 59, 999), 500), 500),
                    DbFunctions.AddNanoseconds(new DateTime(2014, 2, 28, 23, 59, 59, 999), 999999 + p),
                    DbFunctions.AddMonths(new DateTime(2014, 2, 1), 1),
                    DbFunctions.AddYears(new DateTime(2013, 3, 1), 1)
                }).First();
                foreach (var result in dateAdds)
                {
                    Assert.IsTrue(result.Value == new DateTime(2014, 3, 1, 0, 0, 0));
                }

                var dateDiffs = oneRow.Select(p => new {
                    a = DbFunctions.DiffDays(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(2000, 1, 1, 0, 0, 0)),
                    b = DbFunctions.DiffHours(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(2000, 1, 1, 0, 0, 0)),
                    c = DbFunctions.DiffMinutes(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(2000, 1, 1, 0, 0, 0)),
                    d = DbFunctions.DiffSeconds(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(2000, 1, 1, 0, 0, 0)),
                    e = DbFunctions.DiffMilliseconds(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(2000, 1, 1, 0, 0, 0)),
                    f = DbFunctions.DiffMicroseconds(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(2000, 1, 1, 0, 0, 0)),
                    g = DbFunctions.DiffNanoseconds(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(2000, 1, 1, 0, 0, 0)),
                    h = DbFunctions.DiffMonths(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(3000, 1, 1, 0, 0, 0)),
                    i = DbFunctions.DiffYears(new DateTime(1999, 12, 31, 23, 59, 59, 999), new DateTime(3000, 1, 1, 0, 0, 0)),
                    j = DbFunctions.DiffYears(null, new DateTime(2000, 1, 1)),
                    k = DbFunctions.DiffMinutes(new TimeSpan(1, 2, 3), new TimeSpan(4, 5, 6)),
                    l = DbFunctions.DiffMinutes(new TimeSpan(1, 2, 3), null)
                }).First();
                Assert.AreEqual(dateDiffs.a, 1);
                Assert.AreEqual(dateDiffs.b, 1);
                Assert.AreEqual(dateDiffs.c, 1);
                Assert.AreEqual(dateDiffs.d, 1);
                Assert.AreEqual(dateDiffs.e, 1);
                Assert.AreEqual(dateDiffs.f, 1000);
                Assert.AreEqual(dateDiffs.g, 1000000);
                Assert.AreEqual(dateDiffs.h, 12001);
                Assert.AreEqual(dateDiffs.i, 1001);
                Assert.AreEqual(dateDiffs.j, null);
                Assert.AreEqual(dateDiffs.k, 183);
                Assert.AreEqual(dateDiffs.l, null);
            }
        }

        //Hunting season is open Happy hunting on OrderBy,GroupBy,Min,Max,Skip,Take,ThenBy... and all posible combinations

        [Test]
        public void TestComplicatedQueries()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                // Test that the subqueries are evaluated in the correct order
                context.Posts.Select(p => p.Content).Distinct().OrderBy(l => l).ToArray();
                context.Posts.OrderByDescending(p => p.BlogId).Take(2).OrderBy(p => p.BlogId).Skip(1).Take(1).Select(l => l.BlogId).ToArray();
                context.Posts.Take(3).Take(4).ToArray();
                context.Posts.OrderByDescending(p => p.BlogId).Take(3).OrderBy(p => p.BlogId).Take(2).ToArray();
                context.Posts.OrderByDescending(p => p.BlogId).Take(3).OrderBy(p => p.BlogId).ToArray();

                // Test that lhs and rhs of UNION ALL is wrapped in parentheses
                context.Blogs.Take(3).Concat(context.Blogs.Take(4)).ToArray();

                // Flatten set ops
                context.Blogs.Concat(context.Blogs).Concat(context.Blogs.Concat(context.Blogs)).ToArray();
                context.Blogs.Intersect(context.Blogs).Intersect(context.Blogs).ToArray();
                // But not except
                context.Blogs.Concat(context.Blogs.Except(context.Blogs)).ToArray();

                // In
                int[] arr = {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40};
                context.Blogs.Where(b => arr.Contains(b.BlogId)).ToArray();

                // Subquery as a select column
                context.Blogs.Select(b => new { b.Name, b.BlogId, c = context.Posts.Count(p => p.BlogId == b.BlogId) + 1 }).ToArray();
                context.Blogs.Where(b => b.Name == context.Blogs.FirstOrDefault().Name).ToArray();

                context.Blogs.Where(b => b.Name == context.Blogs.Where(b2 => b2.BlogId < 100).FirstOrDefault().Name).ToArray();

                // Similar to https://github.com/npgsql/Npgsql/issues/156 However EF is turning the GroupBy into a Distinct here
                context.Posts.OrderBy(p => p.Title).ThenBy(p => p.Content).Take(100).GroupBy(p => p.Title).Select(p => p.Key).ToArray();

                // Check precedence for ||
                // http://stackoverflow.com/questions/21908464/wrong-query-generated-by-postgresql-provider-to-entity-framework-for-contains-an
                context.Posts.Where(p => "a" != string.Concat("a", "b")).ToArray();

                Action<string> elinq = (string query) => {
                    new System.Data.Entity.Core.Objects.ObjectQuery<System.Data.Common.DbDataRecord>(query, ((System.Data.Entity.Infrastructure.IObjectContextAdapter)context).ObjectContext).ToArray();
                };

                elinq("Select a, max(b) from {1,2,3,3,4} as b group by b as a");
                elinq("Select (select count(a.Name) from ((select a1.Name from Blogs as a1) union all (select b2.Name from Blogs as b2)) as a) as cnt, b.BlogId from Posts as b");

                elinq("Select a.ID from (select distinct c.ID, c.ID2 From (select b.Name as ID, b.Name = '' as ID2 From Blogs as b order by ID asc limit 3) as c) as a where a.ID = 'a'");

                // a LEFT JOIN b LEFT JOIN c ON x ON y => Parsed as: a LEFT JOIN (b LEFT JOIN c ON x) ON y, which is correct
                elinq("Select a.BlogId, d.id2, d.id3 from Blogs as a left outer join (Select b.BlogId as id2, c.BlogId as id3 From Blogs as b left outer join Blogs as c on true) as d on true");
                // Aliasing
                elinq("Select a.BlogId, d.id2, d.id3 from Blogs as a left outer join (Select top(1) b.BlogId as id2, c.BlogId as id3 From Blogs as b left outer join Blogs as c on true) as d on true");

                // Anyelement (creates DbNewInstanceExpressions)
                elinq("Anyelement (Select Blogs.BlogId, Blogs.Name from Blogs)");
                elinq("Select a.BlogId, Anyelement (Select value b.BlogId + 1 from Blogs as b) as c from Blogs as a");
            }
        }

        [Test]
        [MonoIgnore("Probably bug in mono. See https://github.com/npgsql/Npgsql/issues/289.")]
        public void TestComplicatedQueriesMonoFails()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                // Similar to https://github.com/npgsql/Npgsql/issues/216
                (from d in context.Posts
                 group d by new { d.Content, d.Title }).FirstOrDefault();

                // NewInstance(Column(Element(Limit(Sort(Project(...))))))
                // https://github.com/npgsql/Npgsql/issues/280
                (from postsGrouped in context.Posts.GroupBy(x => x.BlogId)
                 let lastPostDate = postsGrouped.OrderByDescending(x => x.CreationDate)
                                                                 .Select(x => x.CreationDate)
                                                                  .FirstOrDefault()
                 select new {
                     LastPostDate = lastPostDate
                 }).ToArray();
            }
        }

        [Test]
        public void TestComplicatedQueriesWithApply()
        {
            using (var conn = OpenConnection(ConnectionString))
                TestUtil.MinimumPgVersion(conn, "9.3.0");
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                // Test Apply
                (from t1 in context.Blogs
                    from t2 in context.Posts.Where(p => p.BlogId == t1.BlogId).Take(1)
                    select new { t1, t2 }).ToArray();

                Action<string> elinq = (string query) =>
                {
                    new System.Data.Entity.Core.Objects.ObjectQuery<System.Data.Common.DbDataRecord>(query, ((System.Data.Entity.Infrastructure.IObjectContextAdapter)context).ObjectContext).ToArray();
                };

                // Joins, apply
                elinq("Select value Blogs.BlogId From Blogs outer apply (Select p1.BlogId as bid, p1.PostId as bid2 from Posts as p1 left outer join (Select value p.PostId from Posts as p where p.PostId < Blogs.BlogId)) as b outer apply (Select p.PostId from Posts as p where p.PostId < b.bid)");

                // Just some really crazy query that results in an apply as well
                context.Blogs.Select(b => new { b, b.BlogId, n = b.Posts.Select(p => new { t = p.Title + b.Name, n = p.Blog.Posts.Count(p2 => p2.BlogId < 4) }).Take(2) }).ToArray();
            }
        }

        [Test]
        public void TestScalarValuedStoredFunctions()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                // Try to call stored function using ESQL
                var directCallQuery = ((IObjectContextAdapter)context).ObjectContext.CreateQuery<int>(
                    "SELECT VALUE BloggingContext.ClrStoredAddFunction(@p1, @p2) FROM {1}",
                    new ObjectParameter("p1", 1),
                    new ObjectParameter("p2", 10)
                    );
                var directSQL = directCallQuery.ToTraceString();
                var directCallResult = directCallQuery.First();

                // Add some data and query it back using Stored Function
                var blog = new Blog
                {
                    Name = "Some blog name",
                    Posts = new List<Post>()
                };
                for (int i = 0; i < 5; i++)
                    blog.Posts.Add(new Post()
                    {
                        Content = "Some post content " + i,
                        Rating = (byte)i,
                        Title = "Some post Title " + i
                    });
                context.Blogs.Add(blog);
                context.NoColumnsEntities.Add(new NoColumnsEntity());
                context.SaveChanges();

                // Query back
                var modifiedIds = context.Posts
                    .Select(x => new { Id = x.PostId, Changed = BloggingContext.StoredAddFunction(x.PostId, 100) })
                    .ToList();
                var localChangedIds = modifiedIds.Select(x => x.Id + 100).ToList();
                var remoteChangedIds = modifiedIds.Select(x => x.Changed).ToList();

                // Comapre results
                Assert.AreEqual(directCallResult, 11);
                Assert.IsTrue(directSQL.Contains("\"dbo\".\"StoredAddFunction\""));
                CollectionAssert.AreEqual(localChangedIds, remoteChangedIds);
            }
        }

        [Test]
        public void TestScalarValuedStoredFunctions_with_null_StoreFunctionName()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                context.Blogs.Add(new Blog { Name = "_" });
                context.SaveChanges();

                // Direct ESQL
                var directCallQuery = ((IObjectContextAdapter)context).ObjectContext.CreateQuery<int>(
                    "SELECT VALUE BloggingContext.StoredEchoFunction(@p1) FROM {1}",
                    new ObjectParameter("p1", 1337));
                var directSQL = directCallQuery.ToTraceString();
                var directCallResult = directCallQuery.First();

                // LINQ
                var echo = context.Blogs
                    .Select(x => BloggingContext.StoredEchoFunction(1337))
                    .First();

                // Comapre results
                Assert.AreEqual(directCallResult, 1337);
                Assert.IsTrue(directSQL.Contains("\"dbo\".\"StoredEchoFunction\""));
                Assert.That(echo, Is.EqualTo(1337));
            }
        }

        [Test]
        public void TestCastFunction()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                var varbitVal = "10011";

                var blog = new Blog
                {
                    Name = "_",
                    Posts = new List<Post>
                    {
                        new Post
                        {
                            Content = "Some post content",
                            Rating = 1,
                            Title = "Some post Title",
                            VarbitColumn = varbitVal
                        }
                    }
                };
                context.Blogs.Add(blog);
                context.SaveChanges();

                Assert.IsTrue(
                    context.Posts.Select(
                        p => NpgsqlTypeFunctions.Cast(p.VarbitColumn, "varbit") == varbitVal).First());

                Assert.IsTrue(
                    context.Posts.Select(
                        p => NpgsqlTypeFunctions.Cast(p.VarbitColumn, "varbit") == "10011").First());
            }
        }

        [Test]
        public void Test_issue_27_select_ef_generated_literals_from_inner_select()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                var blog = new Blog { Name = "Hello" };
                context.Users.Add(new Administrator { Blogs = new List<Blog> { blog } });
                context.Users.Add(new Editor());
                context.SaveChanges();

                var administrator = context.Users
                    .Where(x => x is Administrator) // Removing this changes the query to using a UNION which doesn't fail.
                    .Select(
                        x => new
                        {
                           // causes entity framework to emit a literal discriminator
                           Computed = x is Administrator
                                ? "I administrate"
                                : x is Editor
                                    ? "I edit"
                                    : "Unknown",
                           // causes an inner select to be emitted thus showing the issue
                           HasBlog = x.Blogs.Any()
                        })
                    .First();

                Assert.That(administrator.Computed, Is.EqualTo("I administrate"));
                Assert.That(administrator.HasBlog, Is.True);
            }
        }

        [Test]
        public void TestTableValuedStoredFunctions()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                // Add some data and query it back using Stored Function
                context.Blogs.Add(new Blog
                {
                    Name = "Some blog1 name",
                    Posts = new List<Post>()
                });
                context.Blogs.Add(new Blog
                {
                    Name = "Some blog2 name",
                    Posts = new List<Post>()
                });
                context.SaveChanges();

                // Query back
                var query = from b in context.GetBlogsByName("blog1")
                        select b;
                var list = query.ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual("Some blog1 name", list[0].Name);

                // Query with projection
                var query2 = from b in context.GetBlogsByName("blog1")
                            select new { b.Name, Something = 1 };
                var list2 = query2.ToList();
                Assert.AreEqual(1, list2.Count);
                Assert.AreEqual("Some blog1 name", list2[0].Name);
                Assert.AreEqual(1, list2[0].Something);
            }
        }

        [Test]
        public void Test_string_type_inference_in_coalesce_statements()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                context.Blogs.Add(new Blog { Name = "Hello" });
                context.SaveChanges();

                string stringValue = "string_value";
                var query = context.Blogs.Select(b => stringValue + "_postfix");
                var blogTitle = query.First();
                Assert.That(blogTitle, Is.EqualTo("string_value_postfix"));
                Console.WriteLine(query.ToString());
                StringAssert.AreEqualIgnoringCase(
                    "SELECT COALESCE(@p__linq__0,E'') || E'_postfix' AS \"C1\" FROM \"dbo\".\"Blogs\" AS \"Extent1\"",
                    query.ToString());
            }
        }

        [Test]
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition")]
        public void Test_string_null_propagation()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                context.Blogs.Add(new Blog { Name = "Hello" });
                context.SaveChanges();

                string stringValue = "string_value";
                var query = context.Blogs.Select(b => (stringValue ?? "default_value") + "_postfix");
                var blog_title = query.First();
                Assert.That(blog_title, Is.EqualTo("string_value_postfix"));

                Console.WriteLine(query.ToString());
                StringAssert.AreEqualIgnoringCase(
                    "SELECT  CASE  WHEN (COALESCE(@p__linq__0,E'default_value') IS NULL) THEN (E'')"
                    + " WHEN (CAST (@p__linq__0 AS varchar) IS NULL) THEN (E'default_value') ELSE (@p__linq__0) END  ||"
                    + " E'_postfix' AS \"C1\" FROM \"dbo\".\"Blogs\" AS \"Extent1\"",
                    query.ToString());
            }
        }

        [Test]
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition")]
        public void Test_string_multiple_null_propagation()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                context.Blogs.Add(new Blog { Name = "Hello" });
                context.SaveChanges();

                string stringValue1 = "string_value1";
                string stringValue2 = "string_value2";
                string stringValue3 = "string_value3";

                var query = context.Blogs.Select(b => (stringValue1 ?? stringValue2 ?? stringValue3) + "_postfix");
                var blog_title = query.First();
                Assert.That(blog_title, Is.EqualTo("string_value1_postfix"));

                Console.WriteLine(query.ToString());
                StringAssert.AreEqualIgnoringCase(
                    "SELECT  CASE  WHEN (COALESCE(@p__linq__0,COALESCE(@p__linq__1,@p__linq__2)) IS NULL)"
                    + " THEN (E'') WHEN (CAST (@p__linq__0 AS varchar) IS NULL) THEN (COALESCE(@p__linq__1,@p__linq__2)) ELSE"
                    + " (@p__linq__0) END  || E'_postfix' AS \"C1\" FROM \"dbo\".\"Blogs\" AS \"Extent1\"",
                    query.ToString());
            }
        }

        [Test]
        public void Test_enum()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                context.ClrEnumEntities.Add(
                    new ClrEnumEntity
                    {
                        TestByte = TestByteEnum.Bar,
                        TestShort = TestShortEnum.Bar,
                        TestInt = TestIntEnum.Bar,
                        TestLong = TestLongEnum.Bar
                    });
                context.SaveChanges();

                var query = context.ClrEnumEntities.Where(
                    x => x.TestByte == TestByteEnum.Bar
                         && x.TestShort == TestShortEnum.Bar
                         && x.TestInt == TestIntEnum.Bar
                         && x.TestLong == TestLongEnum.Bar);

                var result = query.First();
                Assert.That(result.TestByte, Is.EqualTo(TestByteEnum.Bar));
                Assert.That(result.TestShort, Is.EqualTo(TestShortEnum.Bar));
                Assert.That(result.TestInt, Is.EqualTo(TestIntEnum.Bar));
                Assert.That(result.TestLong, Is.EqualTo(TestLongEnum.Bar));
            }
        }

        [Test]
        public void Test_enum_composite_key()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                context.ClrEnumCompositeKeyEntities.Add(
                    new ClrEnumCompositeKeyEntity
                    {
                        TestByte = TestByteEnum.Bar,
                        TestShort = TestShortEnum.Bar,
                        TestInt = TestIntEnum.Bar,
                        TestLong = TestLongEnum.Bar
                    });
                context.SaveChanges();
            }

            using (var context = new BloggingContext(ConnectionString))
            {
                var result = context.ClrEnumCompositeKeyEntities.Find(
                        TestByteEnum.Bar,
                        TestShortEnum.Bar,
                        TestIntEnum.Bar,
                        TestLongEnum.Bar);

                Assert.That(result, Is.Not.Null);
                Assert.That(result.TestByte, Is.EqualTo(TestByteEnum.Bar));
                Assert.That(result.TestShort, Is.EqualTo(TestShortEnum.Bar));
                Assert.That(result.TestInt, Is.EqualTo(TestIntEnum.Bar));
                Assert.That(result.TestLong, Is.EqualTo(TestLongEnum.Bar));
            }
        }

        [Test]
        public void Test_non_composable_function()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Database.Log = Console.Out.WriteLine;

                // Add some data and query it back using Stored Function
                context.Blogs.Add(new Blog
                {
                    Name = "Some blog1 name",
                    Posts = new List<Post>()
                });
                context.SaveChanges();

                // Query back
                var nameParameter = new ObjectParameter("Name", "blog1");
                var blogs = ((IObjectContextAdapter)context).ObjectContext.ExecuteFunction<Blog>("GetBlogsByName2", nameParameter).ToArray();

                Assert.AreEqual(1, blogs.Length);
                Assert.AreEqual("Some blog1 name", blogs[0].Name);
            }
        }

    }
}
