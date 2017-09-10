#region License
// The PostgreSQL License
//
// Copyright (C) 2016 The Npgsql Development Team
//
// Permission to use, copy, modify, and distribute this software and its
// documentation for any purpose, without fee, and without a written
// agreement is hereby granted, provided that the above copyright notice
// and this paragraph and the following two paragraphs appear in all copies.
//
// IN NO EVENT SHALL THE NPGSQL DEVELOPMENT TEAM BE LIABLE TO ANY PARTY
// FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES,
// INCLUDING LOST PROFITS, ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS
// DOCUMENTATION, EVEN IF THE NPGSQL DEVELOPMENT TEAM HAS BEEN ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//
// THE NPGSQL DEVELOPMENT TEAM SPECIFICALLY DISCLAIMS ANY WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE. THE SOFTWARE PROVIDED HEREUNDER IS
// ON AN "AS IS" BASIS, AND THE NPGSQL DEVELOPMENT TEAM HAS NO OBLIGATIONS
// TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.
#endregion

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
#if ENTITIES6
using System.Globalization;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
#else
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
#endif
using System.Linq;
using JetBrains.Annotations;
using System.Text.RegularExpressions;
using System.Text;

namespace Npgsql.SqlGenerators
{
    internal abstract class SqlBaseGenerator : DbExpressionVisitor<VisitedExpression>
    {
        internal NpgsqlCommand Command;
        internal bool CreateParametersForConstants;
        bool _useNewPrecedences;

        protected Dictionary<string, PendingProjectsNode> RefToNode = new Dictionary<string, PendingProjectsNode>();
        protected HashSet<InputExpression> CurrentExpressions = new HashSet<InputExpression>();
        protected uint AliasCounter;
        protected uint ParameterCount;

        internal Version Version
        {
            get { return _version; }
            set
            {
                _version = value;
                _useNewPrecedences = value >= new Version(9, 5);
            }
        }
        Version _version;

        static readonly Dictionary<string, string> AggregateFunctionNames = new Dictionary<string, string>()
        {
            {"Avg","avg"},
            {"Count","count"},
            {"Min","min"},
            {"Max","max"},
            {"Sum","sum"},
            {"BigCount","count"},
            {"StDev","stddev_samp"},
            {"StDevP","stddev_pop"},
            {"Var","var_samp"},
            {"VarP","var_pop"},
        };

#if ENTITIES6
        static readonly Dictionary<string, Operator> BinaryOperatorFunctionNames = new Dictionary<string, Operator>()
        {
            {"@@",Operator.QueryMatch},
            {"operator_tsquery_and",Operator.QueryAnd},
            {"operator_tsquery_or",Operator.QueryOr},
            {"operator_tsquery_contains",Operator.QueryContains},
            {"operator_tsquery_is_contained",Operator.QueryIsContained}
        };
#endif

        void EnterExpression(PendingProjectsNode n) => CurrentExpressions.Add(n.Last.Exp);
        void LeaveExpression(PendingProjectsNode n) => CurrentExpressions.Remove(n.Last.Exp);

        protected string NextAlias() => "Alias" + AliasCounter++;

        bool IsCompatible(InputExpression child, DbExpressionKind parentKind)
        {
            switch (parentKind)
            {
            case DbExpressionKind.Filter:
                return
                    child.Projection == null &&
                    child.GroupBy == null &&
                    child.Skip == null &&
                    child.Limit == null;
            case DbExpressionKind.GroupBy:
                return
                    child.Projection == null &&
                    child.GroupBy == null &&
                    child.Distinct == false &&
                    child.OrderBy == null &&
                    child.Skip == null &&
                    child.Limit == null;
            case DbExpressionKind.Distinct:
                return
                    child.OrderBy == null &&
                    child.Skip == null &&
                    child.Limit == null;
            case DbExpressionKind.Sort:
                return
                    child.Projection == null &&
                    child.GroupBy == null &&
                    child.Skip == null &&
                    child.Limit == null;
            case DbExpressionKind.Skip:
                return
                    child.Projection == null &&
                    child.Skip == null &&
                    child.Limit == null;
            case DbExpressionKind.Project:
                return
                    child.Projection == null &&
                    child.Distinct == false;
            // Limit and NewInstance are always true
            default:
                throw new ArgumentException("Unexpected parent expression kind");
            }
        }

        PendingProjectsNode GetInput(DbExpression expression, string childBindingName, string parentBindingName, DbExpressionKind parentKind)
        {
            var n = VisitInputWithBinding(expression, childBindingName);
            if (!IsCompatible(n.Last.Exp, parentKind))
                n.Selects.Add(new NameAndInputExpression(parentBindingName, new InputExpression(n.Last.Exp, n.Last.AsName)));
            return n;
        }

