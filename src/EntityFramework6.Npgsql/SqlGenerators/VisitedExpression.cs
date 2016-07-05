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
using System.Linq;
using System.Text;
#if ENTITIES6
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
#else
using System.Data.Metadata.Edm;
using System.Data.Common.CommandTrees;
#endif
using NpgsqlTypes;
using System.Globalization;
using JetBrains.Annotations;

namespace Npgsql.SqlGenerators
{
    internal abstract class VisitedExpression
    {
        protected VisitedExpression()
        {
            ExpressionList = new List<VisitedExpression>();
        }

        public VisitedExpression Append(VisitedExpression expression)
        {
            ExpressionList.Add(expression);
            return this;
        }

        public VisitedExpression Append(string literal)
        {
            ExpressionList.Add(new LiteralExpression(literal));
            return this;
        }

        public override string ToString()
        {
            var sqlText = new StringBuilder();
            WriteSql(sqlText);
            return sqlText.ToString();
        }

        protected List<VisitedExpression> ExpressionList { get; }

        internal virtual void WriteSql(StringBuilder sqlText)
        {
            foreach (var expression in ExpressionList)
                expression.WriteSql(sqlText);
        }
    }

    internal class LiteralExpression : VisitedExpression
    {
        readonly string _literal;

        public LiteralExpression(string literal)
        {
            _literal = literal;
        }

        public new LiteralExpression Append(VisitedExpression expresion)
        {
            base.Append(expresion);
            return this;
        }

        public new void Append(string literal)
        {
            base.Append(literal);
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append(_literal);
            base.WriteSql(sqlText);
        }
    }

    internal class CommaSeparatedExpression : VisitedExpression
    {
        public readonly List<VisitedExpression> Arguments = new List<VisitedExpression>();

        internal override void WriteSql(StringBuilder sqlText)
        {
            for (var i = 0; i < Arguments.Count; ++i)
            {
                if (i != 0)
                    sqlText.Append(", ");
                Arguments[i].WriteSql(sqlText);
            }
            base.WriteSql(sqlText);
        }
    }

    internal class ConstantExpression : VisitedExpression
    {
        readonly PrimitiveTypeKind _primitiveType;
        readonly object _value;

