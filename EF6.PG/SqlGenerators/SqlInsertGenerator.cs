using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees;

namespace Npgsql.SqlGenerators
{
    internal class SqlInsertGenerator : SqlBaseGenerator
    {
        readonly DbInsertCommandTree _commandTree;
        string _tableName;

        public SqlInsertGenerator(DbInsertCommandTree commandTree)
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
            // TODO: handle_commandTree.Parameters
            var insert = new InsertExpression();
            _tableName = _commandTree.Target.VariableName;
            insert.AppendTarget(_commandTree.Target.Expression.Accept(this));
            var columns = new List<VisitedExpression>();
            var values = new List<VisitedExpression>();
            foreach (DbSetClause clause in _commandTree.SetClauses)
            {
                columns.Add(clause.Property.Accept(this));
                values.Add(clause.Value.Accept(this));
            }
            insert.AppendColumns(columns);
            insert.AppendValues(values);
            if (_commandTree.Returning != null)
                insert.AppendReturning((DbNewInstanceExpression)_commandTree.Returning);
            _tableName = null;
            command.CommandText = insert.ToString();
        }
    }
}
