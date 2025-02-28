﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class ExtensionChoiceAttribute : Attribute
	{
		public ExtensionChoiceAttribute(string configuration, string expression, params Type?[] types)
		{
			Configuration = configuration;
			Expression    = expression;
			Types         = types ?? throw new ArgumentNullException(nameof(types));
		}

		public string  Configuration { get; set; }
		public string  Expression    { get; set; }
		public Type?[] Types         { get; }
	}

	sealed class GenericBuilder : Sql.IExtensionCallBuilder
	{
		string Match(Type[] current, ExtensionChoiceAttribute[] choices)
		{
			var found = new List<ExtensionChoiceAttribute>();
			foreach (var c in choices)
			{
				var notMatched = false;
				for (int i = 0; i < Math.Min(current.Length, c.Types.Length); i++)
				{
					if (c.Types[i] != null && c.Types[i] != current[i])
					{
						notMatched = true;
						break;
					}
				}

				if (!notMatched)
					found.Add(c);
			}

			if (found.Count > 1)
				found = found.Where(f => f.Types.Any(t => t != null)).ToList();

			if (found.Count == 0)
				throw new InvalidOperationException("Can not deduce pattern for types sequence: " +
				                                    string.Join(", ", current.Select(t => t.Name)));

			if (found.Count > 1)
				throw new InvalidOperationException("Ambiguous patterns found:\n" +
				                                    string.Join("\n",
					                                    found.Select(f => string.Join(", ",
						                                    f.Types.Select(t => t == null ? "null" : t.Name)))));
			return found[0].Expression;
		}

		public void Build(Sql.ISqExtensionBuilder builder)
		{
			var method = builder.Member as MethodInfo;

			if (method != null && method.IsGenericMethod)
			{
				var typeParameters = method.GetGenericArguments();
				builder.Expression = Match(typeParameters,
					builder.Mapping.GetAttributes<ExtensionChoiceAttribute>(builder.Member.DeclaringType!, method, a => a.Configuration));
			}
			else
				throw new InvalidOperationException("This extension could be applied only to methods with type parameters.");
		}
	}

	public static class GenericExtensionsFunctions
	{
		[Sql.Extension(typeof(GenericBuilder))]
		[ExtensionChoice("", "'T1=UNSUPPORTED PARAMETERS'",                                                                                                             null,           null          )]
		[ExtensionChoice("", "'T2=(BYTE: ' + CASE WHEN {second} IS NULL THEN 'null' ELSE CAST({second} AS NVARCHAR) END + ')'",                                         null,           typeof(byte?) )]
		[ExtensionChoice("", "'T3=(BYTE: ' + CAST({first} AS NVARCHAR) + ', INT: ' + CASE WHEN {second} IS NULL THEN 'null' ELSE CAST({second} AS NVARCHAR) END + ')'", typeof(byte),   typeof(int?)  )]
		[ExtensionChoice("", "'T4=(BYTE: ' + CAST({first} AS NVARCHAR) + ', INT: ' + CAST({second} AS NVARCHAR) + ')'",                                                 typeof(byte),   typeof(int)   )]
		[ExtensionChoice("", "'T5=(CHAR: ' + CASE WHEN {first} IS NULL THEN 'null' ELSE CAST({first} AS NVARCHAR) END + ', STRING: ' + {second} + ')'",                 typeof(char?),  typeof(string))]
		public static string TestGenericExpression<TFirstValue, TSecondValue>(this Sql.ISqlExtension? ext,
			[ExprParameter("first")]  TFirstValue value,
			[ExprParameter("second")] TSecondValue secondValue)
		{
			throw new InvalidOperationException("Server-side call failed");
		}
	}

	sealed class GenericExtensionTests : TestBase
	{
		[Test]
		public void Issue326([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Parent>()
					.Select(_ => new
					{
						R1 = Sql.Ext.TestGenericExpression<char?, string>('X', "some string"),
						R2 = Sql.Ext.TestGenericExpression<char?, string>(null, "another string"),
						R3 = Sql.Ext.TestGenericExpression<char?, string?>(null, null),

						R4 = Sql.Ext.TestGenericExpression<byte, int?>(123, 456),
						R5 = Sql.Ext.TestGenericExpression<byte, int?>(123, null),

						R6 = Sql.Ext.TestGenericExpression<byte, int>(123, 456),

						R7 = Sql.Ext.TestGenericExpression<long, byte?>(123, null),
						R8 = Sql.Ext.TestGenericExpression<short, byte?>(123, 45),

						R9 = Sql.Ext.TestGenericExpression<byte, long>(123, 45)
					}).First();

				Assert.AreEqual("T5=(CHAR: X, STRING: some string)", result.R1);
				Assert.AreEqual("T5=(CHAR: null, STRING: another string)", result.R2);
				Assert.AreEqual(null, result.R3);
				Assert.AreEqual("T3=(BYTE: 123, INT: 456)", result.R4);
				Assert.AreEqual("T3=(BYTE: 123, INT: null)", result.R5);
				Assert.AreEqual("T4=(BYTE: 123, INT: 456)", result.R6);
				Assert.AreEqual("T2=(BYTE: null)", result.R7);
				Assert.AreEqual("T2=(BYTE: 45)", result.R8);
				Assert.AreEqual("T1=UNSUPPORTED PARAMETERS", result.R9);
			}
		}
	}
}
