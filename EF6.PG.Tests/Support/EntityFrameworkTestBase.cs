using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Reflection;
using NpgsqlTypes;

// ReSharper disable once CheckNamespace
namespace EntityFramework6.Npgsql.Tests
{
    public abstract class EntityFrameworkTestBase : TestBase
    {
        [OneTimeSetUp]
        public new void TestFixtureSetup()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                if (context.Database.Exists())
                    context.Database.Delete();//We delete to be 100% schema is synced
                context.Database.Create();
            }

            // Create sequence for the IntComputedValue property.
            using (var createSequenceConn = OpenConnection(ConnectionString))
            {
                createSequenceConn.ExecuteNonQuery("create sequence blog_int_computed_value_seq");
                createSequenceConn.ExecuteNonQuery("alter table \"dbo\".\"Blogs\" alter column \"IntComputedValue\" set default nextval('blog_int_computed_value_seq');");
                createSequenceConn.ExecuteNonQuery("alter table \"dbo\".\"Posts\" alter column \"VarbitColumn\" type varbit using null");
                createSequenceConn.ExecuteNonQuery("CREATE OR REPLACE FUNCTION \"dbo\".\"StoredAddFunction\"(integer, integer) RETURNS integer AS $$ SELECT $1 + $2; $$ LANGUAGE SQL;");
                createSequenceConn.ExecuteNonQuery("CREATE OR REPLACE FUNCTION \"dbo\".\"StoredEchoFunction\"(integer) RETURNS integer AS $$ SELECT $1; $$ LANGUAGE SQL;");
                createSequenceConn.ExecuteNonQuery("CREATE OR REPLACE FUNCTION \"dbo\".\"GetBlogsByName\"(text) RETURNS TABLE(\"BlogId\" int, \"Name\" text, \"IntComputedValue\" int) as $$ select \"BlogId\", \"Name\", \"IntComputedValue\" from \"dbo\".\"Blogs\" where \"Name\" ilike '%' || $1 || '%' $$ LANGUAGE SQL;");
            }
        }

        /// <summary>
        /// Clean any previous entites before our test
        /// </summary>
        [SetUp]
        protected void SetUp()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                context.Blogs.RemoveRange(context.Blogs);
                context.Posts.RemoveRange(context.Posts);
                context.Logs.RemoveRange(context.Logs);
                context.NoColumnsEntities.RemoveRange(context.NoColumnsEntities);
                context.SaveChanges();
            }
        }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Name { get; set; }

        public virtual List<Post> Posts { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int IntComputedValue { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public byte Rating { get; set; }
        public DateTime CreationDate { get; set; }
        public string VarbitColumn { get; set; }
        public int BlogId { get; set; }
        public virtual Blog Blog { get; set; }
    }

    public class Log
    {
        public int Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }

    public class ClrEnumEntity
    {
        public int Id { get; set; }
        public TestByteEnum TestByte { get; set; }
        public TestShortEnum TestShort { get; set; }
        public TestIntEnum TestInt { get; set; }
        public TestLongEnum TestLong { get; set; }
    }

    public class ClrEnumCompositeKeyEntity
    {
        [Key, Column(Order = 1)]
        public TestByteEnum TestByte { get; set; }

        [Key, Column(Order = 2)]
        public TestShortEnum TestShort { get; set; }

        [Key, Column(Order = 3)]
        public TestIntEnum TestInt { get; set; }

        [Key, Column(Order = 4)]
        public TestLongEnum TestLong { get; set; }
    }

    public enum TestByteEnum : byte
    {
        Foo = 0,
        Bar = 1
    }

    public enum TestShortEnum : short
    {
        Foo = 0,
        Bar = 1
    }

    public enum TestIntEnum
    {
        Foo = 0,
        Bar = 1
    }

    public enum TestLongEnum : long
    {
        Foo = 0,
        Bar = 1
    }

    public class NoColumnsEntity
    {
        public int Id { get; set; }
    }

    [Table("Users")]
    public abstract class User
    {
        public int Id { get; set; }

        public IList<Blog> Blogs { get; set; }
    }

    [Table("Editors")]
    public class Editor : User { }

    [Table("Administrators")]
    public class Administrator : User { }

    public class BloggingContext : DbContext
    {
        public BloggingContext(string connection)
            : base(new NpgsqlConnection(connection), CreateModel(new NpgsqlConnection(connection)), true)
        {
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<NoColumnsEntity> NoColumnsEntities { get; set; }
        public DbSet<ClrEnumEntity> ClrEnumEntities { get; set; }
        public DbSet<ClrEnumCompositeKeyEntity> ClrEnumCompositeKeyEntities { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Editor> Editors { get; set; }
        public DbSet<Administrator> Administrators { get; set; }
        
        [DbFunction("BloggingContext", "ClrStoredAddFunction")]
        public static int StoredAddFunction(int val1, int val2)
        {
            throw new NotSupportedException();
        }

        [DbFunction("BloggingContext", "StoredEchoFunction")]
        public static int StoredEchoFunction(int value)
        {
            throw new NotSupportedException();
        }

        [DbFunction("BloggingContext", "GetBlogsByName")]
        public IQueryable<Blog> GetBlogsByName(string name)
        {
            ObjectParameter nameParameter = new ObjectParameter("Name", name);

            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<Blog>(
                $"[GetBlogsByName](@Name)", nameParameter);
        }
        
        private static DbCompiledModel CreateModel(NpgsqlConnection connection)
        {
            var dbModelBuilder = new DbModelBuilder(DbModelBuilderVersion.Latest);

            // Import Sets
            dbModelBuilder.Entity<Blog>();
            dbModelBuilder.Entity<Post>();
            dbModelBuilder.Entity<Log>();
            dbModelBuilder.Entity<NoColumnsEntity>();
            dbModelBuilder.Entity<ClrEnumEntity>();
            dbModelBuilder.Entity<ClrEnumCompositeKeyEntity>();
            dbModelBuilder.Entity<User>();
            dbModelBuilder.Entity<Editor>();
            dbModelBuilder.Entity<Administrator>();

            // Import function
            var dbModel = dbModelBuilder.Build(connection);
            var edmType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32);

            //these parameter types need to match both the database method and the C# method for EF to link
            var edmStringType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String);

            var addFunc = EdmFunction.Create(
                "ClrStoredAddFunction",
                "BloggingContext",
                DataSpace.SSpace,
                new EdmFunctionPayload
                {
                    ParameterTypeSemantics = ParameterTypeSemantics.AllowImplicitConversion,
                    Schema = "dbo",
                    IsComposable = true,
                    IsNiladic = false,
                    IsBuiltIn = false,
                    IsAggregate = false,
                    IsFromProviderManifest = true,
                    StoreFunctionName = "StoredAddFunction",
                    ReturnParameters = new[]
                    {
                            FunctionParameter.Create("ReturnType", edmType, ParameterMode.ReturnValue)
                    },
                    Parameters = new[]
                    {
                            FunctionParameter.Create("Value1", edmType, ParameterMode.In),
                            FunctionParameter.Create("Value2", edmType, ParameterMode.In)
                    }
                },
                null);
            dbModel.StoreModel.AddItem(addFunc);

            var echoFunc = EdmFunction.Create(
                "StoredEchoFunction",
                "BloggingContext",
                DataSpace.SSpace,
                new EdmFunctionPayload
                {
                    ParameterTypeSemantics = ParameterTypeSemantics.AllowImplicitConversion,
                    Schema = "dbo",
                    IsComposable = true,
                    IsNiladic = false,
                    IsBuiltIn = false,
                    IsAggregate = false,
                    IsFromProviderManifest = true,
                    StoreFunctionName = null, // intentional
                        ReturnParameters = new[]
                    {
                            FunctionParameter.Create("ReturnType", edmType, ParameterMode.ReturnValue)
                    },
                    Parameters = new[]
                    {
                            FunctionParameter.Create("Value1", edmType, ParameterMode.In)
                    }
                },
                null);
            dbModel.StoreModel.AddItem(echoFunc);

            var stringStoreType = dbModel.ProviderManifest.GetStoreTypes().First(x => x.ClrEquivalentType == typeof(string));
            var modelBlogStoreType = dbModel.StoreModel.EntityTypes.First(x => x.Name == typeof(Blog).Name);
            var rowType = RowType.Create(
                modelBlogStoreType.Properties.Select(x =>
                {
                    var clone = EdmProperty.Create(x.Name, x.TypeUsage);
                    clone.CollectionKind = x.CollectionKind;
                    clone.ConcurrencyMode = x.ConcurrencyMode;
                    clone.IsFixedLength = x.IsFixedLength;
                    clone.IsMaxLength = x.IsMaxLength;
                    clone.IsUnicode = x.IsUnicode;
                    clone.MaxLength = x.MaxLength;
                    clone.Precision = x.Precision;
                    clone.Scale = x.Scale;
                    clone.StoreGeneratedPattern = x.StoreGeneratedPattern;
                    clone.SetMetadataProperties(x
                        .MetadataProperties
                        .Where(metadataProerty => !clone
                            .MetadataProperties
                            .Any(cloneMetadataProperty => cloneMetadataProperty.Name.Equals(metadataProerty.Name))));
                    return clone;
                }),
                null);

            var getBlogsFunc = EdmFunction.Create(
                "StoredGetBlogsFunction",
                "BloggingContext",
                DataSpace.SSpace,
                new EdmFunctionPayload
                {
                    ParameterTypeSemantics = ParameterTypeSemantics.AllowImplicitConversion,
                    Schema = "dbo",
                    IsComposable = true,
                    IsNiladic = false,
                    IsBuiltIn = false,
                    IsAggregate = false,
                    StoreFunctionName = "GetBlogsByName",
                    ReturnParameters = new[]
                    {
                            FunctionParameter.Create("ReturnType1", rowType.GetCollectionType(), ParameterMode.ReturnValue)
                    },
                    Parameters = new[]
                    {
                            FunctionParameter.Create("Name", stringStoreType, ParameterMode.In)
                    }
                },
                null);
            dbModel.StoreModel.AddItem(getBlogsFunc);

            var stringPrimitiveType = PrimitiveType.GetEdmPrimitiveTypes().First(x => x.ClrEquivalentType == typeof(string));
            var modelBlogConceptualType = dbModel.ConceptualModel.EntityTypes.First(x => x.Name == typeof(Blog).Name);
            EdmFunction getBlogsFuncModel = EdmFunction.Create(
                "GetBlogsByName",
                dbModel.ConceptualModel.Container.Name,
                DataSpace.CSpace,
                new EdmFunctionPayload
                {
                    IsFunctionImport = true,
                    IsComposable = true,
                    Parameters = new[]
                    {
                        FunctionParameter.Create("Name", stringPrimitiveType, ParameterMode.In)
                    },
                    ReturnParameters = new[]
                    {
                        FunctionParameter.Create("ReturnType1", modelBlogConceptualType.GetCollectionType(), ParameterMode.ReturnValue)
                    },
                    EntitySets = new[]
                    {
                        dbModel.ConceptualModel.Container.EntitySets.First(x => x.ElementType == modelBlogConceptualType)
                    }
                },
                null);
            dbModel.ConceptualModel.Container.AddFunctionImport(getBlogsFuncModel);

            dbModel.ConceptualToStoreMapping.AddFunctionImportMapping(new FunctionImportMappingComposable(
                    getBlogsFuncModel,
                    getBlogsFunc,
                    new FunctionImportResultMapping(),
                    dbModel.ConceptualToStoreMapping));

            var compiledModel = dbModel.Compile();
            return compiledModel;
        }
    }
}