        PendingProjectsNode VisitInputWithBinding(DbExpression expression, string bindingName)
        {
            PendingProjectsNode n;
            switch (expression.ExpressionKind)
            {
            case DbExpressionKind.Scan:
            {
                var scan = (ScanExpression)expression.Accept(this);
                var input = new InputExpression(scan, bindingName);
                n = new PendingProjectsNode(bindingName, input);

                break;
            }
            case DbExpressionKind.Filter:
            {
                var exp = (DbFilterExpression)expression;
                n = GetInput(exp.Input.Expression, exp.Input.VariableName, bindingName, expression.ExpressionKind);
                EnterExpression(n);
                var pred = exp.Predicate.Accept(this);
                if (n.Last.Exp.Where == null)
                    n.Last.Exp.Where = new WhereExpression(pred);
                else
                    n.Last.Exp.Where.And(pred);
                LeaveExpression(n);

                break;
            }
            case DbExpressionKind.Sort:
            {
                var exp = (DbSortExpression)expression;
                n = GetInput(exp.Input.Expression, exp.Input.VariableName, bindingName, expression.ExpressionKind);
                EnterExpression(n);
                n.Last.Exp.OrderBy = new OrderByExpression();
                foreach (var order in exp.SortOrder)
                    n.Last.Exp.OrderBy.AppendSort(order.Expression.Accept(this), order.Ascending);
                LeaveExpression(n);

                break;
            }
            case DbExpressionKind.Skip:
            {
                var exp = (DbSkipExpression)expression;
                n = GetInput(exp.Input.Expression, exp.Input.VariableName, bindingName, expression.ExpressionKind);
                EnterExpression(n);
                n.Last.Exp.OrderBy = new OrderByExpression();
                foreach (var order in exp.SortOrder)
                    n.Last.Exp.OrderBy.AppendSort(order.Expression.Accept(this), order.Ascending);
                n.Last.Exp.Skip = new SkipExpression(exp.Count.Accept(this));
                LeaveExpression(n);
                break;
            }
            case DbExpressionKind.Distinct:
            {
                var exp = (DbDistinctExpression)expression;
                var childBindingName = NextAlias();

                n = VisitInputWithBinding(exp.Argument, childBindingName);
                if (!IsCompatible(n.Last.Exp, expression.ExpressionKind))
                {
                    var prev = n.Last.Exp;
                    var prevName = n.Last.AsName;
                    var input = new InputExpression(prev, prevName);
                    n.Selects.Add(new NameAndInputExpression(bindingName, input));

                    // We need to copy all the projected columns so the DISTINCT keyword will work on the correct columns
                    // A parent project expression is never compatible with this new expression,
                    // so these are the columns that finally will be projected, as wanted
                    foreach (ColumnExpression col in prev.Projection.Arguments)
                    {
                        input.ColumnsToProject.Add(new StringPair(prevName, col.Name), col.Name);
                        input.ProjectNewNames.Add(col.Name);
                    }
                }
                n.Last.Exp.Distinct = true;
                break;
            }
            case DbExpressionKind.Limit:
            {
                var exp = (DbLimitExpression)expression;
                n = VisitInputWithBinding(exp.Argument, NextAlias());
                if (n.Last.Exp.Limit != null)
                {
                    var least = new FunctionExpression("LEAST");
                    least.AddArgument(n.Last.Exp.Limit.Arg);
                    least.AddArgument(exp.Limit.Accept(this));
                    n.Last.Exp.Limit.Arg = least;
                }
                else
                    n.Last.Exp.Limit = new LimitExpression(exp.Limit.Accept(this));
                break;
            }
            case DbExpressionKind.NewInstance:
            {
                var exp = (DbNewInstanceExpression)expression;
                if (exp.Arguments.Count == 1 && exp.Arguments[0].ExpressionKind == DbExpressionKind.Element)
                {
                    n = VisitInputWithBinding(((DbElementExpression)exp.Arguments[0]).Argument, NextAlias());
                    if (n.Last.Exp.Limit != null)
                    {
                        var least = new FunctionExpression("LEAST");
                        least.AddArgument(n.Last.Exp.Limit.Arg);
                        least.AddArgument(new LiteralExpression("1"));
                        n.Last.Exp.Limit.Arg = least;
                    }
                    else
                        n.Last.Exp.Limit = new LimitExpression(new LiteralExpression("1"));
                }
                else if (exp.Arguments.Count >= 1)
                {
                    var result = new LiteralExpression("(");
                    for (var i = 0; i < exp.Arguments.Count; ++i)
                    {
                        var arg = exp.Arguments[i];
                        var visitedColumn = arg.Accept(this);
                        if (!(visitedColumn is ColumnExpression))
                            visitedColumn = new ColumnExpression(visitedColumn, "C", arg.ResultType);

                        result.Append(i == 0 ? "SELECT " : " UNION ALL SELECT ");
                        result.Append(visitedColumn);
                    }
                    result.Append(")");
                    n = new PendingProjectsNode(bindingName, new InputExpression(result, bindingName));
                }
                else
                {
                    var type = ((CollectionType)exp.ResultType.EdmType).TypeUsage;
                    var result = new LiteralExpression("(SELECT ");
                    result.Append(new CastExpression(new LiteralExpression("NULL"), GetDbType(type.EdmType)));
                    result.Append(" LIMIT 0)");
                    n = new PendingProjectsNode(bindingName, new InputExpression(result, bindingName));
                }
                break;
            }
            case DbExpressionKind.UnionAll:
            case DbExpressionKind.Intersect:
            case DbExpressionKind.Except:
            {
                var exp = (DbBinaryExpression)expression;
                var expKind = exp.ExpressionKind;
                var list = new List<VisitedExpression>();
                Action<DbExpression> func = null;
                func = e =>
                {
                    if (e.ExpressionKind == expKind && e.ExpressionKind != DbExpressionKind.Except)
                    {
                        var binaryExp = (DbBinaryExpression)e;
                        func(binaryExp.Left);
                        func(binaryExp.Right);
                    }
                    else
                        list.Add(VisitInputWithBinding(e, bindingName + "_" + list.Count).Last.Exp);
                };
                func(exp.Left);
                func(exp.Right);
                var input = new InputExpression(new CombinedProjectionExpression(expression.ExpressionKind, list), bindingName);
                n = new PendingProjectsNode(bindingName, input);
                break;
            }
            case DbExpressionKind.Project:
            {
                var exp = (DbProjectExpression)expression;
                var child = VisitInputWithBinding(exp.Input.Expression, exp.Input.VariableName);
                var input = child.Last.Exp;
                var enterScope = false;
                if (!IsCompatible(input, expression.ExpressionKind))
                    input = new InputExpression(input, child.Last.AsName);
                else
                    enterScope = true;

                if (enterScope) EnterExpression(child);

                input.Projection = new CommaSeparatedExpression();

                var projection = (DbNewInstanceExpression)exp.Projection;
                var rowType = (RowType)projection.ResultType.EdmType;
                for (var i = 0; i < rowType.Properties.Count && i < projection.Arguments.Count; ++i)
                {
                    var prop = rowType.Properties[i];
                    var argument = projection.Arguments[i].Accept(this);
                    var constantArgument = projection.Arguments[i] as DbConstantExpression;
                    if (constantArgument != null && constantArgument.Value is string)
                    {
                        argument = new CastExpression(argument, "varchar");
                    }

                    input.Projection.Arguments.Add(new ColumnExpression(argument, prop.Name, prop.TypeUsage));
                }

                if (enterScope) LeaveExpression(child);

                n = new PendingProjectsNode(bindingName, input);
                break;
            }
            case DbExpressionKind.GroupBy:
            {
                var exp = (DbGroupByExpression)expression;
                var child = VisitInputWithBinding(exp.Input.Expression, exp.Input.VariableName);

                // I don't know why the input for GroupBy in EF have two names
                RefToNode[exp.Input.GroupVariableName] = child;

                var input = child.Last.Exp;
                var enterScope = false;
                if (!IsCompatible(input, expression.ExpressionKind))
                    input = new InputExpression(input, child.Last.AsName);
                else enterScope = true;

                if (enterScope) EnterExpression(child);

                input.Projection = new CommaSeparatedExpression();

                input.GroupBy = new GroupByExpression();
                var rowType = (RowType)((CollectionType)exp.ResultType.EdmType).TypeUsage.EdmType;
                var columnIndex = 0;
                foreach (var key in exp.Keys)
                {
                    var keyColumnExpression = key.Accept(this);
                    var prop = rowType.Properties[columnIndex];
                    input.Projection.Arguments.Add(new ColumnExpression(keyColumnExpression, prop.Name, prop.TypeUsage));
                    // have no idea why EF is generating a group by with a constant expression,
                    // but postgresql doesn't need it.
                    if (!(key is DbConstantExpression))
                        input.GroupBy.AppendGroupingKey(keyColumnExpression);
                    ++columnIndex;
                }
                foreach (var ag in exp.Aggregates)
                {
                    var function = (DbFunctionAggregate)ag;
                    var functionExpression = VisitFunction(function);
                    var prop = rowType.Properties[columnIndex];
                    input.Projection.Arguments.Add(new ColumnExpression(functionExpression, prop.Name, prop.TypeUsage));
                    ++columnIndex;
                }

                if (enterScope) LeaveExpression(child);

                n = new PendingProjectsNode(bindingName, input);
                break;
            }
            case DbExpressionKind.CrossJoin:
            case DbExpressionKind.FullOuterJoin:
            case DbExpressionKind.InnerJoin:
            case DbExpressionKind.LeftOuterJoin:
            case DbExpressionKind.CrossApply:
            case DbExpressionKind.OuterApply:
            {
                var input = new InputExpression();
                n = new PendingProjectsNode(bindingName, input);

                var from = VisitJoinChildren(expression, input, n);

                input.From = from;

                break;
            }
            case DbExpressionKind.Function:
            {
                var function = (DbFunctionExpression)expression;
                var input = new InputExpression(
                    VisitFunction(function.Function, function.Arguments, function.ResultType), bindingName);

                n = new PendingProjectsNode(bindingName, input);
                break;
            }
            default:
                throw new NotImplementedException();
            }

            RefToNode[bindingName] = n;
            return n;
        }

