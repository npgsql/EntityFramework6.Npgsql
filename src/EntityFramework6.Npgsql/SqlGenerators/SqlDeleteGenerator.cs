using System;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees;

namespace Npgsql.SqlGenerators
{
    internal class SqlDeleteGenerator : SqlBaseGenerator
    {
        readonly DbDeleteCommandTree _commandTree;
        string _tableName;

        public SqlDeleteGenerator(DbDeleteCommandTree commandTree)
        {
            _commandTree = commandTree;
        }

        public override VisitedExpression Visit(DbPropertyExpression expression)
        {
            var variable = expression.Instance as DbVariableReferenceExpression;
            if (variable == null || variable.VariableName != _tableName)
                throw new NotSupportedException();
            return new PropertyExpression(expression.Property);
        }

        public override void BuildCommand(DbCommand command)
        {
            // TODO: handle _commandTree.Returning and _commandTree.Parameters
            var delete = new DeleteExpression();
            _tableName = _commandTree.Target.VariableName;
            delete.AppendFrom(_commandTree.Target.Expression.Accept(this));
            if (_commandTree.Predicate != null)
                delete.AppendWhere(_commandTree.Predicate.Accept(this));
            _tableName = null;
            command.CommandText = delete.ToString();
        }
    }
}
