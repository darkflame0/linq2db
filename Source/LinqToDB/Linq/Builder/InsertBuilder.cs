﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Common;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using SqlQuery;
	using LinqToDB.Expressions;

	sealed class InsertBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames = new []
		{
			nameof(LinqExtensions.Insert),
			nameof(LinqExtensions.InsertWithIdentity),
			nameof(LinqExtensions.InsertWithOutput),
			nameof(LinqExtensions.InsertWithOutputInto)
		};

		#region InsertBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		static void AddInsertColumns(SelectQuery selectQuery, List<SqlSetExpression> items)
		{
			foreach (var item in items)
			{
				if (item.Expression is SqlColumn column)
				{
					if (column.Parent == selectQuery)
					{
						if (selectQuery.Select.Columns.IndexOf(column) < 0)
							selectQuery.Select.Columns.Add(column);
						continue;
					}
				}
				selectQuery.Select.ExprNew(item.Expression!);
			}
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var isSubQuery = sequence.SelectQuery.Select.IsDistinct;

			if (isSubQuery)
				sequence = new SubQueryContext(sequence);

			if (!(sequence.Statement is SqlInsertStatement insertStatement))
			{
				insertStatement    = new SqlInsertStatement(sequence.SelectQuery);
				sequence.Statement = insertStatement;
			}

			var insertType = InsertContext.InsertType.Insert;

			switch (methodCall.Method.Name)
			{
				case nameof(LinqExtensions.Insert)                : insertType = InsertContext.InsertType.Insert;             break;
				case nameof(LinqExtensions.InsertWithIdentity)    : insertType = InsertContext.InsertType.InsertWithIdentity; break;
				case nameof(LinqExtensions.InsertWithOutput)      : insertType = InsertContext.InsertType.InsertOutput;       break;
				case nameof(LinqExtensions.InsertWithOutputInto)  : insertType = InsertContext.InsertType.InsertOutputInto;   break;
			}

			static LambdaExpression BuildDefaultOutputExpression(Type outputType)
			{
				var param = Expression.Parameter(outputType);
				return Expression.Lambda(param, param);
			}

			IBuildContext?    outputContext    = null;
			LambdaExpression? outputExpression = null;

			if (methodCall.Arguments.Count > 0)
			{
				var argument = methodCall.Arguments[0];
				if (typeof(IValueInsertable<>).IsSameOrParentOf(argument.Type) ||
				    typeof(ISelectInsertable<,>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<T>              (this IValueInsertable<T> source)
					// static int Insert<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)

					sequence.SelectQuery.Select.Columns.Clear();

					if (insertStatement.Insert.Items.Count == 0)
						insertStatement.Insert.Items.AddRange(insertStatement.Insert.DefaultItems);

					AddInsertColumns(sequence.SelectQuery, insertStatement.Insert.Items);
				}
				else if (methodCall.Arguments.Count > 1                  &&
					typeof(IQueryable<>).IsSameOrParentOf(argument.Type) &&
					typeof(ITable<>).IsSameOrParentOf(methodCall.Arguments[1].Type))
				{
					// static int Insert<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)

					var into = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));
					var setter = (LambdaExpression)methodCall.GetArgumentByName("setter")!.Unwrap();

					UpdateBuilder.BuildSetter(
						builder,
						buildInfo,
						setter,
						into,
						insertStatement.Insert.Items,
						sequence);

					sequence.SelectQuery.Select.Columns.Clear();

					if (insertStatement.Insert.Items.Count == 0)
						insertStatement.Insert.Items.AddRange(insertStatement.Insert.DefaultItems);

					AddInsertColumns(sequence.SelectQuery, insertStatement.Insert.Items);

					insertStatement.Insert.Into = ((TableBuilder.TableContext)into).SqlTable;
				}
				else if (typeof(ITable<>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<T>(this Table<T> target, Expression<Func<T>> setter)
					// static TTarget InsertWithOutput<TTarget>(this ITable<TTarget> target, Expression<Func<TTarget>> setter)
					// static TTarget InsertWithOutput<TTarget>(this ITable<TTarget> target, Expression<Func<TTarget>> setter, Expression<Func<TTarget,TOutput>> outputExpression)
					var argIndex = 1;
					var arg = methodCall.Arguments[argIndex].Unwrap();
					LambdaExpression? setter = null;
					switch (arg)
					{
						case LambdaExpression lambda:
							{
								setter = lambda;

								UpdateBuilder.BuildSetter(
									builder,
									buildInfo,
									setter,
									sequence,
									insertStatement.Insert.Items,
									sequence);

								break;
							}
						default:
							{
								var objType = arg.Type;

								var ed   = builder.MappingSchema.GetEntityDescriptor(objType);
								var into = sequence;
								var ctx  = new TableBuilder.TableContext(builder, buildInfo, objType);

								var table = new SqlTable(objType);

								foreach (var c in ed.Columns.Where(c => !c.SkipOnInsert))
								{
									var field     = table[c.MemberName] ?? throw new InvalidOperationException($"Cannot find column {c.MemberName}({c.ColumnName})");
									var pe        = Expression.MakeMemberAccess(arg, c.MemberInfo);
									var column    = into.ConvertToSql(pe, 1, ConvertFlags.Field);
									var parameter = builder.ParametersContext.BuildParameterFromArgumentProperty(methodCall, argIndex, field.ColumnDescriptor);

									insertStatement.Insert.Items.Add(new SqlSetExpression(column[0].Sql, parameter.SqlParameter));
								}

								break;
							}
					}

					insertStatement.Insert.Into = ((TableBuilder.TableContext)sequence).SqlTable;
					sequence.SelectQuery.From.Tables.Clear();
				}

				if (insertType == InsertContext.InsertType.InsertOutput || insertType == InsertContext.InsertType.InsertOutputInto)
				{
					outputExpression =
						(LambdaExpression?)methodCall.GetArgumentByName("outputExpression")?.Unwrap()
						?? BuildDefaultOutputExpression(methodCall.Method.GetGenericArguments().Last());

					insertStatement.Output = new SqlOutputClause();

					var insertedTable = builder.DataContext.SqlProviderFlags.OutputInsertUseSpecialTable ? SqlTable.Inserted(outputExpression.Parameters[0].Type) : insertStatement.Insert.Into;

					if (insertedTable == null)
						throw new InvalidOperationException("Cannot find target table for INSERT statement");

					outputContext = new TableBuilder.TableContext(builder, new SelectQuery(), insertedTable);

					if (builder.DataContext.SqlProviderFlags.OutputInsertUseSpecialTable)
						insertStatement.Output.InsertedTable = insertedTable;

					if (insertType == InsertContext.InsertType.InsertOutputInto)
					{
						var outputTable = methodCall.GetArgumentByName("outputTable")!;
						var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));

						UpdateBuilder.BuildSetter(
							builder,
							buildInfo,
							outputExpression,
							destination,
							insertStatement.Output.OutputItems,
							outputContext);

						insertStatement.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;
					}
				}
			}

			var insert = insertStatement.Insert;

			if (insert.Into == null)
				throw new LinqToDBException("Insert query has no setters defined.");

			var q = insert.Into.IdentityFields
				.Except(insert.Items.Select(e => e.Column).OfType<SqlField>());

			foreach (var field in q)
			{
				var expr = builder.DataContext.CreateSqlProvider().GetIdentityExpression(insert.Into);

				if (expr != null)
				{
					insert.Items.Insert(0, new SqlSetExpression(field, expr));

					if (methodCall.Arguments.Count == 3)
					{
						sequence.SelectQuery.Select.Columns.Insert(0, new SqlColumn(sequence.SelectQuery, insert.Items[0].Expression!));
					}
				}
			}

			insertStatement.Insert.WithIdentity = insertType == InsertContext.InsertType.InsertWithIdentity;
			sequence.Statement = insertStatement;

			if (insertType == InsertContext.InsertType.InsertOutput)
				return new InsertWithOutputContext(buildInfo.Parent, sequence, outputContext!, outputExpression!);

			return new InsertContext(buildInfo.Parent, sequence, insertType, outputExpression);
		}

		#endregion

		#region InsertContext

		sealed class InsertContext : SequenceContextBase
		{
			public enum InsertType
			{
				Insert,
				InsertWithIdentity,
				InsertOutput,
				InsertOutputInto
			}

			public InsertContext(IBuildContext? parent, IBuildContext sequence, InsertType insertType, LambdaExpression? outputExpression)
				: base(parent, sequence, outputExpression)
			{
				_insertType       = insertType;
				_outputExpression = outputExpression;
			}

			readonly InsertType        _insertType;
			readonly LambdaExpression? _outputExpression;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				switch (_insertType)
				{
					case InsertType.Insert:
						QueryRunner.SetNonQueryQuery(query);
						break;
					case InsertType.InsertWithIdentity:
						QueryRunner.SetScalarQuery(query);
						break;
					case InsertType.InsertOutput:
						//TODO:
						var mapper = Builder.BuildMapper<T>(_outputExpression!.Body.Unwrap());
						QueryRunner.SetRunQuery(query, mapper);
						break;
					case InsertType.InsertOutputInto:
						QueryRunner.SetNonQueryQuery(query);
						break;
					default:
						throw new InvalidOperationException($"Unexpected insert type: {_insertType}");
				}
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region InsertWithOutputContext

		sealed class InsertWithOutputContext : SelectContext
		{
			public InsertWithOutputContext(IBuildContext? parent, IBuildContext sequence, IBuildContext outputContext, LambdaExpression outputExpression)
				: base(parent, outputExpression, outputContext)
			{
				Statement = sequence.Statement;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				var insertStatement = (SqlInsertStatement)Statement!;
				var outputQuery     = Sequence[0].SelectQuery;

				insertStatement.Output!.OutputColumns = outputQuery.Select.Columns.Select(c => c.Expression).ToList();

				QueryRunner.SetRunQuery(query, mapper);
			}
		}

		#endregion

		#region Into

		internal sealed class Into : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsQueryable("Into");
			}

			public static List<Tuple<SqlInfo, SqlInfo>> MatchSequences(IBuildContext source, IBuildContext destination)
			{
				var sourceInfos = source.ConvertToSql(null, 0, ConvertFlags.All).ToList();
				var destInfos   = destination.ConvertToSql(null, 0, ConvertFlags.All).ToList();

				var result = new List<Tuple<SqlInfo, SqlInfo>>();

				foreach (var info in sourceInfos)
				{
					if (info.MemberChain.Length == 0)
						continue;

					var destInfo = destInfos.FirstOrDefault(info.CompareMembers);

					if (destInfo != null)
						result.Add(Tuple.Create(info, destInfo));
				}

				return result;
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var source = methodCall.Arguments[0].Unwrap();
				var into   = methodCall.Arguments[1].Unwrap();

				IBuildContext sequence;
				IBuildContext destinationSequence;
				SqlInsertStatement insertStatement;

				// static IValueInsertable<T> Into<T>(this IDataContext dataContext, Table<T> target)
				//
				if (source.IsNullValue())
				{
					sequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, into, new SelectQuery()));
					destinationSequence = sequence;

					if (sequence.SelectQuery.Select.IsDistinct)
						sequence = new SubQueryContext(sequence);

					insertStatement = new SqlInsertStatement(sequence.SelectQuery);
					insertStatement.Insert.Into = ((TableBuilder.TableContext)sequence).SqlTable;
					insertStatement.SelectQuery.From.Tables.Clear();
				}
				// static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target)
				//
				else
				{
					sequence = builder.BuildSequence(new BuildInfo(buildInfo, source));
					destinationSequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, into, new SelectQuery()));

					if (sequence.SelectQuery.Select.IsDistinct)
						sequence = new SubQueryContext(sequence);

					var destinationTable = ((TableBuilder.TableContext)destinationSequence).SqlTable;

					insertStatement = new SqlInsertStatement(sequence.SelectQuery);
					insertStatement.Insert.Into = destinationTable;
				}

				// generating default items
				var matched = MatchSequences(sequence, destinationSequence);
				foreach (var tuple in matched)
				{
					var field = QueryHelper.GetUnderlyingField(tuple.Item2.Sql);
					if (field == null || field.ColumnDescriptor.SkipOnInsert)
						continue;
					insertStatement.Insert.DefaultItems.Add(new SqlSetExpression(field, tuple.Item1.Sql));
				}

				sequence.Statement = insertStatement;
				sequence.SelectQuery.Select.Columns.Clear();

				return sequence;
			}
		}

		#endregion

		#region Value

		internal sealed class Value : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsQueryable("Value");
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
				var extract  = (LambdaExpression)methodCall.Arguments[1].Unwrap();
				var update   =                   methodCall.Arguments[2].Unwrap();

				if (!(sequence.Statement is SqlInsertStatement insertStatement))
				{
					insertStatement    = new SqlInsertStatement(sequence.SelectQuery);
					sequence.Statement = insertStatement;
				}

				if (insertStatement.Insert.Into == null)
				{
					insertStatement.Insert.Into = (SqlTable)sequence.SelectQuery.From.Tables[0].Source;
					insertStatement.SelectQuery.From.Tables.Clear();
				}

				if (update.NodeType == ExpressionType.Lambda)
				{
					var fieldsContext = new TableBuilder.TableContext(builder, new SelectQuery(), insertStatement.Insert.Into);
					UpdateBuilder.ParseSet(
						builder,
						buildInfo,
						extract,
						(LambdaExpression)update,
						fieldsContext,
						sequence,
						insertStatement.Insert.Into,
						insertStatement.Insert.Items);
				}
				else
					UpdateBuilder.ParseSet(
						builder,
						extract,
						methodCall,
						2,
						sequence,
						insertStatement.Insert.Items);

				// why we even do it?
				// TODO: remove in v4?
				insertStatement.Insert.Items.RemoveDuplicatesFromTail((s1, s2) => s1.Column.Equals(s2.Column));

				return sequence;
			}
		}

		#endregion
	}
}