        bool IsJoin(DbExpressionKind kind)
        {
            switch (kind)
            {
            case DbExpressionKind.CrossJoin:
            case DbExpressionKind.FullOuterJoin:
            case DbExpressionKind.InnerJoin:
            case DbExpressionKind.LeftOuterJoin:
            case DbExpressionKind.CrossApply:
            case DbExpressionKind.OuterApply:
                return true;
            }
            return false;
        }

        JoinExpression VisitJoinChildren(DbExpression expression, InputExpression input, PendingProjectsNode n)
        {
            DbExpressionBinding left, right;
            DbExpression condition = null;
            if (expression.ExpressionKind == DbExpressionKind.CrossJoin)
            {
                left = ((DbCrossJoinExpression)expression).Inputs[0];
                right = ((DbCrossJoinExpression)expression).Inputs[1];
                if (((DbCrossJoinExpression)expression).Inputs.Count > 2)
                {
                    // I have never seen more than 2 inputs in CrossJoin
                    throw new NotImplementedException();
                }
            }
            else if (expression.ExpressionKind == DbExpressionKind.CrossApply || expression.ExpressionKind == DbExpressionKind.OuterApply)
            {
                left = ((DbApplyExpression)expression).Input;
                right = ((DbApplyExpression)expression).Apply;
            }
            else
            {
                left = ((DbJoinExpression)expression).Left;
                right = ((DbJoinExpression)expression).Right;
                condition = ((DbJoinExpression)expression).JoinCondition;
            }

            return VisitJoinChildren(left.Expression, left.VariableName, right.Expression, right.VariableName, expression.ExpressionKind, condition, input, n);
        }

        JoinExpression VisitJoinChildren(DbExpression left, string leftName, DbExpression right, string rightName, DbExpressionKind joinType, [CanBeNull] DbExpression condition, InputExpression input, PendingProjectsNode n)
        {
            var join = new JoinExpression { JoinType = joinType };

            if (IsJoin(left.ExpressionKind))
                join.Left = VisitJoinChildren(left, input, n);
            else
            {
                var l = VisitInputWithBinding(left, leftName);
                l.JoinParent = n;
                join.Left = new FromExpression(l.Last.Exp, l.Last.AsName);
            }

            if (joinType == DbExpressionKind.OuterApply || joinType == DbExpressionKind.CrossApply)
            {
                EnterExpression(n);
                var r = VisitInputWithBinding(right, rightName);
                LeaveExpression(n);
                r.JoinParent = n;
                join.Right = new FromExpression(r.Last.Exp, r.Last.AsName) { ForceSubquery = true };
            }
            else
            {
                if (IsJoin(right.ExpressionKind))
                    join.Right = VisitJoinChildren(right, input, n);
                else
                {
                    var r = VisitInputWithBinding(right, rightName);
                    r.JoinParent = n;
                    join.Right = new FromExpression(r.Last.Exp, r.Last.AsName);
                }
            }

            if (condition != null)
            {
                EnterExpression(n);
                join.Condition = condition.Accept(this);
                LeaveExpression(n);
            }
            return join;
        }

        public override VisitedExpression Visit([NotNull] DbVariableReferenceExpression expression)
        {
            //return new VariableReferenceExpression(expression.VariableName, _variableSubstitution);
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbUnionAllExpression expression)
        {
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbTreatExpression expression)
        {
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbSkipExpression expression)
        {
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbSortExpression expression)
        {
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbScanExpression expression)
        {
            MetadataProperty metadata;
            string tableName;
            var overrideTable = "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator:Name";
            if (expression.Target.MetadataProperties.TryGetValue(overrideTable, false, out metadata) && metadata.Value != null)
                tableName = metadata.Value.ToString();
            else if (expression.Target.MetadataProperties.TryGetValue("Table", false, out metadata) && metadata.Value != null)
                tableName = metadata.Value.ToString();
            else
                tableName = expression.Target.Name;

            if (expression.Target.MetadataProperties.Contains("DefiningQuery"))
            {
                var definingQuery = expression.Target.MetadataProperties.GetValue("DefiningQuery", false);
                if (definingQuery.Value != null)
                    return new ScanExpression("(" + definingQuery.Value + ")", expression.Target);
            }

            ScanExpression scan;
            var overrideSchema = "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator:Schema";
            if (expression.Target.MetadataProperties.TryGetValue(overrideSchema, false, out metadata) && metadata.Value != null)
            {
                var schema = metadata.Value.ToString();
                if (schema == string.Empty)
                    scan = new ScanExpression(QuoteIdentifier(tableName), expression.Target);
                else
                    scan = new ScanExpression(QuoteIdentifier(schema) + "." + QuoteIdentifier(tableName), expression.Target);
            }
            else if (expression.Target.MetadataProperties.TryGetValue("Schema", false, out metadata) && metadata.Value != null)
            {
                var schema = metadata.Value.ToString();
                if (schema == string.Empty)
                    scan = new ScanExpression(QuoteIdentifier(tableName), expression.Target);
                else
                    scan = new ScanExpression(QuoteIdentifier(schema) + "." + QuoteIdentifier(tableName), expression.Target);
            }
            else
            {
                if (expression.Target.EntityContainer.Name == string.Empty)
                    scan = new ScanExpression(QuoteIdentifier(tableName), expression.Target);
                else
                    scan = new ScanExpression(QuoteIdentifier(expression.Target.EntityContainer.Name) + "." + QuoteIdentifier(tableName), expression.Target);
            }
            
            return scan;
        }

