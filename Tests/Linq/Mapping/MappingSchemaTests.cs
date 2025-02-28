﻿using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

using NUnit.Framework;

namespace Tests.Mapping
{
	public class MappingSchemaTests : TestBase
	{
		[Test]
		public void DefaultValue1()
		{
			var ms = new MappingSchema();

			ms.SetDefaultValue(typeof(int), -1);

			var c = ms.GetConverter<int?,int>()!;

			Assert.AreEqual(-1, c(null));
		}

		[Test]
		public void DefaultValue2()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			ms1.SetConvertExpression<int?,int>(i => i.HasValue ? i.Value * 2 : DefaultValue<int>.Value, false);
			ms2.SetDefaultValue(typeof(int), -1);

			var c1 = ms1.GetConverter<int?,int>()!;
			var c2 = ms2.GetConverter<int?,int>()!;

			Assert.AreEqual( 4, c1(2));
			Assert.AreEqual( 0, c1(null));
			Assert.AreEqual( 4, c2(2));
			Assert.AreEqual(-1, c2(null));
		}

		[Test]
		public void DefaultValue3()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			ms1.SetConvertExpression<int?,int>(i => i!.Value * 2);
			ms2.SetDefaultValue(typeof(int), -1);

			var c1 = ms1.GetConverter<int?,int>()!;
			var c2 = ms2.GetConverter<int?,int>()!;

