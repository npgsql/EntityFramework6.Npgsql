using System;
using System.Data.Entity;
using Npgsql;

namespace EntityFramework6.Npgsql.Tests.Support
{
    public class TestDbConfiguration : DbConfiguration
    {
        public TestDbConfiguration()
        {
            SetProviderFactory("Npgsql", NpgsqlFactory.Instance);
            SetProviderServices("Npgsql", NpgsqlServices.Instance);
        }
    }
}