        public override VisitedExpression Visit([NotNull] DbRelationshipNavigationExpression expression)
        {
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbRefExpression expression)
        {
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbQuantifierExpression expression)
        {
            // TODO: EXISTS or NOT EXISTS depending on expression.ExpressionKind
            // comes with it's built in test (subselect for EXISTS)
            // This kind of expression is never even created in the EF6 code base
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbProjectExpression expression)
            => VisitInputWithBinding(expression, NextAlias()).Last.Exp;

        // use parameter in sql
        public override VisitedExpression Visit([NotNull] DbParameterReferenceExpression expression)
            => new LiteralExpression("@" + expression.ParameterName);

        public override VisitedExpression Visit([NotNull] DbOrExpression expression)
            => OperatorExpression.Build(Operator.Or, _useNewPrecedences, expression.Left.Accept(this), expression.Right.Accept(this));

        public override VisitedExpression Visit([NotNull] DbOfTypeExpression expression)
        {
            throw new NotImplementedException();
        }

        // select does something different here.  But insert, update, delete, and functions can just use
        // a NULL literal.
        public override VisitedExpression Visit([NotNull] DbNullExpression expression)
            => new LiteralExpression("NULL");

        // argument can be a "NOT EXISTS" or similar operator that can be negated.
        // Convert the not if that's the case
        public override VisitedExpression Visit([NotNull] DbNotExpression expression)
            => OperatorExpression.Negate(expression.Argument.Accept(this), _useNewPrecedences);

        // Handled by VisitInputWithBinding
        public override VisitedExpression Visit([NotNull] DbNewInstanceExpression expression)
        {
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbLimitExpression expression)
        {
            // Normally handled by VisitInputWithBinding

            // Otherwise, it is (probably) a child of a DbElementExpression,
            // in which case the child of this expression might be a DbProjectExpression,
            // then the correct columns will be projected since Limit is compatible with the result of a DbProjectExpression,
            // which will result in having a Projection on the node after visiting it.
            var node = VisitInputWithBinding(expression, NextAlias());
            if (node.Last.Exp.Projection == null)
            {
                // This DbLimitExpression is (probably) a child of DbElementExpression
                // and this expression's child is not a DbProjectExpression, but we should
                // find a DbProjectExpression if we look deeper in the command tree.
                // The child of this expression is (probably) a DbSortExpression or something else
                // that will (probably) be an ancestor to a DbProjectExpression.

                // Since this is (probably) a child of DbElementExpression, we want the first column,
                // so make sure it is propagated from the nearest explicit projection.

                var projection = node.Selects[0].Exp.Projection;
                for (var i = 1; i < node.Selects.Count; i++)
                {
                    var column = (ColumnExpression)projection.Arguments[0];
                    node.Selects[i].Exp.ColumnsToProject[new StringPair(node.Selects[i - 1].AsName, column.Name)] = column.Name;
                }
            }
            return node.Last.Exp;
        }

        // LIKE keyword
        public override VisitedExpression Visit([NotNull] DbLikeExpression expression)
            => OperatorExpression.Build(Operator.Like, _useNewPrecedences, expression.Argument.Accept(this), expression.Pattern.Accept(this));

        public override VisitedExpression Visit([NotNull] DbJoinExpression expression)
        {
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbIsOfExpression expression)
        {
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbIsNullExpression expression)
            => OperatorExpression.Build(Operator.IsNull, _useNewPrecedences, expression.Argument.Accept(this));

        // NOT EXISTS
        public override VisitedExpression Visit([NotNull] DbIsEmptyExpression expression)
            => OperatorExpression.Negate(new ExistsExpression(expression.Argument.Accept(this)), _useNewPrecedences);

