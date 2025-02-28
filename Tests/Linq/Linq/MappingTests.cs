﻿using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;
#if NET472
	using System.ServiceModel;
#endif

	[TestFixture]
	public class MappingTests : TestBase
	{
		[Test]
		public void Enum1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { Gender.Male }.Contains(p.Gender) select p,
					from p in db.Person where new[] { Gender.Male }.Contains(p.Gender) select p);
		}

		[Test]
		public void Enum2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender == Gender.Male select p,
					from p in db.Person where p.Gender == Gender.Male select p);
		}

		[Test]
		public void Enum21([DataSources] string context)
		{
			var gender = Gender.Male;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender == gender select p,
					from p in db.Person where p.Gender == gender select p);
		}

		[Test]
		public void Enum3([DataSources] string context)
		{
			var fm = Gender.Female;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender != fm select p,
					from p in db.Person where p.Gender != fm select p);
		}

		[Test]
		public void Enum4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4 where p.Value1 == TypeValue.Value1 select p,
					from p in db.Parent4 where p.Value1 == TypeValue.Value1 select p);
		}

		[Test]
		public void EnumValue1()
		{
			var value = ConvertTo<TypeValue>.From(1);

			Assert.AreEqual(TypeValue.Value1, value);
			Assert.AreEqual(10,               (int)value);
		}

		[Test]
		public void Enum5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4 where p.Value1 == TypeValue.Value3 select p,
					from p in db.Parent4 where p.Value1 == TypeValue.Value3 select p);
		}

		[Test]
		public void Enum6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4
					join c in    Child on p.ParentID equals c.ParentID
					where p.Value1 == TypeValue.Value1 select p,
					from p in db.Parent4
					join c in db.Child on p.ParentID equals c.ParentID
					where p.Value1 == TypeValue.Value1 select p);
		}

		[Test]
		public void Enum7([DataSources] string context)
		{
			var v1 = TypeValue.Value1;

			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();
				db.Parent4.Update(p => p.Value1 == v1, p => new Parent4 { Value1 = v1 });
			}
		}

		public enum TestValue
		{
			Value1 = 1,
		}

		[Table("Parent")]
		sealed class TestParent
		{
			[Column] public int       ParentID;
			[Column] public TestValue Value1;
		}

		[Test]
		public void Enum81([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<TestParent>().Where(p => p.Value1 == TestValue.Value1).ToList();
		}

		internal sealed class LinqDataTypes
		{
			public TestValue ID;
		}

		[Test]
		public void Enum812([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<LinqDataTypes>()
					.Where(p => p.ID == TestValue.Value1)
					.Count();
		}

		[Test]
		public void Enum82([DataSources] string context)
		{
			var testValue = TestValue.Value1;
			using (var db = GetDataContext(context))
				db.GetTable<TestParent>().Where(p => p.Value1 == testValue).ToList();
		}

		public enum Gender9
		{
			[MapValue("M")] Male,
			[MapValue("F")] Female,
			[MapValue("U")] Unknown,
			[MapValue("O")] Other,
		}

		[Table("Person", IsColumnAttributeRequired=false)]
		public class Person9
		{
			public int     PersonID;
			public string  FirstName = null!;
			public string  LastName = null!;
			public string? MiddleName;
			public Gender9 Gender;
		}

		[Test]
		public void Enum9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Person9>().Where(p => p.PersonID == 1 && p.Gender == Gender9.Male).ToList();
		}

		[Table("Parent")]
		public class ParentObject
		{
			[Column]                      public int   ParentID;
			[Column("Value1", ".Value1")] public Inner Value = new ();

			public class Inner
			{
				public int? Value1;
			}
		}

		[Test]
		public void Inner1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1);
				Assert.AreEqual(1, e.ParentID);
				Assert.AreEqual(1, e.Value.Value1);
			}
		}

		[Test]
		public void Inner2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1 && p.Value.Value1 == 1);
				Assert.AreEqual(1, e.ParentID);
				Assert.AreEqual(1, e.Value.Value1);
			}
		}

		[Table(Name="Child")]
		public class ChildObject
		{
			[Column] public int ParentID;
			[Column] public int ChildID;

			[Association(ThisKey="ParentID", OtherKey="ParentID")]
			public ParentObject? Parent;
		}

		[Test]
		public void Inner3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ChildObject>().First(c => c.Parent!.Value.Value1 == 1);
				Assert.AreEqual(1, e.ParentID);
			}
		}

		struct MyInt
		{
			public int MyValue;
		}

		[Table(Name="Parent")]
		sealed class MyParent
		{
			[Column] public MyInt ParentID;
			[Column] public int?  Value1;
		}

		sealed class MyMappingSchema : MappingSchema
		{
			public MyMappingSchema()
			{
				SetConvertExpression<long,MyInt>         (n => new MyInt { MyValue = (int)n });
				SetConvertExpression<int,MyInt>          (n => new MyInt { MyValue =      n });
				SetConvertExpression<MyInt,DataParameter>(n => new DataParameter { Value = n.MyValue });
			}
		}

		static readonly MyMappingSchema _myMappingSchema = new ();

		[Test]
		public void MyType1()
		{
			using (var db = new TestDataConnection().AddMappingSchema(_myMappingSchema))
			{
				var _ = db.GetTable<MyParent>().ToList();
			}
		}

		[Test]
		public void MyType2()
		{
			using (var db = new TestDataConnection().AddMappingSchema(_myMappingSchema))
			{
				var _ = db.GetTable<MyParent>()
					.Select(t => new MyParent { ParentID = t.ParentID, Value1 = t.Value1 })
					.ToList();
			}
		}

		[Test]
		public void MyType3()
		{
			using (var db = (TestDataConnection) new TestDataConnection().AddMappingSchema(_myMappingSchema))
			{
				try
				{
					db.Insert(new MyParent { ParentID = new MyInt { MyValue = 1001 }, Value1 = 1001 });
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void MyType4()
		{
			using (var db = (TestDataConnection)new TestDataConnection().AddMappingSchema(_myMappingSchema))
			{
				try
				{
					var id = new MyInt { MyValue = 1001 };
					db.GetTable<MyParent>().Insert(() => new MyParent { ParentID = id, Value1 = 1001 });
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID >= 1000);
				}
			}
		}

		[Test]
		public void MyType5()
		{
			using (var db = (TestDataConnection)new TestDataConnection().AddMappingSchema(_myMappingSchema))
			{
				try
				{
					db.GetTable<MyParent>().Insert(() => new MyParent { ParentID = new MyInt { MyValue = 1001 }, Value1 = 1001 });
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID >= 1000);
				}
			}
		}

		[Table("Parent")]
		sealed class MyParent1
		{
			[Column] public int  ParentID;
			[Column] public int? Value1;

			public string Value2 { get { return "1"; } }

			public int GetValue() { return 2; }
		}

		[Test]
		public void MapIgnore1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID,   Value2 = "1" }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, p.Value2 }));
		}

		[Test]
		public void MapIgnore2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID,          Length = 1 }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, p.Value2.Length }));
		}

		[Test]
		public void MapIgnore3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID, Value = 2            }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, Value = p.GetValue() }));
		}

		public class     Entity    { public int Id { get; set; } }
		public interface IDocument { int Id { get; set; } }
		public class     Document : Entity, IDocument { }

		[Test]
		public void TestMethod()
		{
			using (var db = new TestDataConnection())
			{
				IQueryable<IDocument> query = db.GetTable<Document>();
				var idsQuery = query.Select(s => s.Id);
				var str = idsQuery.ToString(); // Exception
				Assert.IsNotNull(str);
			}
		}

		[Table("Person")]
		sealed class Table171
		{
			[Column] public Gender Gender;
		}

		[Test]
		public void Issue171Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			db.GetTable<Table171>()
				.Where (t => t.Gender == Gender.Male)
				.Select(t => new { value = (int)t.Gender })
				.ToList();
		}

		[Table("Child")]
		interface IChild
		{
			[Column]
			int ChildID { get; set; }
		}

		[Test]
		public void TestInterfaceMapping1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = db.GetTable<IChild>().Where(c => c.ChildID == 32).Count();

				Assert.AreEqual(1, results);
			}
		}

		[Test]
		public void TestInterfaceMapping2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = db.GetTable<IChild>().Where(c => c.ChildID == 32).Select(_ => new { _.ChildID }).ToList();

				Assert.AreEqual(1, results.Count);
				Assert.AreEqual(32, results[0].ChildID);
			}
		}

		[Table("Person")]
		public class BadMapping
		{
			[Column("FirstName")]
			public int NotInt { get; set; }

			[Column("LastName")]
			public BadEnum BadEnum { get; set; }
		}

		public enum BadEnum
		{
			[MapValue("SOME_VALUE")]
			Value = 1
		}

		[Test]
		public void ColumnMappingException1([DataSources] string context)
		{
			GetProviderName(context, out var isLinqService);

			using (var db = GetDataContext(context, testLinqService : false, suppressSequentialAccess: true))
			{
				if (isLinqService)
				{
#if NETFRAMEWORK
					var fe = Assert.Throws<FaultException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
					Assert.True(fe.Message.ToLowerInvariant().Contains("firstname"));
#else
					var fe = Assert.Throws<Grpc.Core.RpcException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
					Assert.True(fe.Message.ToLowerInvariant().Contains("firstname"));
#endif
				}
				else
				{
					var ex = Assert.Throws<LinqToDBConvertException>(() => db.GetTable<BadMapping>().Select(_ => new { _.NotInt }).ToList())!;
					// field name casing depends on database
					Assert.AreEqual("firstname", ex.ColumnName!.ToLowerInvariant());
				}
			}
		}

		[Test]
		public void ColumnMappingException2([DataSources] string context)
		{
			GetProviderName(context, out var isLinqService);

			using (var db = GetDataContext(context, suppressSequentialAccess: true))
			{
				var ex = Assert.Throws<LinqToDBConvertException>(() => db.GetTable<BadMapping>().Select(_ => new { _.BadEnum }).ToList())!;
				Assert.AreEqual("lastname", ex.ColumnName!.ToLowerInvariant());
			}
		}

		[Test, ActiveIssue(1592)]
		public void Issue1592CallbackWithDefaultMappingSchema([DataSources] string context)
		{
			bool result = false;

			MappingSchema.Default.EntityDescriptorCreatedCallback = (ms, ed) =>
			{
				result = true;
			};

			using (var db = GetDataContext(context))
			{
				db.GetTable<Person>().FirstOrDefault();

				Assert.IsTrue(result);
			}
		}

		[ActiveIssue(1592)]
		[Test]
		public void Issue1592CallbackWithContextProperty([DataSources] string context)
		{
			bool result = false;

			using (var db = GetDataContext(context))
			{
				db.MappingSchema.EntityDescriptorCreatedCallback = (ms, ed) =>
				{
					result = true;
				};

				db.GetTable<Person>().FirstOrDefault();

				Assert.IsTrue(result);
			}
		}

		[Test, ActiveIssue(1592)]
		public void Issue1592CallbackWithContextConstructor([DataSources] string context)
		{
			bool result = false;

			var mappingSchema = new MappingSchema
			{
				EntityDescriptorCreatedCallback = (ms, ed) =>
				{
					result = true;
				}
			};

			using (var db = GetDataContext(context, mappingSchema))
			{
				db.GetTable<Person>().FirstOrDefault();

				Assert.IsTrue(result);
			}
		}

