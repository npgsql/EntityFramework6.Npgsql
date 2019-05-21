using System;
using NLog.Config;
using NLog.Targets;
using NLog;
using Npgsql;
using Npgsql.Logging;

using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace EntityFramework6.Npgsql.Tests
{
    public abstract class TestBase
    {
        static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The connection string that will be used when opening the connection to the tests database.
        /// May be overridden in fixtures, e.g. to set special connection parameters
        /// </summary>
        protected virtual string ConnectionString =>
            _connectionString ?? (_connectionString = Environment.GetEnvironmentVariable("NPGSQL_TEST_DB") ?? DefaultConnectionString);

        string _connectionString;

        static bool _loggingSetUp;

        /// <summary>
        /// Unless the NPGSQL_TEST_DB environment variable is defined, this is used as the connection string for the
        /// test database.
        /// </summary>
        const string DefaultConnectionString = "Server=localhost;User ID=npgsql_tests;Password=npgsql_tests;Database=npgsql_tests_ef6";

        #region Setup / Teardown

        [OneTimeSetUp]
        public virtual void TestFixtureSetup()
        {
            SetupLogging();
            _log.Debug("Connection string is: " + ConnectionString);
        }

        protected virtual void SetupLogging()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget();
            consoleTarget.Layout = @"${message} ${exception:format=tostring}";
            config.AddTarget("console", consoleTarget);
            var rule = new LoggingRule("*", NLog.LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule);
            NLog.LogManager.Configuration = config;

            if (!_loggingSetUp)
            {
                NpgsqlLogManager.Provider = new NLogLoggingProvider();
                NpgsqlLogManager.IsParameterLoggingEnabled = true;
                _loggingSetUp = true;
            }
        }

        #endregion

        #region Utilities for use by tests

        protected NpgsqlConnection OpenConnection(string connectionString = null)
        {
            if (connectionString == null)
                connectionString = ConnectionString;
            var conn = new NpgsqlConnection(connectionString);
            try
            {
                conn.Open();
            }
            catch (PostgresException e)
            {
                if (e.SqlState == "3D000")
                    TestUtil.IgnoreExceptOnBuildServer("Please create a database npgsql_tests, owned by user npgsql_tests");
                else if (e.SqlState == "28P01")
                    TestUtil.IgnoreExceptOnBuildServer("Please create a user npgsql_tests as follows: create user npgsql_tests with password 'npgsql_tests'");
                else
                    throw;
            }

            return conn;
        }

        protected NpgsqlConnection OpenConnection(NpgsqlConnectionStringBuilder csb)
        {
            return OpenConnection(csb.ToString());
        }

        #endregion
    }
}