        public override VisitedExpression Visit([NotNull] DbIntersectExpression expression)
        {
            // INTERSECT keyword
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        // Normally handled by VisitInputWithBinding
        // Otherwise, it is (probably) a child of a DbElementExpression.
        // Group by always projects the correct columns.
        public override VisitedExpression Visit([NotNull] DbGroupByExpression expression)
            => VisitInputWithBinding(expression, NextAlias()).Last.Exp;

        public override VisitedExpression Visit([NotNull] DbRefKeyExpression expression)
        {
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbEntityRefExpression expression)
        {
            throw new NotImplementedException();
        }

        // a function call
        // may be built in, canonical, or user defined
        public override VisitedExpression Visit([NotNull] DbFunctionExpression expression)
            => VisitFunction(expression.Function, expression.Arguments, expression.ResultType);

        public override VisitedExpression Visit([NotNull] DbFilterExpression expression)
        {
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbExceptExpression expression)
        {
            // EXCEPT keyword
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbElementExpression expression)
        {
            // If child of DbNewInstanceExpression, this is handled in VisitInputWithBinding

            // a scalar expression (ie ExecuteScalar)
            // so it will likely be translated into a select
            //throw new NotImplementedException();
            var scalar = new LiteralExpression("(");
            scalar.Append(expression.Argument.Accept(this));
            scalar.Append(")");
            return scalar;
        }

        public override VisitedExpression Visit([NotNull] DbDistinctExpression expression)
        {
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbDerefExpression expression)
        {
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbCrossJoinExpression expression)
        {
            // join without ON
            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbConstantExpression expression)
        {
            if (CreateParametersForConstants)
            {
                var parameter = new NpgsqlParameter
                {
                    ParameterName = "p_" + ParameterCount++,
                    NpgsqlDbType = NpgsqlProviderManifest.GetNpgsqlDbType(((PrimitiveType)expression.ResultType.EdmType).PrimitiveTypeKind),
                    Value = expression.Value
                };
                Command.Parameters.Add(parameter);
                return new LiteralExpression("@" + parameter.ParameterName);
            }

            return new ConstantExpression(expression.Value, expression.ResultType);
        }

        public override VisitedExpression Visit([NotNull] DbComparisonExpression expression)
        {
            Operator comparisonOperator;
            switch (expression.ExpressionKind)
            {
            case DbExpressionKind.Equals: comparisonOperator = Operator.Equals; break;
            case DbExpressionKind.GreaterThan: comparisonOperator = Operator.GreaterThan; break;
            case DbExpressionKind.GreaterThanOrEquals: comparisonOperator = Operator.GreaterThanOrEquals; break;
            case DbExpressionKind.LessThan: comparisonOperator = Operator.LessThan; break;
            case DbExpressionKind.LessThanOrEquals: comparisonOperator = Operator.LessThanOrEquals; break;
            case DbExpressionKind.Like: comparisonOperator = Operator.Like; break;
            case DbExpressionKind.NotEquals: comparisonOperator = Operator.NotEquals; break;
            default: throw new NotSupportedException();
            }
            return OperatorExpression.Build(comparisonOperator, _useNewPrecedences, expression.Left.Accept(this), expression.Right.Accept(this));
        }

        public override VisitedExpression Visit([NotNull] DbCastExpression expression)
            => new CastExpression(expression.Argument.Accept(this), GetDbType(expression.ResultType.EdmType));

        protected string GetDbType(EdmType edmType)
        {
            var primitiveType = edmType as PrimitiveType;
            if (primitiveType == null)
                throw new NotSupportedException();

            switch (primitiveType.PrimitiveTypeKind)
            {
            case PrimitiveTypeKind.Boolean:
                return "bool";
            case PrimitiveTypeKind.SByte:
            case PrimitiveTypeKind.Byte:
            case PrimitiveTypeKind.Int16:
                return "int2";
            case PrimitiveTypeKind.Int32:
                return "int4";
            case PrimitiveTypeKind.Int64:
                return "int8";
            case PrimitiveTypeKind.String:
                return "text";
            case PrimitiveTypeKind.Decimal:
                return "numeric";
            case PrimitiveTypeKind.Single:
                return "float4";
            case PrimitiveTypeKind.Double:
                return "float8";
            case PrimitiveTypeKind.DateTime:
                return "timestamp";
            case PrimitiveTypeKind.DateTimeOffset:
                return "timestamptz";
            case PrimitiveTypeKind.Time:
                return "interval";
            case PrimitiveTypeKind.Binary:
                return "bytea";
            case PrimitiveTypeKind.Guid:
                return "uuid";
            }
            throw new NotSupportedException();
        }

        public override VisitedExpression Visit([NotNull] DbCaseExpression expression)
        {
            var caseExpression = new LiteralExpression(" CASE ");
            for (var i = 0; i < expression.When.Count && i < expression.Then.Count; ++i)
            {
                caseExpression.Append(" WHEN (");
                caseExpression.Append(expression.When[i].Accept(this));
                caseExpression.Append(") THEN (");
                caseExpression.Append(expression.Then[i].Accept(this));
                caseExpression.Append(")");
            }

            if (expression.Else is DbNullExpression)
                caseExpression.Append(" END ");
            else
            {
                caseExpression.Append(" ELSE (");
                caseExpression.Append(expression.Else.Accept(this));
                caseExpression.Append(") END ");
            }

            return caseExpression;
        }

        public override VisitedExpression Visit([NotNull] DbArithmeticExpression expression)
        {
            Operator arithmeticOperator;

            switch (expression.ExpressionKind)
            {
            case DbExpressionKind.Divide:
                arithmeticOperator = Operator.Div;
                break;
            case DbExpressionKind.Minus:
                arithmeticOperator = Operator.Sub;
                break;
            case DbExpressionKind.Modulo:
                arithmeticOperator = Operator.Mod;
                break;
            case DbExpressionKind.Multiply:
                arithmeticOperator = Operator.Mul;
                break;
            case DbExpressionKind.Plus:
                arithmeticOperator = Operator.Add;
                break;
            case DbExpressionKind.UnaryMinus:
                arithmeticOperator = Operator.UnaryMinus;
                break;
            default:
                throw new NotSupportedException();
            }

            if (expression.ExpressionKind == DbExpressionKind.UnaryMinus)
            {
                Debug.Assert(expression.Arguments.Count == 1);
                return OperatorExpression.Build(arithmeticOperator, _useNewPrecedences, expression.Arguments[0].Accept(this));
            }

            Debug.Assert(expression.Arguments.Count == 2);
            return OperatorExpression.Build(arithmeticOperator, _useNewPrecedences, expression.Arguments[0].Accept(this), expression.Arguments[1].Accept(this));
        }

        public override VisitedExpression Visit([NotNull] DbApplyExpression expression)
        {
            // like a join, but used when the right hand side (the Apply part) is a function.
            // it lets you return the results of a function call given values from the
            // left hand side (the Input part).
            // sql standard is lateral join

            // Handled by VisitInputWithBinding
            throw new NotImplementedException();
        }

        public override VisitedExpression Visit([NotNull] DbAndExpression expression)
            => OperatorExpression.Build(Operator.And, _useNewPrecedences, expression.Left.Accept(this), expression.Right.Accept(this));

        public override VisitedExpression Visit([NotNull] DbExpression expression)
        {
            // only concrete types visited
            throw new NotSupportedException();
        }

        public abstract void BuildCommand(DbCommand command);

        internal static string QuoteIdentifier(string identifier)
            => "\"" + identifier.Replace("\"", "\"\"") + "\"";

        VisitedExpression VisitFunction(DbFunctionAggregate functionAggregate)
        {
            if (functionAggregate.Function.NamespaceName == "Edm")
            {
                FunctionExpression aggregate;
                try
                {
                    aggregate = new FunctionExpression(AggregateFunctionNames[functionAggregate.Function.Name]);
                }
                catch (KeyNotFoundException)
                {
                    throw new NotSupportedException();
                }
                Debug.Assert(functionAggregate.Arguments.Count == 1);
                VisitedExpression aggregateArg;
                if (functionAggregate.Distinct)
                {
                    aggregateArg = new LiteralExpression("DISTINCT ");
                    ((LiteralExpression)aggregateArg).Append(functionAggregate.Arguments[0].Accept(this));
                }
                else
                {
                    aggregateArg = functionAggregate.Arguments[0].Accept(this);
                }
                aggregate.AddArgument(aggregateArg);
                return new CastExpression(aggregate, GetDbType(functionAggregate.ResultType.EdmType));
            }
            throw new NotSupportedException();
        }

        VisitedExpression VisitFunction(EdmFunction function, IList<DbExpression> args, TypeUsage resultType)
        {
            if (function.NamespaceName == "Edm")
            {
                VisitedExpression arg;
                switch (function.Name)
                {
                // string functions
                case "Concat":
                    Debug.Assert(args.Count == 2);
                    return OperatorExpression.Build(Operator.Concat, _useNewPrecedences, args[0].Accept(this), args[1].Accept(this));
                case "Contains":
                    Debug.Assert(args.Count == 2);
                    var contains = new FunctionExpression("position");
                    arg = args[1].Accept(this);
                    arg.Append(" in ");
                    arg.Append(args[0].Accept(this));
                    contains.AddArgument(arg);
                    // if position returns zero, then contains is false
                    return OperatorExpression.Build(Operator.GreaterThan, _useNewPrecedences, contains, new LiteralExpression("0"));
                // case "EndsWith": - depends on a reverse function to be able to implement with parameterized queries
                case "IndexOf":
                    Debug.Assert(args.Count == 2);
                    var indexOf = new FunctionExpression("position");
                    arg = args[0].Accept(this);
                    arg.Append(" in ");
                    arg.Append(args[1].Accept(this));
                    indexOf.AddArgument(arg);
                    return indexOf;
                case "Left":
                    Debug.Assert(args.Count == 2);
                    return Substring(args[0].Accept(this), new LiteralExpression(" 1 "), args[1].Accept(this));
                case "Length":
                    var length = new FunctionExpression("char_length");
                    Debug.Assert(args.Count == 1);
                    length.AddArgument(args[0].Accept(this));
                    return new CastExpression(length, GetDbType(resultType.EdmType));
                case "LTrim":
                    return StringModifier("ltrim", args);
                case "Replace":
                    var replace = new FunctionExpression("replace");
                    Debug.Assert(args.Count == 3);
                    replace.AddArgument(args[0].Accept(this));
                    replace.AddArgument(args[1].Accept(this));
                    replace.AddArgument(args[2].Accept(this));
                    return replace;
                // case "Reverse":
                case "Right":
                    Debug.Assert(args.Count == 2);
                    {
                        var arg0 = args[0].Accept(this);
                        var arg1 = args[1].Accept(this);
                        var start = new FunctionExpression("char_length");
                        start.AddArgument(arg0);
                        // add one before subtracting count since strings are 1 based in postgresql
                        return Substring(arg0, OperatorExpression.Build(Operator.Sub, _useNewPrecedences, OperatorExpression.Build(Operator.Add, _useNewPrecedences, start, new LiteralExpression("1")), arg1));
                    }
                case "RTrim":
                    return StringModifier("rtrim", args);
                case "Substring":
                    Debug.Assert(args.Count == 3);
                    return Substring(args[0].Accept(this), args[1].Accept(this), args[2].Accept(this));
                case "StartsWith":
                    Debug.Assert(args.Count == 2);
                    var startsWith = new FunctionExpression("position");
                    arg = args[1].Accept(this);
                    arg.Append(" in ");
                    arg.Append(args[0].Accept(this));
                    startsWith.AddArgument(arg);
                    return OperatorExpression.Build(Operator.Equals, _useNewPrecedences, startsWith, new LiteralExpression("1"));
                case "ToLower":
                    return StringModifier("lower", args);
                case "ToUpper":
                    return StringModifier("upper", args);
                case "Trim":
                    return StringModifier("btrim", args);

                // date functions
                // date functions
                case "AddDays":
                case "AddHours":
                case "AddMicroseconds":
                case "AddMilliseconds":
                case "AddMinutes":
                case "AddMonths":
                case "AddNanoseconds":
                case "AddSeconds":
                case "AddYears":
                    return DateAdd(function.Name, args);
                case "DiffDays":
                case "DiffHours":
                case "DiffMicroseconds":
                case "DiffMilliseconds":
                case "DiffMinutes":
                case "DiffMonths":
                case "DiffNanoseconds":
                case "DiffSeconds":
                case "DiffYears":
                    Debug.Assert(args.Count == 2);
                    return DateDiff(function.Name, args[0].Accept(this), args[1].Accept(this));
                case "Day":
                case "Hour":
                case "Minute":
                case "Month":
                case "Second":
                case "Year":
                    return DatePart(function.Name, args);
                case "Millisecond":
                    return DatePart("milliseconds", args);
                case "GetTotalOffsetMinutes":
                    var timezone = DatePart("timezone", args);
                    return OperatorExpression.Build(Operator.Div, _useNewPrecedences, timezone, new LiteralExpression("60"));
                case "CurrentDateTime":
                    return new LiteralExpression("LOCALTIMESTAMP");
                case "CurrentUtcDateTime":
                    var utcNow = new LiteralExpression("CURRENT_TIMESTAMP");
                    utcNow.Append(" AT TIME ZONE 'UTC'");
                    return utcNow;
                case "CurrentDateTimeOffset":
                    // TODO: this doesn't work yet because the reader
                    // doesn't return DateTimeOffset.
                    return new LiteralExpression("CURRENT_TIMESTAMP");

                // bitwise operators
                case "BitwiseAnd":
                    return BitwiseOperator(args, Operator.BitwiseAnd);
                case "BitwiseOr":
                    return BitwiseOperator(args, Operator.BitwiseOr);
                case "BitwiseXor":
                    return BitwiseOperator(args, Operator.BitwiseXor);
                case "BitwiseNot":
                    Debug.Assert(args.Count == 1);
                    return OperatorExpression.Build(Operator.BitwiseNot, _useNewPrecedences, args[0].Accept(this));

                // math operators
                case "Abs":
                case "Ceiling":
                case "Floor":
                    return UnaryMath(function.Name, args);
                case "Round":
                    return args.Count == 1 ? UnaryMath(function.Name, args) : BinaryMath(function.Name, args);
                case "Power":
                    return BinaryMath(function.Name, args);
                case "Truncate":
                    return BinaryMath("trunc", args);

                case "NewGuid":
                    return new FunctionExpression("uuid_generate_v4");
                case "TruncateTime":
                    return new TruncateTimeExpression("day", args[0].Accept(this));

                default:
                    throw new NotSupportedException("NotSupported " + function.Name);
                }
            }

#if ENTITIES6
            var functionName = function.StoreFunctionNameAttribute ?? function.Name;
            if (function.NamespaceName == "Npgsql")
            {
                Operator binaryOperator;
                if (BinaryOperatorFunctionNames.TryGetValue(functionName, out binaryOperator))
                {
                    if (args.Count != 2)
                        throw new ArgumentException($"Invalid number of {functionName} arguments. Expected 2.", nameof(args));

                    return OperatorExpression.Build(
                        binaryOperator,
                        _useNewPrecedences,
                        args[0].Accept(this),
                        args[1].Accept(this));
                }

                if (functionName == "operator_tsquery_negate")
                {
                    if (args.Count != 1)
                        throw new ArgumentException("Invalid number of operator_tsquery_not arguments. Expected 1.", nameof(args));

                    return OperatorExpression.Build(Operator.QueryNegate, _useNewPrecedences, args[0].Accept(this));
                }

                if (functionName == "ts_rank" || functionName == "ts_rank_cd")
                {
                    if (args.Count > 4)
                    {
                        var weightD = args[0] as DbConstantExpression;
                        var weightC = args[1] as DbConstantExpression;
                        var weightB = args[2] as DbConstantExpression;
                        var weightA = args[3] as DbConstantExpression;

                        if (weightD == null || weightC == null || weightB == null || weightA == null)
                            throw new NotSupportedException("All weight values must be constant expressions.");

                        var newValue = string.Format(
                            CultureInfo.InvariantCulture,
                            "{{ {0:r}, {1:r}, {2:r}, {3:r} }}",
                            weightD.Value,
                            weightC.Value,
                            weightB.Value,
                            weightA.Value);

                        args = new[] { DbExpression.FromString(newValue) }.Concat(args.Skip(4)).ToList();
                    }
                }
                else if (functionName == "setweight")
                {
                    if (args.Count != 2)
                        throw new ArgumentException("Invalid number of setweight arguments. Expected 2.", nameof(args));

                    var weightLabelExpression = args[1] as DbConstantExpression;
                    if (weightLabelExpression == null)
                        throw new NotSupportedException("setweight label argument must be a constant expression.");

                    var weightLabel = (NpgsqlWeightLabel)weightLabelExpression.Value;
                    if (!Enum.IsDefined(typeof(NpgsqlWeightLabel), weightLabelExpression.Value))
                        throw new NotSupportedException("Unsupported weight label value: " + weightLabel);

                    args = new[] { args[0], DbExpression.FromString(weightLabel.ToString()) };
                }
                else if (functionName == "as_tsvector")
                {
                    if (args.Count != 1)
                        throw new ArgumentException("Invalid number of arguments. Expected 1.", nameof(args));

                    return new CastExpression(args[0].Accept(this), "tsvector");
                }
                else if (functionName == "as_tsquery")
                {
                    if (args.Count != 1)
                        throw new ArgumentException("Invalid number of arguments. Expected 1.", nameof(args));

                    return new CastExpression(args[0].Accept(this), "tsquery");
                }
                else if (functionName == "match_regex")
                {
                    return VisitMatchRegex(function, args, resultType);
                }
                else if (functionName == "cast")
                {
                    if (args.Count != 2)
                        throw new ArgumentException("Invalid number of arguments. Expected 2.", "args");

                    var typeNameExpression = args[1] as DbConstantExpression;
                    if (typeNameExpression == null)
                        throw new NotSupportedException("cast type name argument must be a constant expression.");

                    return new CastExpression(args[0].Accept(this), typeNameExpression.Value.ToString());
                }
            }

            var customFuncCall = new FunctionExpression(
                string.IsNullOrEmpty(function.Schema)
                    ? QuoteIdentifier(functionName)
                    : QuoteIdentifier(function.Schema) + "." + QuoteIdentifier(functionName)
            );

            foreach (var a in args)
                customFuncCall.AddArgument(a.Accept(this));
            return customFuncCall;
#else
            throw new NotSupportedException();
#endif
        }

#if ENTITIES6
        VisitedExpression VisitMatchRegex(EdmFunction function, IList<DbExpression> args, TypeUsage resultType)
        {
            if (args.Count != 2 && args.Count != 3)
                throw new ArgumentException("Invalid number of arguments. Expected 2 or 3.", nameof(args));

            var options = RegexOptions.None;

            if (args.Count == 3)
            {
                var optionsExpression = args[2] as DbConstantExpression;
                if (optionsExpression == null)
                    throw new NotSupportedException("Options must be constant expression.");

                options = (RegexOptions)optionsExpression.Value;
            }

            if (options.HasFlag(RegexOptions.RightToLeft) || options.HasFlag(RegexOptions.ECMAScript))
            {
                throw new NotSupportedException("Options RightToLeft and ECMAScript are not supported.");
            }

            if (options == RegexOptions.Singleline)
            {
                return OperatorExpression.Build(
                    Operator.RegexMatch,
                    _useNewPrecedences,
                    args[0].Accept(this),
                    args[1].Accept(this));
            }

            var flags = new StringBuilder("(?");

            if (options.HasFlag(RegexOptions.IgnoreCase))
            {
                flags.Append('i');
            }

            if (options.HasFlag(RegexOptions.Multiline))
            {
                flags.Append('n');
            }
            else if (!options.HasFlag(RegexOptions.Singleline))
            {
                // In .NET's default mode, . doesn't match newlines but PostgreSQL it does.
                flags.Append('p');
            }

            if (options.HasFlag(RegexOptions.IgnorePatternWhitespace))
            {
                flags.Append('x');
            }

            flags.Append(')');

            var primitiveType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String);
            var newRegexExpression = OperatorExpression.Build(
                Operator.Concat,
                _useNewPrecedences,
                new ConstantExpression(flags.ToString(), TypeUsage.CreateStringTypeUsage(primitiveType, true, false)),
                args[1].Accept(this));

            return OperatorExpression.Build(
                    Operator.RegexMatch,
                    _useNewPrecedences,
                    args[0].Accept(this),
                    newRegexExpression);
        }
#endif

