using System;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;

namespace Npgsql
{
    /// <summary>
    /// Use this class in LINQ queries to emit timestamp manipulation SQL fragments.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static class NpgsqlDateTimeFunctions
    {
        /// <summary>
        /// Convert a timestamptz to a timezone
        /// </summary>
        [DbFunction("Npgsql", "timezone")]
        public static DateTime Timezone(string zone, DateTimeOffset timestamp)
            => throw new NotSupportedException();
    }
}