#region Records

		public record Record(int Id, string Value, string BaseValue) : RecordBase(Id, BaseValue);
		public abstract record RecordBase(int Id, string BaseValue);

		public class RecordLike : RecordLikeBase
		{
			public RecordLike(int Id, string Value, string BaseValue)
				: base(Id, BaseValue)
			{
				this.Value = Value;
			}

			public string Value { get; init; }
		}

		public abstract class RecordLikeBase
		{
			public RecordLikeBase(int Id, string BaseValue)
			{
				this.Id = Id;
				this.BaseValue = BaseValue;
			}

			public int    Id        { get; init; }
			public string BaseValue { get; init; }
		}

		public class WithInitOnly : WithInitOnlyBase
		{
			public string? Value { get; init; }
		}

		public abstract class WithInitOnlyBase
		{
			public int     Id        { get; init; }
			public string? BaseValue { get; init; }
		}

		[Test]
		public void TestRecordMapping([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			ms.GetFluentMappingBuilder().Entity<Record>()
				.Property(p => p.Id).IsPrimaryKey()
				.Property(p => p.Value)
				.Property(p => p.BaseValue);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<Record>())
			{
				db.Insert(new Record(1, "One", "OneBase"));
				db.Insert(new Record(2, "Two", "TwoBase"));

				var data = table.OrderBy(r => r.Id).ToArray();

				Assert.AreEqual(2        , data.Length);
				Assert.AreEqual(1        , data[0].Id);
				Assert.AreEqual("One"    , data[0].Value);
				Assert.AreEqual("OneBase", data[0].BaseValue );
				Assert.AreEqual(2        , data[1].Id);
				Assert.AreEqual("Two"    , data[1].Value);
				Assert.AreEqual("TwoBase", data[1].BaseValue);

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.AreEqual(2        , proj.Length);
				Assert.AreEqual(1        , proj[0].Id);
				Assert.AreEqual("One"    , proj[0].Value);
				Assert.AreEqual("OneBase", proj[0].BaseValue );
				Assert.AreEqual(2        , proj[1].Id);
				Assert.AreEqual("Two"    , proj[1].Value);
				Assert.AreEqual("TwoBase", proj[1].BaseValue);
			}
		}

		[Test]
		public void TestRecordLikeMapping([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			ms.GetFluentMappingBuilder().Entity<RecordLike>()
				.Property(p => p.Id).IsPrimaryKey()
				.Property(p => p.Value)
				.Property(p => p.BaseValue);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<RecordLike>())
			{
				db.Insert(new RecordLike(1, "One", "OneBase"));
				db.Insert(new RecordLike(2, "Two", "TwoBase"));

				var data = table.OrderBy(r => r.Id).ToArray();

				Assert.AreEqual(2        , data.Length);
				Assert.AreEqual(1        , data[0].Id);
				Assert.AreEqual("One"    , data[0].Value);
				Assert.AreEqual("OneBase", data[0].BaseValue );
				Assert.AreEqual(2        , data[1].Id);
				Assert.AreEqual("Two"    , data[1].Value);
				Assert.AreEqual("TwoBase", data[1].BaseValue);

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.AreEqual(2        , proj.Length);
				Assert.AreEqual(1        , proj[0].Id);
				Assert.AreEqual("One"    , proj[0].Value);
				Assert.AreEqual("OneBase", proj[0].BaseValue );
				Assert.AreEqual(2        , proj[1].Id);
				Assert.AreEqual("Two"    , proj[1].Value);
				Assert.AreEqual("TwoBase", proj[1].BaseValue);
			}
		}

		[Test]
		public void TestInitOnly([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			ms.GetFluentMappingBuilder().Entity<WithInitOnly>()
				.Property(p => p.Id).IsPrimaryKey()
				.Property(p => p.Value);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<WithInitOnly>())
			{
				db.Insert(new WithInitOnly{Id = 1, Value = "One", BaseValue = "OneBase"});
				db.Insert(new WithInitOnly{Id = 2, Value = "Two", BaseValue = "TwoBase"});

				var data = table.OrderBy(r => r.Id).ToArray();

				Assert.AreEqual(2        , data.Length);
				Assert.AreEqual(1        , data[0].Id);
				Assert.AreEqual("One"    , data[0].Value);
				Assert.AreEqual("OneBase", data[0].BaseValue );
				Assert.AreEqual(2        , data[1].Id);
				Assert.AreEqual("Two"    , data[1].Value);
				Assert.AreEqual("TwoBase", data[1].BaseValue);

				var proj = table.OrderBy(r => r.Id).Select(r => new { r.Id, r.Value, r.BaseValue }).ToArray();

				Assert.AreEqual(2        , proj.Length);
				Assert.AreEqual(1        , proj[0].Id);
				Assert.AreEqual("One"    , proj[0].Value);
				Assert.AreEqual("OneBase", proj[0].BaseValue );
				Assert.AreEqual(2        , proj[1].Id);
				Assert.AreEqual("Two"    , proj[1].Value);
				Assert.AreEqual("TwoBase", proj[1].BaseValue);
			}
		}

#endregion
	}
}