        VisitedExpression Substring(VisitedExpression source, VisitedExpression start, VisitedExpression count)
        {
            var substring = new FunctionExpression("substr");
            substring.AddArgument(source);
            substring.AddArgument(start);
            substring.AddArgument(count);
            return substring;
        }

        VisitedExpression Substring(VisitedExpression source, VisitedExpression start)
        {
            var substring = new FunctionExpression("substr");
            substring.AddArgument(source);
            substring.AddArgument(start);
            return substring;
        }

        VisitedExpression UnaryMath(string funcName, IList<DbExpression> args)
        {
            var mathFunction = new FunctionExpression(funcName);
            Debug.Assert(args.Count == 1);
            mathFunction.AddArgument(args[0].Accept(this));
            return mathFunction;
        }

        VisitedExpression BinaryMath(string funcName, IList<DbExpression> args)
        {
            var mathFunction = new FunctionExpression(funcName);
            Debug.Assert(args.Count == 2);
            mathFunction.AddArgument(args[0].Accept(this));
            mathFunction.AddArgument(args[1].Accept(this));
            return mathFunction;
        }

        VisitedExpression StringModifier(string modifier, IList<DbExpression> args)
        {
            var modifierFunction = new FunctionExpression(modifier);
            Debug.Assert(args.Count == 1);
            modifierFunction.AddArgument(args[0].Accept(this));
            return modifierFunction;
        }

