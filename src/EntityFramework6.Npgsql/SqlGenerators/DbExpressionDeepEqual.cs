using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;

namespace Npgsql.SqlGenerators
{
    public class DbExpressionDeepEqual
    {
        public static bool DeepEqual(DbExpression e1, DbExpression e2)
        {
            if (e1.Equals(e2)) return true;
            if (e1.GetType() != e2.GetType()) return false;
            if (!e1.ExpressionKind.Equals(e2.ExpressionKind)) return false;
            if (!DeepEqual(e1.ResultType,e2.ResultType)) return false;

            if (e1 is DbFunctionExpression f1 && e2 is DbFunctionExpression f2)
            {
                return DeepEqual(f1,f2);
            }
            if (e1 is DbConstantExpression c1 && e2 is DbConstantExpression c2)
            {
                return c1.Value.Equals(c2.Value);
            }
            if (e1 is DbBinaryExpression b1 && e2 is DbBinaryExpression b2)
            {
                return DeepEqual(b1,b2);
            }
            if (e1 is DbUnaryExpression u1 && e2 is DbUnaryExpression u2)
            {
                return DeepEqual(u1,u2);
            }
            if (e1 is DbVariableReferenceExpression v1 && e2 is DbVariableReferenceExpression v2)
            {
                return DeepEqual(v1,v2);
            }

            return false;
        }

        static bool DeepEqual(TypeUsage r1, TypeUsage r2)
        {
            if (r1.EdmType !=  r2.EdmType) return false;
            return true;
        }

        private static bool DeepEqual(DbFunctionExpression f1, DbFunctionExpression f2)
        {
            if (!f1.Function.Name.Equals(f2.Function.Name)) return false;
            if (!f1.Function.NamespaceName.Equals(f2.Function.NamespaceName)) return false;
            if (!f1.Arguments.Count.Equals(f2.Arguments.Count)) return false;

            var argumenst_equals = f1.Arguments
                .Zip(f2.Arguments, (a, b) => DeepEqual(a, b))
                .All(areEquals => areEquals);

            return argumenst_equals;
        }

        private static bool DeepEqual(DbBinaryExpression b1, DbBinaryExpression b2)
        {
            if (!DeepEqual(b1.Left,b2.Left)) return false;
            if (!DeepEqual(b1.Right,b2.Right)) return false;

            return true;
        }

        private static bool DeepEqual(DbUnaryExpression u1, DbUnaryExpression u2)
        {
            return DeepEqual(u1.Argument,u2.Argument);
        }

        private static bool DeepEqual(DbVariableReferenceExpression v1, DbVariableReferenceExpression v2)
        {
            return DeepEqual(v1.VariableName,v1.VariableName);
        }
    }
}
