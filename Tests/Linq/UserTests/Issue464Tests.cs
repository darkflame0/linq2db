﻿using System.Globalization;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	using System.Linq;

	using LinqToDB.DataProvider.Firebird;

	[TestFixture]
	public class Issue464Tests : TestBase
	{

		[Test]
		public void Test([DataSources(false)] string context)
		{
			var schema = new MappingSchema();

			schema.SetDataType(typeof(MyInt), DataType.Int32);

			schema.SetConvertExpression<MyInt,   int>          (x => x.Value);
			schema.SetConvertExpression<int,     MyInt>        (x => new MyInt { Value = x });
			schema.SetConvertExpression<long,    MyInt>        (x => new MyInt { Value = (int)x }); //SQLite
			schema.SetConvertExpression<string,  MyInt>        (x => new MyInt { Value = int.Parse(x, CultureInfo.InvariantCulture) }); //ClickHouse.MySql
			schema.SetConvertExpression<decimal, MyInt>        (x => new MyInt { Value = (int)x }); //Oracle
			schema.SetConvertExpression<MyInt,   DataParameter>(x => new DataParameter { DataType = DataType.Int32, Value = x.Value });

			schema.GetFluentMappingBuilder()
				  .Entity<Entity>()
				  .HasTableName("Issue464")
				  .HasColumn(x => x.Id)
				  .HasColumn(x => x.Value);

			using (var db = new  DataConnection(context).AddMappingSchema(schema))
			using (new FirebirdQuoteMode(FirebirdIdentifierQuoteMode.Auto))
			{
				try
				{
					var temptable = db.CreateTable<Entity>();

					var data = new[]
					{
						new Entity {Id = 1, Value = new MyInt {Value = 1}},
						new Entity {Id = 2, Value = new MyInt {Value = 2}},
						new Entity {Id = 3, Value = new MyInt {Value = 3}}
					};

					temptable.BulkCopy(GetDefaultBulkCopyOptions(context), data);

					AreEqual(data, temptable.ToList());
				}
				finally
				{
					db.DropTable<Entity>();
				}

			}
		}

		public class Entity
		{
			public int    Id    { get; set; }
			public MyInt? Value { get; set; }

			public override bool Equals(object? obj)
			{
				return obj is Entity e
					&& Id == e.Id
					&& Value!.Value == Id;
			}

			public override int GetHashCode()
			{
				return Id;
			}
		}

		public class MyInt
		{
			public int Value { get; set; }
		}
	}
}
