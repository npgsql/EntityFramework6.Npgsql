using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Npgsql.SqlGenerators
{
    /// <summary>
    /// Used for lookup in a Dictionary, since Tuple is not available in .NET 3.5
    /// </summary>
    internal class StringPair
    {
        public string Item1 { get; }
        public string Item2 { get; }

        public StringPair(string s1, string s2)
        {
            Item1 = s1;
            Item2 = s2;
        }

        public override bool Equals([CanBeNull] object obj)
        {
            var o = obj as StringPair;
            if (o == null)
                return false;

            return Item1 == o.Item1 && Item2 == o.Item2;
        }

        public override int GetHashCode()
            => (Item1 + "." + Item2).GetHashCode();
    }
}