        public ConstantExpression(object value, TypeUsage edmType)
        {
            if (edmType == null)
                throw new ArgumentNullException(nameof(edmType));
            if (edmType.EdmType == null || edmType.EdmType.BuiltInTypeKind != BuiltInTypeKind.PrimitiveType)
                throw new ArgumentException("Require primitive EdmType", nameof(edmType));
            _primitiveType = ((PrimitiveType)edmType.EdmType).PrimitiveTypeKind;
            _value = value;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            var ni = CultureInfo.InvariantCulture.NumberFormat;
            var value = _value;
            switch (_primitiveType)
            {
            case PrimitiveTypeKind.Binary:
            {
                sqlText.Append($"decode('{Convert.ToBase64String((byte[])_value)}', 'base64')");
            }
            break;
            case PrimitiveTypeKind.DateTime:
                sqlText.AppendFormat(ni, "TIMESTAMP '{0:o}'", _value);
                break;
            case PrimitiveTypeKind.DateTimeOffset:
                sqlText.AppendFormat(ni, "TIMESTAMP WITH TIME ZONE '{0:o}'", _value);
                break;
            case PrimitiveTypeKind.Decimal:
                sqlText.AppendFormat(ni, (decimal)_value < 0
                    ? "({0})::numeric"
                    : "{0}::numeric", _value
                );
                break;
            case PrimitiveTypeKind.Double:
                if (double.IsNaN((double)_value))
                    sqlText.Append("'NaN'::float8");
                else if (double.IsPositiveInfinity((double)_value))
                    sqlText.Append("'Infinity'::float8");
                else if (double.IsNegativeInfinity((double)_value))
                    sqlText.Append("'-Infinity'::float8");
                else if ((double)_value < 0)
                    sqlText.AppendFormat(ni, "({0:r})::float8", _value);
                else
                    sqlText.AppendFormat(ni, "{0:r}::float8", _value);
                break;
                // PostgreSQL has no support for bytes. int2 is used instead in Npgsql.
            case PrimitiveTypeKind.Byte:
                value = (short)(byte)_value;
                goto case PrimitiveTypeKind.Int16;
            case PrimitiveTypeKind.SByte:
                value = (short)(sbyte)_value;
                goto case PrimitiveTypeKind.Int16;
            case PrimitiveTypeKind.Int16:
                sqlText.AppendFormat(ni, (short)value < 0
                    ? "({0})::int2"
                    : "{0}::int2", _value
                );
                break;
            case PrimitiveTypeKind.Int32:
                sqlText.AppendFormat(ni, "{0}", _value);
                break;
            case PrimitiveTypeKind.Int64:
                sqlText.AppendFormat(ni, (long)_value < 0
                    ? "({0})::int8"
                    : "{0}::int8", _value
                );
                break;
            case PrimitiveTypeKind.Single:
                if (float.IsNaN((float)_value))
                    sqlText.Append("'NaN'::float4");
                else if (float.IsPositiveInfinity((float)_value))
                    sqlText.Append("'Infinity'::float4");
                else if (float.IsNegativeInfinity((float)_value))
                    sqlText.Append("'-Infinity'::float4");
                else if ((float)_value < 0)
                    sqlText.AppendFormat(ni, "({0:r})::float4", _value);
                else
                    sqlText.AppendFormat(ni, "{0:r}::float4", _value);
                break;
            case PrimitiveTypeKind.Boolean:
                sqlText.Append((bool)_value ? "TRUE" : "FALSE");
                break;
            case PrimitiveTypeKind.Guid:
                sqlText.Append('\'').Append((Guid)_value).Append('\'');
                sqlText.Append("::uuid");
                break;
            case PrimitiveTypeKind.String:
                sqlText.Append("E'").Append(((string)_value).Replace(@"\", @"\\").Replace("'", @"\'")).Append("'");
                break;
            case PrimitiveTypeKind.Time:
                sqlText.AppendFormat(ni, "INTERVAL '{0}'", (NpgsqlTimeSpan)(TimeSpan)_value);
                break;
            default:
                // TODO: must support more constant value types.
                throw new NotSupportedException($"NotSupported: {_primitiveType} {_value}");
            }
            base.WriteSql(sqlText);
        }
    }

    internal class InsertExpression : VisitedExpression
    {
        public void AppendTarget(VisitedExpression target)
        {
            Append(target);
        }

        public void AppendColumns(IEnumerable<VisitedExpression> columns)
        {
            if (!columns.Any())
                return;

            Append("(");
            var first = true;
            foreach (var expression in columns)
            {
                if (!first)
                    Append(",");
                Append(expression);
                first = false;
            }
            Append(")");
        }

        public void AppendValues(IEnumerable<VisitedExpression> columns)
        {
            if (columns.Any())
            {
                Append(" VALUES (");
                bool first = true;
                foreach (var expression in columns)
                {
                    if (!first)
                        Append(",");
                    Append(expression);
                    first = false;
                }
                Append(")");
            }
            else
                Append(" DEFAULT VALUES");
        }

        internal void AppendReturning(DbNewInstanceExpression expression)
        {
            Append(" RETURNING ");//Don't put () around columns it will probably have unwanted effect
            var first = true;
            foreach (var returingProperty in expression.Arguments)
            {
                if (!first)
                    Append(",");
                Append(SqlBaseGenerator.QuoteIdentifier(((DbPropertyExpression)returingProperty).Property.Name));
                first = false;
            }
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append("INSERT INTO ");
            base.WriteSql(sqlText);
        }
    }

    internal class UpdateExpression : VisitedExpression
    {
        bool _setSeperatorRequired;

        public void AppendTarget(VisitedExpression target)
        {
            Append(target);
        }

        public void AppendSet(VisitedExpression property, VisitedExpression value)
        {
            Append(_setSeperatorRequired ? "," : " SET ");
            Append(property);
            Append("=");
            Append(value);
            _setSeperatorRequired = true;
        }

        public void AppendWhere(VisitedExpression where)
        {
            Append(" WHERE ");
            Append(where);
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append("UPDATE ");
            base.WriteSql(sqlText);
        }

        internal void AppendReturning(DbNewInstanceExpression expression)
        {
            Append(" RETURNING ");//Don't put () around columns it will probably have unwanted effect
            var first = true;
            foreach (var returingProperty in expression.Arguments)
            {
                if (!first)
                    Append(",");
                Append(SqlBaseGenerator.QuoteIdentifier(((DbPropertyExpression)returingProperty).Property.Name));
                first = false;
            }
        }
    }

    internal class DeleteExpression : VisitedExpression
    {
        public void AppendFrom(VisitedExpression from)
        {
            Append(from);
        }

        public void AppendWhere(VisitedExpression where)
        {
            Append(" WHERE ");
            Append(where);
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append("DELETE FROM ");
            base.WriteSql(sqlText);
        }
    }

    internal class ColumnExpression : VisitedExpression
    {
        internal string Name { get; }
        internal TypeUsage ColumnType { get; }
        readonly VisitedExpression _column;

        public ColumnExpression(VisitedExpression column, string columnName, TypeUsage columnType)
        {
            _column = column;
            Name = columnName;
            ColumnType = columnType;
        }

        public Type ClrType
        {
            get
            {
                var pt = ColumnType?.EdmType as PrimitiveType;
                return pt?.ClrEquivalentType;
            }
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            _column.WriteSql(sqlText);

            var column = _column as ColumnReferenceExpression;
            if (column == null || column.Name != Name)
            {
                sqlText.Append(" AS ");
                sqlText.Append(SqlBaseGenerator.QuoteIdentifier(Name));
            }

            base.WriteSql(sqlText);
        }
    }

    internal class ColumnReferenceExpression : VisitedExpression
    {
        public string Variable { get; set; }
        public string Name { get; set; }

        internal override void WriteSql(StringBuilder sqlText)
        {
            if (Variable != null)
            {
                sqlText.Append(SqlBaseGenerator.QuoteIdentifier(Variable));
                sqlText.Append(".");
            }
            sqlText.Append(SqlBaseGenerator.QuoteIdentifier(Name));
            base.WriteSql(sqlText);
        }
    }

    internal class ScanExpression : VisitedExpression
    {
        readonly string _scanString;
        internal EntitySetBase Target { get; }

        public ScanExpression(string scanString, EntitySetBase target)
        {
            _scanString = scanString;
            Target = target;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append(_scanString);
            base.WriteSql(sqlText);
        }
    }

    internal class InputExpression : VisitedExpression
    {
        public bool Distinct { get; set; }

        public CommaSeparatedExpression Projection { get; set; }

        public readonly Dictionary<StringPair, string> ColumnsToProject = new Dictionary<StringPair, string>(); // (from, name) -> newName
        public readonly HashSet<string> ProjectNewNames = new HashSet<string>();

        // Either FromExpression or JoinExpression
        public VisitedExpression From { get; set; }
        public WhereExpression Where { get; set; }
        public GroupByExpression GroupBy { get; set; }
        public OrderByExpression OrderBy { get; set; }
        public SkipExpression Skip { get; set; }
        public LimitExpression Limit { get; set; }

        public InputExpression() { }

        public InputExpression(VisitedExpression from, string asName)
        {
            From = new FromExpression(from, asName);
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append("SELECT ");
            if (Distinct)
                sqlText.Append("DISTINCT ");
            if (Projection != null)
                Projection.WriteSql(sqlText);
            else
            {
                if (ColumnsToProject.Count == 0) sqlText.Append("1"); // Could be arbitrary, let's pick 1
                else
                {
                    var first = true;
                    foreach (var column in ColumnsToProject)
                    {
                        if (!first)
                            sqlText.Append(", ");
                        else
                            first = false;
                        sqlText.Append(SqlBaseGenerator.QuoteIdentifier(column.Key.Item1));
                        sqlText.Append(".");
                        sqlText.Append(SqlBaseGenerator.QuoteIdentifier(column.Key.Item2));
                        if (column.Key.Item2 != column.Value)
                        {
                            sqlText.Append(" AS ");
                            sqlText.Append(SqlBaseGenerator.QuoteIdentifier(column.Value));
                        }
                    }
                }
            }
            sqlText.Append(" FROM ");
            From.WriteSql(sqlText);
            Where?.WriteSql(sqlText);
            GroupBy?.WriteSql(sqlText);
            OrderBy?.WriteSql(sqlText);
            Skip?.WriteSql(sqlText);
            Limit?.WriteSql(sqlText);
            base.WriteSql(sqlText);
        }
    }

    internal class FromExpression : VisitedExpression
    {
        readonly VisitedExpression _from;
        internal string Name { get; }

        public FromExpression(VisitedExpression from, string name)
        {
            _from = from;
            Name = name;
        }

        public bool ForceSubquery { get; set; }

        internal override void WriteSql(StringBuilder sqlText)
        {
            var from = _from as InputExpression;
            if (from != null)
            {
                var input = from;
                if (!ForceSubquery && input.Projection == null && input.Where == null && input.Distinct == false && input.OrderBy == null &&
                    input.Skip == null && input.Limit == null)
                {
                    // There is no point of writing
                    // (SELECT ? FROM <from> AS <name>) AS <name>
                    // so just write <from> AS <name>
                    // <name> is always the same for both nodes
                    // However, PostgreSQL needs a subquery in case we are in the right hand side of an Apply expression
                    if (((FromExpression)input.From).Name != Name)
                        throw new ArgumentException();
                    input.From.WriteSql(sqlText);
                }
                else
                {
                    sqlText.Append("(");
                    input.WriteSql(sqlText);
                    sqlText.Append(") AS ");
                    sqlText.Append(SqlBaseGenerator.QuoteIdentifier(Name));
                }
            }
            else
            {
                var wrap = !(_from is LiteralExpression || _from is ScanExpression);
                if (wrap)
                    sqlText.Append("(");
                _from.WriteSql(sqlText);
                if (wrap)
                    sqlText.Append(")");
                sqlText.Append(" AS ");
                sqlText.Append(SqlBaseGenerator.QuoteIdentifier(Name));
            }
            base.WriteSql(sqlText);
        }
    }

    internal class JoinExpression : VisitedExpression
    {
        internal VisitedExpression Left { get; set; }
        internal DbExpressionKind JoinType { get; set; }
        internal VisitedExpression Right { get; set; }
        internal VisitedExpression Condition { get; set; }

        public JoinExpression() { }

        public JoinExpression(InputExpression left, DbExpressionKind joinType, InputExpression right, VisitedExpression condition)
        {
            Left = left;
            JoinType = joinType;
            Right = right;
            Condition = condition;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            Left.WriteSql(sqlText);
            switch (JoinType)
            {
            case DbExpressionKind.InnerJoin:
                sqlText.Append(" INNER JOIN ");
                break;
            case DbExpressionKind.LeftOuterJoin:
                sqlText.Append(" LEFT OUTER JOIN ");
                break;
            case DbExpressionKind.FullOuterJoin:
                sqlText.Append(" FULL OUTER JOIN ");
                break;
            case DbExpressionKind.CrossJoin:
                sqlText.Append(" CROSS JOIN ");
                break;
            case DbExpressionKind.CrossApply:
                sqlText.Append(" CROSS JOIN LATERAL ");
                break;
            case DbExpressionKind.OuterApply:
                sqlText.Append(" LEFT OUTER JOIN LATERAL ");
                break;
            default:
                throw new NotSupportedException();
            }
            Right.WriteSql(sqlText);
            if (JoinType == DbExpressionKind.OuterApply)
                sqlText.Append(" ON TRUE");
            else if (JoinType != DbExpressionKind.CrossJoin && JoinType != DbExpressionKind.CrossApply)
            {
                sqlText.Append(" ON ");
                Condition.WriteSql(sqlText);
            }
            base.WriteSql(sqlText);
        }
    }

    internal class WhereExpression : VisitedExpression
    {
        VisitedExpression _where;

        public WhereExpression(VisitedExpression where)
        {
            _where = where;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append(" WHERE ");
            _where.WriteSql(sqlText);
            base.WriteSql(sqlText);
        }

        internal void And(VisitedExpression andAlso)
        {
            // useNewPrecedence doesn't matter here since there was no change with the AND operator
            _where = OperatorExpression.Build(Operator.And, true, _where, andAlso);
        }
    }

    internal class PropertyExpression : VisitedExpression
    {
        readonly EdmMember _property;
        public string Name => _property.Name;
        public TypeUsage PropertyType => _property.TypeUsage;

        // used for inserts or updates where the column is not qualified
        public PropertyExpression(EdmMember property)
        {
            _property = property;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append(SqlBaseGenerator.QuoteIdentifier(_property.Name));
            base.WriteSql(sqlText);
        }

        // override ToString since we don't want variable substitution or identifier quoting
        // until writing out the SQL.
        public override string ToString() => Name;
    }

    internal class FunctionExpression : VisitedExpression
    {
        readonly string _name;
        readonly List<VisitedExpression> _args = new List<VisitedExpression>();

        public FunctionExpression(string name)
        {
            _name = name;
        }

        internal FunctionExpression AddArgument(VisitedExpression visitedExpression)
        {
            _args.Add(visitedExpression);
            return this;
        }

        internal FunctionExpression AddArgument(string argument)
        {
            _args.Add(new LiteralExpression(argument));
            return this;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append(_name);
            sqlText.Append("(");
            bool first = true;
            foreach (var arg in _args)
            {
                if (!first)
                    sqlText.Append(",");
                arg.WriteSql(sqlText);
                first = false;
            }
            sqlText.Append(")");
            base.WriteSql(sqlText);
        }
    }

    internal class CastExpression : VisitedExpression
    {
        readonly VisitedExpression _value;
        readonly string _type;

        public CastExpression(VisitedExpression value, string type)
        {
            _value = value;
            _type = type;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append("CAST (");
            _value.WriteSql(sqlText);
            sqlText.AppendFormat(" AS {0})", _type);
            base.WriteSql(sqlText);
        }
    }

    internal class GroupByExpression : VisitedExpression
    {
        bool _requiresGroupSeperator;

        public void AppendGroupingKey(VisitedExpression key)
        {
            if (_requiresGroupSeperator)
                Append(",");
            Append(key);
            _requiresGroupSeperator = true;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            if (ExpressionList.Count != 0)
                sqlText.Append(" GROUP BY ");
            base.WriteSql(sqlText);
        }
    }

    internal class LimitExpression : VisitedExpression
    {
        internal VisitedExpression Arg { get; set; }

        public LimitExpression(VisitedExpression arg)
        {
            Arg = arg;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append(" LIMIT ");
            Arg.WriteSql(sqlText);
            base.WriteSql(sqlText);
        }
    }

    internal class SkipExpression : VisitedExpression
    {
        readonly VisitedExpression _arg;

        public SkipExpression(VisitedExpression arg)
        {
            _arg = arg;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append(" OFFSET ");
            _arg.WriteSql(sqlText);
            base.WriteSql(sqlText);
        }
    }

    internal class Operator
    {
        internal string Op { get; }
        internal int LeftPrecedence { get; }
        internal int RightPrecedence { get; }
        // Since PostgreSQL 9.5, the operator precedence was changed
        internal int NewPrecedence { get; }
        internal UnaryTypes UnaryType { get; }
        internal bool RightAssoc { get; }

        internal enum UnaryTypes {
            Binary,
            Prefix,
            Postfix
        }

        Operator(string op, int precedence, int newPrecedence)
        {
            Op = ' ' + op + ' ';
            LeftPrecedence = precedence;
            RightPrecedence = precedence;
            NewPrecedence = newPrecedence;
            UnaryType = UnaryTypes.Binary;
        }

        Operator(string op, int leftPrecedence, int rightPrecedence, int newPrecedence)
        {
            Op = ' ' + op + ' ';
            LeftPrecedence = leftPrecedence;
            RightPrecedence = rightPrecedence;
            NewPrecedence = newPrecedence;
            UnaryType = UnaryTypes.Binary;
        }

        Operator(string op, int precedence, int newPrecedence, UnaryTypes unaryType, bool rightAssoc)
        {
            Op = unaryType == UnaryTypes.Binary ? ' ' + op + ' ' : unaryType == UnaryTypes.Prefix ? op + ' ' : ' ' + op;
            LeftPrecedence = precedence;
            RightPrecedence = precedence;
            NewPrecedence = newPrecedence;
            UnaryType = unaryType;
            RightAssoc = rightAssoc;
        }

        /*
         * Operator table
         * Corresponds to the operator precedence table at
         * http://www.postgresql.org/docs/current/interactive/sql-syntax-lexical.html
         *
         * Note that in versions up to 9.4, NOT IN and NOT LIKE have different precedences depending on
         * if the other operator is to the left or to the right.
         * For example, "a = b NOT LIKE c" is parsed as "(a = b) NOT LIKE c"
         * but "a NOT LIKE b = c" is parsed as "(a NOT LIKE b) = c"
         * This is because PostgreSQL's parser uses Bison's automatic
         * operator precedence handling, and NOT and LIKE has different precedences,
         * so this happens when the two keywords are put together like this.
         *
         */
        public static readonly Operator UnaryMinus = new Operator("-", 17, 12, UnaryTypes.Prefix, true);
        public static readonly Operator Mul = new Operator("*", 15, 10);
        public static readonly Operator Div = new Operator("/", 15, 10);
        public static readonly Operator Mod = new Operator("%", 15, 10);
        public static readonly Operator Add = new Operator("+", 14, 9);
        public static readonly Operator Sub = new Operator("-", 14, 9);
        public static readonly Operator IsNull = new Operator("IS NULL", 13, 4, UnaryTypes.Postfix, false);
        public static readonly Operator IsNotNull = new Operator("IS NOT NULL", 13, 4, UnaryTypes.Postfix, false);
        public static readonly Operator LessThanOrEquals = new Operator("<=", 10, 5);
        public static readonly Operator GreaterThanOrEquals = new Operator(">=", 10, 5);
        public static readonly Operator NotEquals = new Operator("!=", 10, 5);
        public static readonly Operator BitwiseAnd = new Operator("&", 10, 8);
        public static readonly Operator BitwiseOr = new Operator("|", 10, 8);
        public static readonly Operator BitwiseXor = new Operator("#", 10, 8);
        public static readonly Operator BitwiseNot = new Operator("~", 10, 8, UnaryTypes.Prefix, false);
        public static readonly Operator Concat = new Operator("||", 10, 8);
        public static readonly Operator In = new Operator("IN", 9, 6);
        public static readonly Operator NotIn = new Operator("NOT IN", 3, 9, 6);
        public static readonly Operator Like = new Operator("LIKE", 6, 6);
        public static readonly Operator NotLike = new Operator("NOT LIKE", 3, 6, 6);
        public static readonly Operator LessThan = new Operator("<", 5, 5);
        public static readonly Operator GreaterThan = new Operator(">", 5, 5);
        public new static readonly Operator Equals = new Operator("=", 4, 5, UnaryTypes.Binary, true);
        public static readonly Operator Not = new Operator("NOT", 3, 3, UnaryTypes.Prefix, true);
        public static readonly Operator And = new Operator("AND", 2, 2);
        public static readonly Operator Or = new Operator("OR", 1, 1);

        public static readonly Operator QueryMatch = new Operator("@@", 10, 8);
        public static readonly Operator QueryAnd = new Operator("&&", 10, 8);
        public static readonly Operator QueryOr = Concat;
        public static readonly Operator QueryNegate = new Operator("!!", 10, 8, UnaryTypes.Prefix, true);
        public static readonly Operator QueryContains = new Operator("@>", 10, 8);
        public static readonly Operator QueryIsContained = new Operator("<@", 10, 8);
        public static readonly Operator RegexMatch = new Operator("~", 10, 8);

        public static readonly Dictionary<Operator, Operator> NegateDict;

        static Operator()
        {
            NegateDict = new Dictionary<Operator, Operator>()
            {
                { IsNull, IsNotNull },
                { IsNotNull, IsNull },
                { LessThanOrEquals, GreaterThan },
                { GreaterThanOrEquals, LessThan },
                { NotEquals, Equals },
                { In, NotIn },
                { NotIn, In },
                { Like, NotLike },
                { NotLike, Like },
                { LessThan, GreaterThanOrEquals },
                { GreaterThan, LessThanOrEquals },
                { Equals, NotEquals }
            };
        }
    }

    internal class OperatorExpression : VisitedExpression
    {
        Operator _op;
        readonly bool _useNewPrecedences;
        readonly VisitedExpression _left;
        readonly VisitedExpression _right;

        OperatorExpression(Operator op, bool useNewPrecedences, [CanBeNull] VisitedExpression left, [CanBeNull] VisitedExpression right)
        {
            _op = op;
            _useNewPrecedences = useNewPrecedences;
            _left = left;
            _right = right;
        }

        public static OperatorExpression Build(Operator op, bool useNewPrecedences, VisitedExpression left, VisitedExpression right)
        {
            if (op.UnaryType == Operator.UnaryTypes.Binary)
                return new OperatorExpression(op, useNewPrecedences, left, right);
            throw new InvalidOperationException("Unary operator with two operands");
        }

        public static OperatorExpression Build(Operator op, bool useNewPrecedences, VisitedExpression exp)
        {
            switch (op.UnaryType)
            {
            case Operator.UnaryTypes.Prefix:
                return new OperatorExpression(op, useNewPrecedences, null, exp);
            case Operator.UnaryTypes.Postfix:
                return new OperatorExpression(op, useNewPrecedences, exp, null);
            default:
                throw new InvalidOperationException("Binary operator with one operand");
            }
        }

        /// <summary>
        /// Negates an expression.
        /// If possible, replaces the operator of exp if exp is a negatable OperatorExpression,
        /// else return a new OperatorExpression of type Not that wraps exp.
        /// </summary>
        public static VisitedExpression Negate(VisitedExpression exp, bool useNewPrecedences)
        {
            var expOp = exp as OperatorExpression;
            if (expOp != null)
            {
                var op = expOp._op;
                Operator newOp;
                if (Operator.NegateDict.TryGetValue(op, out newOp))
                {
                    expOp._op = newOp;
                    return expOp;
                }
                if (expOp._op == Operator.Not)
                    return expOp._right;
            }

            return Build(Operator.Not, useNewPrecedences, exp);
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            WriteSql(sqlText, null);
        }

        void WriteSql(StringBuilder sqlText, [CanBeNull] OperatorExpression rightParent)
        {
            var leftOp = _left as OperatorExpression;
            var rightOp = _right as OperatorExpression;

            bool wrapLeft, wrapRight;

            if (!_useNewPrecedences)
            {
                wrapLeft = leftOp != null && (_op.RightAssoc ? leftOp._op.RightPrecedence <= _op.LeftPrecedence : leftOp._op.RightPrecedence < _op.LeftPrecedence);
                wrapRight = rightOp != null && (!_op.RightAssoc ? rightOp._op.LeftPrecedence <= _op.RightPrecedence : rightOp._op.LeftPrecedence < _op.RightPrecedence);
            }
            else
            {
                wrapLeft = leftOp != null && (_op.RightAssoc ? leftOp._op.NewPrecedence <= _op.NewPrecedence : leftOp._op.NewPrecedence < _op.NewPrecedence);
                wrapRight = rightOp != null && (!_op.RightAssoc ? rightOp._op.NewPrecedence <= _op.NewPrecedence : rightOp._op.NewPrecedence < _op.NewPrecedence);
            }

            // Avoid parentheses for prefix operators if possible,
            // e.g. BitwiseNot: (a & (~ b)) & c is written as a & ~ b & c
            // but (a + (~ b)) + c must be written as a + (~ b) + c
            if (!_useNewPrecedences)
            {
                if (wrapRight && rightOp._left == null && (rightParent == null || (!rightParent._op.RightAssoc ? rightOp._op.RightPrecedence >= rightParent._op.LeftPrecedence : rightOp._op.RightPrecedence > rightParent._op.LeftPrecedence)))
                    wrapRight = false;
            }
            else
            {
                if (wrapRight && rightOp._left == null && (rightParent == null || (!rightParent._op.RightAssoc ? rightOp._op.NewPrecedence >= rightParent._op.NewPrecedence : rightOp._op.NewPrecedence > rightParent._op.NewPrecedence)))
                    wrapRight = false;
            }

            if (_left != null)
            {
                if (wrapLeft)
                    sqlText.Append("(");
                if (leftOp != null && !wrapLeft)
                    leftOp.WriteSql(sqlText, this);
                else
                    _left.WriteSql(sqlText);
                if (wrapLeft)
                    sqlText.Append(")");
            }

            sqlText.Append(_op.Op);

            if (_right != null)
            {
                if (wrapRight)
                    sqlText.Append("(");
                if (rightOp != null && !wrapRight)
                    rightOp.WriteSql(sqlText, rightParent);
                else
                    _right.WriteSql(sqlText);
                if (wrapRight)
                    sqlText.Append(")");
            }

            base.WriteSql(sqlText);
        }
    }

    internal class ConstantListExpression : VisitedExpression
    {
        readonly IEnumerable<ConstantExpression> _list;

        public ConstantListExpression(IEnumerable<ConstantExpression> list)
        {
            _list = list;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append("(");
            var first = true;
            foreach (var constant in _list)
            {
                if (!first)
                    sqlText.Append(",");
                constant.WriteSql(sqlText);
                first = false;
            }
            sqlText.Append(")");
            base.WriteSql(sqlText);
        }
    }

    internal class CombinedProjectionExpression : VisitedExpression
    {
        readonly List<VisitedExpression> _list;
        readonly string _setOperator;

        public CombinedProjectionExpression(DbExpressionKind setOperator, List<VisitedExpression> list)
        {
            _setOperator = setOperator == DbExpressionKind.UnionAll
                ? "UNION ALL"
                : setOperator == DbExpressionKind.Except
                    ? "EXCEPT"
                    : "INTERSECT";
            _list = list;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            for (var i = 0; i < _list.Count; i++)
            {
                if (i != 0)
                    sqlText.Append(' ').Append(_setOperator).Append(' ');
                sqlText.Append('(');
                _list[i].WriteSql(sqlText);
                sqlText.Append(')');
            }

            base.WriteSql(sqlText);
        }
    }

    internal class ExistsExpression : VisitedExpression
    {
        readonly VisitedExpression _argument;

        public ExistsExpression(VisitedExpression argument)
        {
            _argument = argument;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append("EXISTS (");
            _argument.WriteSql(sqlText);
            sqlText.Append(")");
            base.WriteSql(sqlText);
        }
    }

    class OrderByExpression : VisitedExpression
    {
        bool _requiresOrderSeperator;

        public void AppendSort(VisitedExpression sort, bool ascending)
        {
            if (_requiresOrderSeperator)
                Append(",");
            Append(sort);
            Append(ascending ? " ASC " : " DESC ");
            _requiresOrderSeperator = true;
        }

        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append(" ORDER BY ");
            base.WriteSql(sqlText);
        }
    }

    internal class TruncateTimeExpression : VisitedExpression
    {
        readonly VisitedExpression _arg;
        readonly string _truncationType;
        public TruncateTimeExpression(string truncationType, VisitedExpression visitedExpression)
        {
            _arg = visitedExpression;
            _truncationType = truncationType;
        }


        internal override void WriteSql(StringBuilder sqlText)
        {
            sqlText.Append("date_trunc");
            sqlText.Append("(");
            sqlText.Append("'" + _truncationType + "',");
            _arg.WriteSql(sqlText);
            sqlText.Append(")");
            base.WriteSql(sqlText);
        }
    }
}
