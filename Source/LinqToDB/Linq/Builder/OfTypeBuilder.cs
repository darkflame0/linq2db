﻿using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;

	sealed class OfTypeBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("OfType");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence is TableBuilder.TableContext table
				&& table.InheritanceMapping.Count > 0)
			{
				var objectType = methodCall.Type.GetGenericArguments()[0];

				if (table.ObjectType.IsSameOrParentOf(objectType))
				{
					var predicate = builder.MakeIsPredicate(table, objectType);

					if (predicate.GetType() != typeof(SqlPredicate.Expr))
						sequence.SelectQuery.Where.SearchCondition.Conditions.Add(new SqlCondition(false, predicate));
				}
			}
			else
			{
				var toType   = methodCall.Type.GetGenericArguments()[0];
				var gargs    = methodCall.Arguments[0].Type.GetGenericArguments(typeof(IQueryable<>));
				var fromType = gargs == null ? typeof(object) : gargs[0];

				if (toType.IsSubclassOf(fromType))
				{
					for (var type = toType.BaseType; type != null && type != typeof(object); type = type.BaseType)
					{
						var mapping = builder.MappingSchema.GetEntityDescriptor(type).InheritanceMapping;

						if (mapping.Count > 0)
						{
							var predicate = MakeIsPredicate(builder, sequence, fromType, toType);

							sequence.SelectQuery.Where.SearchCondition.Conditions.Add(new SqlCondition(false, predicate));

							return new OfTypeContext(sequence, methodCall);
						}
					}
				}
			}

			return sequence;
		}

		static ISqlPredicate MakeIsPredicate(ExpressionBuilder builder, IBuildContext context, Type fromType, Type toType)
		{
			var table          = new SqlTable(builder.MappingSchema, fromType);
			var mapper         = builder.MappingSchema.GetEntityDescriptor(fromType);
			var discriminators = mapper.InheritanceMapping;

			return builder.MakeIsPredicate((context, table), context, discriminators, toType,
				static (context, name) =>
				{
					var field  = context.table[name] ?? throw new LinqException($"Field {name} not found in table {context.table}");
					var member = field.ColumnDescriptor.MemberInfo;
					var expr   = Expression.MakeMemberAccess(Expression.Parameter(member.DeclaringType!, "p"), member);
					var sql    = context.context.ConvertToSql(expr, 1, ConvertFlags.Field)[0].Sql;

					return sql;
				});
		}

		#region OfTypeContext

		sealed class OfTypeContext : PassThroughContext
		{
			public OfTypeContext(IBuildContext context, MethodCallExpression methodCall)
				: base(context)
			{
				_methodCall = methodCall;
			}

			readonly MethodCallExpression _methodCall;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				var expr = base.BuildExpression(expression, level, enforceServerSide);
				var type = _methodCall.Method.GetGenericArguments()[0];

				if (expr.Type != type)
					expr = Expression.Convert(expr, type);

				return expr;
			}
		}

		#endregion
	}
}
