﻿using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.Linq;

namespace LinqToDB.DataProvider.SQLite
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;
	using Common;

	sealed class SQLiteSqlOptimizer : BasicSqlOptimizer
	{
		public SQLiteSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags)
		{
		}

		public override bool CanCompareSearchConditions => true;

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete :
				{
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;
				}

				case QueryType.Update :
				{
					if (SqlProviderFlags.IsUpdateFromSupported)
					{
						statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement);
					}
					else
					{
						statement = GetAlternativeUpdate((SqlUpdateStatement)statement);
					}

					break;
				}
			}

			return statement;
		}

		public override ISqlPredicate ConvertSearchStringPredicate(
			SqlPredicate.SearchString              predicate,
			ConvertVisitor<RunOptimizationContext> visitor)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate, visitor);

			if (predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(string), "Substr", predicate.Expr1, new SqlValue(1),
									new SqlFunction(typeof(int), "Length", predicate.Expr2)),
								SqlPredicate.Operator.Equal,
								predicate.Expr2, null);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(string), "Substr", predicate.Expr1,
									new SqlBinaryExpression(typeof(int),
										new SqlFunction(typeof(int), "Length", predicate.Expr2), "*", new SqlValue(-1),
										Precedence.Multiplicative)
								),
								SqlPredicate.Operator.Equal,
								predicate.Expr2, null);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "InStr", predicate.Expr1, predicate.Expr2),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SqlSearchCondition(
						new SqlCondition(false, like, predicate.IsNot),
						new SqlCondition(predicate.IsNot, subStrPredicate));

					return result;
				}
			}

			return like;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			if (expression is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expression;
					case "^": // (a + b) - (a & b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlBinaryExpression(be.SystemType, be.Expr1, "&", be.Expr2), 2), be.SystemType);
				}
			}
			else if (expression is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Space"   : return new SqlFunction(func.SystemType, "PadR", new SqlValue(" "), func.Parameters[0]);
					case "Convert" :
					{
						var ftype = func.SystemType.ToUnderlying();

						if (ftype == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset)
#if NET6_0_OR_GREATER
							|| ftype == typeof(DateOnly)
#endif
							)
						{
							if (IsDateDataType(func.Parameters[0], "Date"))
								return new SqlFunction(func.SystemType, "Date", func.Parameters[1]);
							return new SqlFunction(func.SystemType, "DateTime", func.Parameters[1]);
						}

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, func.Parameters[1], func.Parameters[0]);
					}
				}
			}

			return expression;
		}

		public override ISqlPredicate ConvertPredicateImpl(ISqlPredicate predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			if (predicate is SqlPredicate.ExprExpr exprExpr)
			{
				var leftType  = QueryHelper.GetDbDataType(exprExpr.Expr1);
				var rightType = QueryHelper.GetDbDataType(exprExpr.Expr2);

				if ((IsDateTime(leftType) || IsDateTime(rightType)) &&
				    !(exprExpr.Expr1.TryEvaluateExpression(visitor. Context.OptimizationContext.Context, out var value1) && value1 == null ||
				      exprExpr.Expr2.TryEvaluateExpression(visitor.Context.OptimizationContext.Context, out var value2) && value2 == null))
				{
					if (!(exprExpr.Expr1 is SqlFunction func1 && (func1.Name == PseudoFunctions.CONVERT || func1.Name == "DateTime")))
					{
						var left = PseudoFunctions.MakeConvert(new SqlDataType(leftType), new SqlDataType(leftType), exprExpr.Expr1);
						exprExpr = new SqlPredicate.ExprExpr(left, exprExpr.Operator, exprExpr.Expr2, null);
					}
					
					if (!(exprExpr.Expr2 is SqlFunction func2 && (func2.Name == PseudoFunctions.CONVERT || func2.Name == "DateTime")))
					{
						var right = PseudoFunctions.MakeConvert(new SqlDataType(rightType), new SqlDataType(rightType), exprExpr.Expr2);
						exprExpr = new SqlPredicate.ExprExpr(exprExpr.Expr1, exprExpr.Operator, right, null);
					}

					predicate = exprExpr;
				}
			}

			predicate = base.ConvertPredicateImpl(predicate, visitor);
			return predicate;
		}


		private static bool IsDateTime(DbDataType dbDataType)
		{
			if (dbDataType.DataType == DataType.Date           ||
				dbDataType.DataType == DataType.Time           ||
				dbDataType.DataType == DataType.DateTime       ||
				dbDataType.DataType == DataType.DateTime2      ||
				dbDataType.DataType == DataType.DateTimeOffset ||
				dbDataType.DataType == DataType.SmallDateTime  ||
				dbDataType.DataType == DataType.Timestamp)
				return true;

			if (dbDataType.DataType != DataType.Undefined)
				return false;

			return IsDateTime(dbDataType.SystemType);
		}

		private static bool IsDateTime(Type type)
		{
			return    type == typeof(DateTime)
			          || type == typeof(DateTimeOffset)
			          || type == typeof(DateTime?)
			          || type == typeof(DateTimeOffset?);
		}

		protected override ISqlExpression ConvertConvertion(SqlFunction func)
		{
			if (!func.DoNotOptimize)
			{
				var from = (SqlDataType)func.Parameters[1];
				var to   = (SqlDataType)func.Parameters[0];

				// prevent same-type conversion removal as it is necessary in case of SQLite
				// to ensure that we get proper type, because converted value could have any type actually
				if (from.Type.EqualsDbOnly(to.Type))
					func.DoNotOptimize = true;
			}

			return base.ConvertConvertion(func);
		}
	}
}