        VisitedExpression DatePart(string partName, IList<DbExpression> args)
        {
            var extractDate = new FunctionExpression("cast(extract");
            Debug.Assert(args.Count == 1);
            VisitedExpression arg = new LiteralExpression(partName + " FROM ");
            arg.Append(args[0].Accept(this));
            extractDate.AddArgument(arg);
            // need to convert to Int32 to match cononical function
            extractDate.Append(" as int4)");
            return extractDate;
        }

        /// <summary>
        /// PostgreSQL has no direct functions to implements DateTime canonical functions
        /// http://msdn.microsoft.com/en-us/library/bb738626.aspx
        /// http://msdn.microsoft.com/en-us/library/bb738626.aspx
        /// but we can use workaround:
        /// expression + number * INTERVAL '1 number_type'
        /// where number_type is the number type (days, years and etc)
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        VisitedExpression DateAdd(string functionName, IList<DbExpression> args)
        {
            var nano = false;
            var part = functionName.Substring(3);

            if (part == "Nanoseconds")
            {
                nano = true;
                part = "Microseconds";
            }

            Debug.Assert(args.Count == 2);
            var time = args[0].Accept(this);
            var mulLeft = args[1].Accept(this);
            if (nano)
                mulLeft = OperatorExpression.Build(Operator.Div, _useNewPrecedences, mulLeft, new LiteralExpression("1000"));
            var mulRight = new LiteralExpression($"INTERVAL '1 {part}'");
            return OperatorExpression.Build(Operator.Add, _useNewPrecedences, time, OperatorExpression.Build(Operator.Mul, _useNewPrecedences, mulLeft, mulRight));
        }

