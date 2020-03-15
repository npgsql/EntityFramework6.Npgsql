using System.Linq;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees;

namespace Npgsql.SqlGenerators
{
    class SqlFunctionGenerator : SqlBaseGenerator
    {
        readonly DbFunctionCommandTree _commandTree;

        public SqlFunctionGenerator(DbFunctionCommandTree commandTree)
        {
            _commandTree = commandTree;
        }

        public override void BuildCommand(DbCommand command)
        {
            var paramStr = string.Join(",", command.Parameters.OfType<DbParameter>().Select(x => "@" + x.ParameterName).ToArray());
            command.CommandText = $"SELECT * FROM { QuoteIdentifier(_commandTree.EdmFunction.Schema) }.{ QuoteIdentifier(_commandTree.EdmFunction.Name) } ({paramStr})";
        }
    }
}
