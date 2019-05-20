using System;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees;

namespace Npgsql.SqlGenerators
{
    class SqlUpdateGenerator : SqlBaseGenerator
    {
        readonly DbUpdateCommandTree _commandTree;
        string _tableName;

        public SqlUpdateGenerator(DbUpdateCommandTree commandTree)
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
            // TODO: handle _commandTree.Parameters
            var update = new UpdateExpression();
            _tableName = _commandTree.Target.VariableName;
            update.AppendTarget(_commandTree.Target.Expression.Accept(this));
            foreach (DbSetClause clause in _commandTree.SetClauses)
                update.AppendSet(clause.Property.Accept(this), clause.Value.Accept(this));
            if (_commandTree.Predicate != null)
                update.AppendWhere(_commandTree.Predicate.Accept(this));
            if (_commandTree.Returning != null)
                update.AppendReturning((DbNewInstanceExpression)_commandTree.Returning);
            _tableName = null;
            command.CommandText = update.ToString();
        }
    }
}
