using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics;
#if ENTITIES6
using System.Globalization;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
#else
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
#endif
using JetBrains.Annotations;


namespace Npgsql.SqlGenerators
{
    public class CaseIsNullToCoalesceReducer
    {
        public static DbFunctionExpression InvokeCoalesceExpression(params DbExpression[] argumentExpressions)
        {
            var fromClrType = PrimitiveType
                .GetEdmPrimitiveTypes()
                .FirstOrDefault(t => t.ClrEquivalentType == typeof(string));

            int i=0;
            var func =  EdmFunction.Create(
                "coalesce",
                "Npgsql",
                DataSpace.SSpace,
                new EdmFunctionPayload
                {
                    ParameterTypeSemantics = ParameterTypeSemantics.AllowImplicitConversion,
                    Schema = string.Empty,
                    IsBuiltIn = true,
                    IsAggregate = false,
                    IsFromProviderManifest = true,
                    StoreFunctionName = "coalesce",
                    IsComposable = true,
                    ReturnParameters = new[]
                    {
                        FunctionParameter.Create("ReturnType", fromClrType,ParameterMode.ReturnValue)
                    },
                    Parameters = argumentExpressions.Select(
                        x => FunctionParameter.Create(
                            "p" + (i++).ToString(),fromClrType,ParameterMode.In)).ToList()
                },
                new List<MetadataProperty>());

            return func.Invoke(argumentExpressions);
        }

        public static DbFunctionExpression UnnestCoalesceInvocations(DbFunctionExpression dbFunctionExpression)
        {
            var args = new List<DbExpression>();
            foreach (var arg in dbFunctionExpression.Arguments)
            {
                if(arg is DbFunctionExpression funcCall
                   && funcCall.Function.NamespaceName=="Npgsql"
                   && funcCall.Function.Name=="coalesce")
                {
                    args.AddRange(funcCall.Arguments);
                }
                else
                {
                    args.Add(arg);
                }
            }
            return InvokeCoalesceExpression(args.ToArray());
        }

        public static DbExpression TransformCoalesce(DbExpression expression)
        {
            if (expression is DbCaseExpression case2)
            {
                return TransformCoalesce(case2);
            }

            if (expression is DbIsNullExpression nullExp)
            {
                return TransformCoalesce(nullExp.Argument).IsNull();
            }
            return expression;
        }

        public static DbExpression TransformCoalesce(DbCaseExpression expression)
        {
            expression = DbExpressionBuilder.Case(
                expression.When.Select(TransformCoalesce),
                expression.Then.Select(TransformCoalesce),
                expression.Else);

            var lastWhen = expression.When.Count-1;
            if (expression.When[lastWhen].ExpressionKind == DbExpressionKind.IsNull)
            {
                var is_null = expression.When[lastWhen] as DbIsNullExpression;
                if (DbExpressionDeepEqual.DeepEqual(is_null.Argument,expression.Else))
                {
                    var coalesceInvocation = InvokeCoalesceExpression(is_null.Argument, expression.Then[lastWhen]);
                    coalesceInvocation = UnnestCoalesceInvocations(coalesceInvocation);

                    if (expression.When.Count == 1)
                    {
                        return coalesceInvocation;
                    }

                    var simplifiendCase = DbExpressionBuilder.Case(
                        expression.When.Take(lastWhen),
                        expression.Then.Take(lastWhen),
                        coalesceInvocation);

                    return TransformCoalesce(simplifiendCase);
                }
                return expression;
            }
            return expression;
        }
    }
}