        VisitedExpression DateDiff(string functionName, VisitedExpression start, VisitedExpression end)
        {
            switch (functionName)
            {
            case "DiffDays":
                start = new FunctionExpression("date_trunc").AddArgument("'day'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'day'").AddArgument(end);
                return new FunctionExpression("date_part").AddArgument("'day'").AddArgument(
                    OperatorExpression.Build(Operator.Sub, _useNewPrecedences, end, start)
                ).Append("::int4");
            case "DiffHours":
            {
                start = new FunctionExpression("date_trunc").AddArgument("'hour'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'hour'").AddArgument(end);
                var epoch = new LiteralExpression("epoch from ");
                var diff = OperatorExpression.Build(Operator.Sub, _useNewPrecedences, end, start);
                epoch.Append(diff);
                return OperatorExpression.Build(Operator.Div, _useNewPrecedences, new FunctionExpression("extract").AddArgument(epoch).Append("::int4"), new LiteralExpression("3600"));
            }
            case "DiffMicroseconds":
            {
                start = new FunctionExpression("date_trunc").AddArgument("'microseconds'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'microseconds'").AddArgument(end);
                var epoch = new LiteralExpression("epoch from ");
                var diff = OperatorExpression.Build(Operator.Sub, _useNewPrecedences, end, start);
                epoch.Append(diff);
                return new CastExpression(OperatorExpression.Build(Operator.Mul, _useNewPrecedences, new FunctionExpression("extract").AddArgument(epoch), new LiteralExpression("1000000")), "int4");
            }
            case "DiffMilliseconds":
            {
                start = new FunctionExpression("date_trunc").AddArgument("'milliseconds'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'milliseconds'").AddArgument(end);
                var epoch = new LiteralExpression("epoch from ");
                var diff = OperatorExpression.Build(Operator.Sub, _useNewPrecedences, end, start);
                epoch.Append(diff);
                return new CastExpression(OperatorExpression.Build(Operator.Mul, _useNewPrecedences, new FunctionExpression("extract").AddArgument(epoch), new LiteralExpression("1000")), "int4");
            }
            case "DiffMinutes":
            {
                start = new FunctionExpression("date_trunc").AddArgument("'minute'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'minute'").AddArgument(end);
                var epoch = new LiteralExpression("epoch from ");
                var diff = OperatorExpression.Build(Operator.Sub, _useNewPrecedences, end, start);
                epoch.Append(diff);
                return OperatorExpression.Build(Operator.Div, _useNewPrecedences, new FunctionExpression("extract").AddArgument(epoch).Append("::int4"), new LiteralExpression("60"));
            }
            case "DiffMonths":
            {
                start = new FunctionExpression("date_trunc").AddArgument("'month'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'month'").AddArgument(end);
                var age = new FunctionExpression("age").AddArgument(end).AddArgument(start);

                // A month is 30 days and a year is 365.25 days after conversion from interval to seconds.
                // After rounding and casting, the result will contain the correct number of months as an int4.
                var seconds = new FunctionExpression("extract").AddArgument(new LiteralExpression("epoch from ").Append(age));
                var months = OperatorExpression.Build(Operator.Div, _useNewPrecedences, seconds, new LiteralExpression("2629800.0"));
                return new FunctionExpression("round").AddArgument(months).Append("::int4");
            }
            case "DiffNanoseconds":
            {
                // PostgreSQL only supports microseconds precision, so the value will be a multiple of 1000
                // This date_trunc will make sure start and end are of type timestamp, e.g. if the arguments is of type date
                start = new FunctionExpression("date_trunc").AddArgument("'microseconds'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'microseconds'").AddArgument(end);
                var epoch = new LiteralExpression("epoch from ");
                var diff = OperatorExpression.Build(Operator.Sub, _useNewPrecedences, end, start);
                epoch.Append(diff);
                return new CastExpression(OperatorExpression.Build(Operator.Mul, _useNewPrecedences, new FunctionExpression("extract").AddArgument(epoch), new LiteralExpression("1000000000")), "int4");
            }
            case "DiffSeconds":
            {
                start = new FunctionExpression("date_trunc").AddArgument("'second'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'second'").AddArgument(end);
                var epoch = new LiteralExpression("epoch from ");
                var diff = OperatorExpression.Build(Operator.Sub, _useNewPrecedences, end, start);
                epoch.Append(diff);
                return new FunctionExpression("extract").AddArgument(epoch).Append("::int4");
            }
            case "DiffYears":
            {
                start = new FunctionExpression("date_trunc").AddArgument("'year'").AddArgument(start);
                end = new FunctionExpression("date_trunc").AddArgument("'year'").AddArgument(end);
                var age = new FunctionExpression("age").AddArgument(end).AddArgument(start);
                return new FunctionExpression("date_part").AddArgument("'year'").AddArgument(age).Append("::int4");
            }
            default:
                throw new NotSupportedException("Internal error: unknown function name " + functionName);
            }
        }

        VisitedExpression BitwiseOperator(IList<DbExpression> args, Operator oper)
        {
            Debug.Assert(args.Count == 2);
            return OperatorExpression.Build(oper, _useNewPrecedences, args[0].Accept(this), args[1].Accept(this));
        }

#if ENTITIES6
        public override VisitedExpression Visit([NotNull] DbInExpression expression)
        {
            var item = expression.Item.Accept(this);

            var elements = new ConstantExpression[expression.List.Count];
            for (var i = 0; i < expression.List.Count; i++)
                elements[i] = (ConstantExpression)expression.List[i].Accept(this);

            return OperatorExpression.Build(Operator.In, _useNewPrecedences, item, new ConstantListExpression(elements));
        }

        public override VisitedExpression Visit([NotNull] DbPropertyExpression expression)
        {
            // This is overridden in the other visitors
            throw new NotImplementedException("New in Entity Framework 6");
        }
#endif
    }
}