			Assert.AreEqual( 4, c1(2));
			Assert.AreEqual( 0, c1(null));
			Assert.AreEqual( 4, c2(2));
			Assert.AreEqual(-1, c2(null));
		}

		[Test]
		public void BaseSchema1()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			ms1.SetDefaultValue(typeof(int), -1);
			ms2.SetDefaultValue(typeof(int), -2);

			var c1 = ms1.GetConverter<int?,int>()!;
			var c2 = ms2.GetConverter<int?,int>()!;

			Assert.AreEqual(-1, c1(null));
			Assert.AreEqual(-2, c2(null));
		}

		[Test]
		public void BaseSchema2()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			Convert<DateTime,string>.Lambda = d => d.ToString(DateTimeFormatInfo.InvariantInfo);

			ms1.SetConverter<DateTime,string>(d => d.ToString("M\\/d\\/yyyy h:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
			ms2.SetConverter<DateTime,string>(d => d.ToString("dd.MM.yyyy HH:mm:ss",  System.Globalization.CultureInfo.InvariantCulture));

			{
				var c0 = Convert<DateTime,string>.Lambda;
				var c1 = ms1.GetConverter<DateTime,string>()!;
				var c2 = ms2.GetConverter<DateTime,string>()!;

				Assert.AreEqual("01/20/2012 16:30:40",  c0(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)));
				Assert.AreEqual("1/20/2012 4:30:40",    c1(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)));
				Assert.AreEqual("20.01.2012 16:30:40",  c2(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)));
			}

			Convert<string,DateTime>.Expression = s => DateTime.Parse(s, DateTimeFormatInfo.InvariantInfo);

			ms1.SetConvertExpression<string,DateTime>(s => DateTime.Parse(s, new CultureInfo("en-US", false).DateTimeFormat));
			ms2.SetConvertExpression<string,DateTime>(s => DateTime.Parse(s, new CultureInfo("ru-RU", false).DateTimeFormat));

			{
				var c0 = Convert<string,DateTime>.Lambda;
				var c1 = ms1.GetConverter<string,DateTime>()!;
				var c2 = ms2.GetConverter<string,DateTime>()!;

				Assert.AreEqual(new DateTime(2012, 1, 20, 16, 30, 40), c0("01/20/2012 16:30:40"));
				Assert.AreEqual(new DateTime(2012, 1, 20, 16, 30, 40), c1("1/20/2012 4:30:40 PM"));
				Assert.AreEqual(new DateTime(2012, 1, 20, 16, 30, 40), c2("20.01.2012 16:30:40"));
			}
		}

		[Test]
		public void CultureInfo()
		{
			var ms = new MappingSchema();

			var ci = (CultureInfo)new CultureInfo("ru-RU", false).Clone();

			ci.DateTimeFormat.FullDateTimePattern = "dd.MM.yyyy HH:mm:ss";
			ci.DateTimeFormat.LongDatePattern = "dd.MM.yyyy";
			ci.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
			ci.DateTimeFormat.LongTimePattern = "HH:mm:ss";
			ci.DateTimeFormat.ShortTimePattern = "HH:mm:ss";

			ms.SetCultureInfo(ci);
			Assert.AreEqual("20.01.2012 16:30:40",                 ms.GetConverter<DateTime,string>()!(new DateTime(2012, 1, 20, 16, 30, 40)));
			Assert.AreEqual(new DateTime(2012, 1, 20, 16, 30, 40), ms.GetConverter<string,DateTime>()!("20.01.2012 16:30:40"));
			Assert.AreEqual("100000,999",                          ms.GetConverter<decimal,string> ()!(100000.999m));
			Assert.AreEqual(100000.999m,                           ms.GetConverter<string,decimal> ()!("100000,999"));
			//Assert.AreEqual(100000.999m,                           ConvertTo<decimal>.From("100000.999")); this will fail if System Locale is ru-RU
			Assert.AreEqual("100000,999",                          ms.GetConverter<double,string>  ()!(100000.999));
			Assert.AreEqual(100000.999,                            ms.GetConverter<string,double>  ()!("100000,999"));
		}

		sealed class AttrTest
		{
			[MapValue(Value = 1)]
			[MapValue(Value = 2, Configuration = "2")]
			[MapValue(Value = 3, Configuration = "3")]
			public int Field1;
		}

		[Test]
		public void AttributeTest1()
		{
			var ms = new MappingSchema("2");

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1),
				a => a.Configuration);

			Assert.That(attrs.Length,   Is.EqualTo(2));
			Assert.That(attrs[0].Value, Is.EqualTo(2));
			Assert.That(attrs[1].Value, Is.EqualTo(1));
		}

		[Test]
		public void AttributeTest2()
		{
			var ms = new MappingSchema("2", new MappingSchema("3"));

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1),
				a => a.Configuration);

			Assert.That(attrs.Length,   Is.EqualTo(3));
			Assert.That(attrs[0].Value, Is.EqualTo(2));
			Assert.That(attrs[1].Value, Is.EqualTo(3));
			Assert.That(attrs[2].Value, Is.EqualTo(1));
		}

		[Test]
		public void AttributeTest3()
		{
			var ms = new MappingSchema("3", new MappingSchema("2"));

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1),
				a => a.Configuration);

			Assert.That(attrs.Length,   Is.EqualTo(3));
			Assert.That(attrs[0].Value, Is.EqualTo(3));
			Assert.That(attrs[1].Value, Is.EqualTo(2));
			Assert.That(attrs[2].Value, Is.EqualTo(1));
		}

		[Test]
		public void AttributeTest4()
		{
			var attrs = MappingSchema.Default.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs.Length, Is.EqualTo(3));
		}

		[Test]
		public void AttributeTest5()
		{
			var attrs = MappingSchema.Default.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1),
				a => a.Configuration);

			Assert.That(attrs.Length,   Is.EqualTo(1));
			Assert.That(attrs[0].Value, Is.EqualTo(1));
		}

		const string Data =
			@"<?xml version='1.0' encoding='utf-8' ?>
			<Types>
				<Type Name='AttrTest'>
					<Member Name='Field1'>
						<MapValueAttribute>
							<Configuration Value='3' />
							<Value Value='30' Type='System.Int32' />
						</MapValueAttribute>
					</Member>
				</Type>
			</Types>";

		[Test]
		public void AttributeTest6()
		{
			var ms = new MappingSchema("2",
				new MappingSchema("3")
				{
					MetadataReader = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data)))
				});

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1),
				a => a.Configuration);

			Assert.That(attrs.Length,   Is.EqualTo(4));
			Assert.That(attrs[0].Value, Is.EqualTo(2));
			Assert.That(attrs[1].Value, Is.EqualTo(30));
			Assert.That(attrs[2].Value, Is.EqualTo(3));
			Assert.That(attrs[3].Value, Is.EqualTo(1));
		}

		[Test]
		public void AttributeTest7()
		{
			var ms = new MappingSchema("2",
				new MappingSchema("3")
				{
					MetadataReader = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data)))
				})
			{
				MetadataReader = MappingSchema.Default.MetadataReader
			};

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1),
				a => a.Configuration);

			Assert.That(attrs.Length,   Is.EqualTo(4));
			Assert.That(attrs[0].Value, Is.EqualTo(2));
			Assert.That(attrs[1].Value, Is.EqualTo(3));
			Assert.That(attrs[2].Value, Is.EqualTo(30));
			Assert.That(attrs[3].Value, Is.EqualTo(1));
		}

		[Test]
		public void AttributeTest8()
		{
			var ms = new MappingSchema("3", new MappingSchema("2"))
			{
				MetadataReader = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data)))
			};

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1),
				a => a.Configuration);

			Assert.That(attrs.Length,   Is.EqualTo(4));
			Assert.That(attrs[0].Value, Is.EqualTo(30));
			Assert.That(attrs[1].Value, Is.EqualTo(3));
			Assert.That(attrs[2].Value, Is.EqualTo(2));
			Assert.That(attrs[3].Value, Is.EqualTo(1));
		}

		enum Enum1
		{
			[MapValue("1", 1), MapValue(2)] Value1,
			[MapValue(1), MapValue("1", 2)] Value2,
		}

		[Test]
		public void ConvertEnum1()
		{
			var conv = MappingSchema.Default.GetConverter<int, Enum1>()!;
			Assert.That(conv(2), Is.EqualTo(Enum1.Value1));
		}

		[Test]
		public void ConvertEnum2()
		{
			var conv = new MappingSchema("1").GetConverter<int,Enum1>()!;
			Assert.That(conv(2), Is.EqualTo(Enum1.Value2));
		}

		[Test]
		public void ConvertNullableEnum()
		{
			var schema = new MappingSchema("2");
			Assert.That(schema.GetDefaultValue(typeof(Enum1?)), Is.Null);
			var mapType = ConvertBuilder.GetDefaultMappingFromEnumType(schema, typeof(Enum1?))!;
			Assert.That(mapType, Is.EqualTo(typeof(int?)));
			var convertedValue = Converter.ChangeType(null, mapType, schema);
			Assert.IsNull(convertedValue);
		}

		public class PkTable
		{
			[PrimaryKey, Identity]
			[DataType(DataType.DateTime)]
			public int Id;
		}

		[Column("ParentId", "Parent.Id")]
		public class FkTable
		{
			[PrimaryKey]
			public int Id;

			public PkTable? Parent;
		}

		[Test]
		public void DoNotUseComplexAttributes()
		{
			var ed = MappingSchema.Default.GetEntityDescriptor(typeof(FkTable));
			var c  = ed.Columns.Single(_ => _.ColumnName == "ParentId");

			Assert.False(c.IsPrimaryKey);
			Assert.False(c.IsIdentity);
			Assert.AreEqual(DataType.DateTime, c.DataType);
		}

		[Repeat(100)]
		[Test]
		public void TestIssue3312()
		{
			var ms = new MappingSchema();

			var tasks = new Task[10];

			for (var i = 0; i < tasks.Length; i++)
				tasks[i] = Task.Run(() => ms.GetConvertExpression(typeof(string), typeof(int)));

			Task.WaitAll(tasks);
		}
	}
}
