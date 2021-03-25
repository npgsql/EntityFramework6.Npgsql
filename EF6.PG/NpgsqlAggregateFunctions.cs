using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;

namespace Npgsql
{
    /// <summary>
    /// Use this class in LINQ queries to emit aggregate functions.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static class NpgsqlAggregateFunctions
    {
        /// <summary>
        /// Concatenate strings
        /// </summary>
        [DbFunction("NpgsqlAggregateFunctions", "StringAgg")]
        public static string StringAgg<TSource>(this IEnumerable<TSource> source)
            => throw new NotSupportedException();
    }
}
