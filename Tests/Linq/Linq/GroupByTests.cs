﻿using System;
using System.Linq;

using LinqToDB;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Linq;
	using LinqToDB.Mapping;
	using Model;

	[TestFixture]
	public class GroupByTests : TestBase
	{
		[Test]
		public void Simple1([DataSources] string context)
		{
			using (new PreloadGroups(true))
			using (new GuardGrouping(false))
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var q =
					from ch in db.Child
					group ch by ch.ParentID;

				var list = q.ToList().Where(n => n.Key < 6).OrderBy(n => n.Key).ToList();

				Assert.AreEqual(4, list.Count);

				for (var i = 0; i < list.Count; i++)
				{
					var values = list[i].OrderBy(c => c.ChildID).ToList();

					Assert.AreEqual(i + 1, list[i].Key);
					Assert.AreEqual(i + 1, values.Count);

					for (var j = 0; j < values.Count; j++)
						Assert.AreEqual((i + 1) * 10 + j + 1, values[j].ChildID);
				}
			}
		}

		[Test]
		public void Simple2([DataSources] string context)
		{
			using (new PreloadGroups(false))
			using (new GuardGrouping(false))
			using (var db = GetDataContext(context))
			{
				var q =
					from ch in db.GrandChild
					group ch by new { ch.ParentID, ch.ChildID };

				var list = q.ToList();

				Assert.AreEqual   (8, list.Count);
				Assert.AreNotEqual(0, list.OrderBy(c => c.Key.ParentID).First().ToList().Count);
			}
		}

		[Test]
		public void Simple3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from ch in db.Child
					group ch by ch.ParentID into g
					select g.Key;

				var list = q.ToList().Where(n => n < 6).OrderBy(n => n).ToList();

				Assert.AreEqual(4, list.Count);
				for (var i = 0; i < list.Count; i++) Assert.AreEqual(i + 1, list[i]);
			}
		}

		[Test]
		public void Simple4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from ch in db.Child
					group ch by ch.ParentID into g
					orderby g.Key
					select g.Key;

				var list = q.ToList().Where(n => n < 6).ToList();

				Assert.AreEqual(4, list.Count);
				for (var i = 0; i < list.Count; i++) Assert.AreEqual(i + 1, list[i]);
			}
		}

		[Test]
		public void Simple5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild
					group ch by new { ch.ParentID, ch.ChildID } into g
					group g  by new { g.Key.ParentID }          into g
					select g.Key
					,
					from ch in db.GrandChild
					group ch by new { ch.ParentID, ch.ChildID } into g
					group g  by new { g.Key.ParentID }          into g
					select g.Key);
		}

		[Test]
		public void Simple6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q    = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID);
				var list = q.ToList();

				Assert.AreNotEqual(0, list[0].Count());
				Assert.AreEqual   (8, list.Count);
			}
		}

		[Test]
		public void Simple7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GrandChild
					.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID)
					.Select (gr => new { gr.Key.ParentID, gr.Key.ChildID });

				var list = q.ToList();
				Assert.AreEqual(8, list.Count);
			}
		}

		[Test]
		public void Simple8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, (g,ch) => g.ChildID);

				var list = q.ToList();
				Assert.AreEqual(8, list.Count);
			}
		}

		[Test]
		public void Simple9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q    = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID,  (g,ch) => g.ChildID);
				var list = q.ToList();

				Assert.AreEqual(8, list.Count);
			}
		}

		[Test]
		public void Simple10([DataSources] string context)
		{
			using (new GuardGrouping(false))
			using (var db = GetDataContext(context))
			{
				var expected = (from ch in    Child group ch by ch.ParentID into g select g).ToList().OrderBy(p => p.Key).ToList();
				var result   = (from ch in db.Child group ch by ch.ParentID into g select g).ToList().OrderBy(p => p.Key).ToList();

				AreEqual(expected[0], result[0]);
				AreEqual(expected.Select(p => p.Key), result.Select(p => p.Key));
				AreEqual(expected[0].ToList(), result[0].ToList());
			}
		}

		[Test]
		public void Simple11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 = GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID);

				var q2 = db.GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID);

				var list1 = q1.AsEnumerable().OrderBy(_ => _.Key.ChildID).ToList();
				var list2 = q2.AsEnumerable().OrderBy(_ => _.Key.ChildID).ToList();

				Assert.AreEqual(list1.Count,       list2.Count);
				Assert.AreEqual(list1[0].ToList(), list2[0].ToList());
			}
		}

		[Test]
		public void Simple12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, (g,ch) => g.ChildID);

				var list = q.ToList();
				Assert.AreEqual(8, list.Count);
			}
		}

		[Test]
		public void Simple13([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID, (g,ch) => g.ChildID);

				var list = q.ToList();
				Assert.AreEqual(8, list.Count);
			}
		}

		//[Test]
		public void Simple14([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					select
						from c in p.Children
						group c by c.ParentID into g
						select g.Key,
					from p in db.Parent
					select
						from c in p.Children
						group c by c.ParentID into g
						select g.Key);
		}

		[Test]
		public void MemberInit1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by new Child { ParentID = ch.ParentID } into g
					select g.Key
					,
					from ch in db.Child
					group ch by new Child { ParentID = ch.ParentID } into g
					select g.Key);
		}

		sealed class GroupByInfo
		{
			public GroupByInfo? Prev;
			public object?      Field;

			public override bool Equals(object? obj)
			{
				return Equals(obj as GroupByInfo);
			}

			public bool Equals(GroupByInfo? other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return Equals(other.Prev, Prev) && Equals(other.Field, Field);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((Prev != null ? Prev.GetHashCode() : 0) * 397) ^ (Field != null ? Field.GetHashCode() : 0);
				}
			}
		}

		[Test]
		public void MemberInit2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by new GroupByInfo { Prev = new GroupByInfo { Field = ch.ParentID }, Field = ch.ChildID } into g
					select g.Key
					,
					from ch in db.Child
					group ch by new GroupByInfo { Prev = new GroupByInfo { Field = ch.ParentID }, Field = ch.ChildID } into g
					select g.Key);
		}

		[Test]
		public void MemberInit3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by new { Prev = new { Field = ch.ParentID }, Field = ch.ChildID } into g
					select g.Key
					,
					from ch in db.Child
					group ch by new { Prev = new { Field = ch.ParentID }, Field = ch.ChildID } into g
					select g.Key);
		}

		[Test]
		public void SubQuery1([DataSources] string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in
						from ch in Child select ch.ParentID + 1
					where ch + 1 > n
					group ch by ch into g
					select g.Key
					,
					from ch in
						from ch in db.Child select ch.ParentID + 1
					where ch > n
					group ch by ch into g
					select g.Key);
		}

		[Test]
		public void SubQuery2([DataSources] string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child select new { ParentID = ch.ParentID + 1 } into ch
					where ch.ParentID > n
					group ch by ch into g
					select g.Key
					,
					from ch in db.Child select new { ParentID = ch.ParentID + 1 } into ch
					where ch.ParentID > n
					group ch by ch into g
					select g.Key);
		}

		[Test]
		public void SubQuery3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in
						from ch in Child
						select new { ch, n = ch.ChildID + 1 }
					group ch by ch.n into g
					select new
					{
						g.Key,
						Sum = g.Sum(_ => _.ch.ParentID)
					}
					,
					from ch in
						from ch in db.Child
						select new { ch, n = ch.ChildID + 1 }
					group ch by ch.n into g
					select new
					{
						g.Key,
						Sum = g.Sum(_ => _.ch.ParentID)
					});
		}

		[Test]
		public void SubQuery31([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in
						from ch in Child
						select new { ch, n = ch.ChildID + 1 }
					group ch.ch by ch.n into g
					select new
					{
						g.Key,
						Sum = g.Sum(_ => _.ParentID)
					}
					,
					from ch in
						from ch in db.Child
						select new { ch, n = ch.ChildID + 1 }
					group ch.ch by ch.n into g
					select new
					{
						g.Key,
						Sum = g.Sum(_ => _.ParentID)
					});
		}

		[Test]
		public void SubQuery32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in
						from ch in Child
						select new { ch, n = ch.ChildID + 1 }
					group ch.ch.ParentID by ch.n into g
					select new
					{
						g.Key,
						Sum = g.Sum(_ => _)
					}
					,
					from ch in
						from ch in db.Child
						select new { ch, n = ch.ChildID + 1 }
					group ch.ch.ParentID by ch.n into g
					select new
					{
						g.Key,
						Sum = g.Sum(_ => _)
					});
		}

		[Test]
		public void SubQuery4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by new { n = ch.ChildID + 1 } into g
					select new
					{
						g.Key,
						Sum = g.Sum(_ => _.ParentID)
					}
					,
					from ch in db.Child
					group ch by new { n = ch.ChildID + 1 } into g
					select new
					{
						g.Key,
						Sum = g.Sum(_ => _.ParentID)
					});
		}

		[Test]
		public void SubQuery5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					join p in Parent on ch.ParentID equals p.ParentID into pg
					from p in pg.DefaultIfEmpty()
					group ch by ch.ChildID into g
					select g.Sum(_ => _.ParentID)
					,
					from ch in db.Child
					join p in db.Parent on ch.ParentID equals p.ParentID into pg
					from p in pg.DefaultIfEmpty()
					group ch by ch.ChildID into g
					select g.Sum(_ => _.ParentID));
		}

		[Test]
		public void SubQuery6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child select new { ParentID = ch.ParentID + 1 } into ch
					group ch.ParentID by ch into g
					select g.Key
					,
					from ch in db.Child select new { ParentID = ch.ParentID + 1 } into ch
					group ch.ParentID by ch into g
					select g.Key);
		}

		[Test]
		public void SubQuery7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join c in
						from c in Child
						where c.ParentID == 1
						select c
					on p.ParentID equals c.ParentID into g
					from c in g.DefaultIfEmpty()
					group p by c == null ? 0 : c.ChildID into gg
					select new { gg.Key }
					,
					from p in db.Parent
					join c in
						from c in db.Child
						where c.ParentID == 1
						select c
					on p.ParentID equals c.ParentID into g
					from c in g.DefaultIfEmpty()
					group p by c.ChildID into gg
					select new { gg.Key });
		}

		[Test]
		public void Calculated1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var expected =
					(
						from ch in Child
						group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
						into g select g
					).ToList().OrderBy(p => p.Key).ToList();

				var result =
					(
						from ch in db.Child
						group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
						into g select g
					).ToList().OrderBy(p => p.Key).ToList();

				AreEqual(expected[0], result[0]);
				AreEqual(expected.Select(p => p.Key), result.Select(p => p.Key));
			}
		}

		[Test]
		public void Calculated2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in
						from ch in
							from ch in Child
							group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
							into g select g
						select ch.Key + "2"
					where p == "22"
					select p
					,
					from p in
						from ch in
							from ch in db.Child
							group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
							into g select g
						select ch.Key + "2"
					where p == "22"
					select p);
		}

		[Test]
		public void GroupBy1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.GroupBy(ch => ch.ParentID).GroupBy(ch => ch).GroupBy(ch => ch).Select(p => p.Key.Key.Key),
					db.Child.GroupBy(ch => ch.ParentID).GroupBy(ch => ch).GroupBy(ch => ch).Select(p => p.Key.Key.Key));
		}

		[Test]
		public void GroupBy2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join c in Child on p.ParentID equals c.ParentID
					group p by new
					{
						ID = p.Value1 ?? c.ChildID
					} into gr
					select new
					{
						gr.Key.ID,
						ID1 = gr.Key.ID + 1,
					}
					,
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group p by new
					{
						ID = p.Value1 ?? c.ChildID
					} into gr
					select new
					{
						gr.Key.ID,
						ID1 = gr.Key.ID + 1,
					});
		}

		[Test]
		public void GroupBy3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join c in Child on p.ParentID equals c.ParentID
					group p by p.Value1 ?? c.ChildID into gr
					select new
					{
						gr.Key
					}
					,
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					group p by p.Value1 ?? c.ChildID into gr
					select new
					{
						gr.Key
					});
		}

		[Test]
		public void Sum1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					select g.Sum(p => p.ChildID)
					,
					from ch in db.Child
					group ch by ch.ParentID into g
					select g.Sum(p => p.ChildID));
		}

		[Test]
		public void Sum2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					select new { Sum = g.Sum(p => p.ChildID) }
					,
					from ch in db.Child
					group ch by ch.ParentID into g
					select new { Sum = g.Sum(p => p.ChildID) });
		}

		[Test]
		public void Sum3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from ch in Child
					group ch by ch.Parent
					into g
					select g.Key.Children.Sum(p => p.ChildID),
					from ch in db.Child
					group ch by ch.Parent
					into g
					select g.Key.Children.Sum(p => p.ChildID));
			}
		}

		[Test]
		public void SumSubQuery1([DataSources] string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in
						from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
					where ch.ParentID + 1 > n group ch by ch into g
					select g.Sum(p => p.ParentID - 3)
					,
					from ch in
						from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
					where ch.ParentID + 1 > n group ch by ch into g
					select g.Sum(p => p.ParentID - 3));
		}

		[Test]
		public void GroupByMax([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child group ch.ParentID by ch.ChildID into g select new { Max = g.Max() },
					from ch in db.Child group ch.ParentID by ch.ChildID into g select new { Max = g.Max() });
		}

		[Test]
		public void Aggregates1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from  ch in Child
					group ch by ch.ParentID into g
					select new
					{
						Cnt = g.Count(),
						Sum = g.Sum(c => c.ChildID),
						Min = g.Min(c => c.ChildID),
						Max = g.Max(c => c.ChildID),
						Avg = (int)g.Average(c => c.ChildID),
					},
					from  ch in db.Child
					group ch by ch.ParentID into g
					select new
					{
						Cnt = g.Count(),
						Sum = g.Sum(c => c.ChildID),
						Min = g.Min(c => c.ChildID),
						Max = g.Max(c => c.ChildID),
						Avg = (int)g.Average(c => c.ChildID),
					});
		}

		[Test]
		public void Aggregates2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from  ch in Child
					group ch by ch.ParentID into g
					select new
					{
						Sum = g.Select(c => c.ChildID).Sum(),
						Min = g.Select(c => c.ChildID).Min(),
						Max = g.Select(c => c.ChildID).Max(),
						Avg = (int)g.Select(c => c.ChildID).Average(),
						Cnt = g.Count()
					},
					from  ch in db.Child
					group ch by ch.ParentID into g
					select new
					{
						Sum = g.Select(c => c.ChildID).Sum(),
						Min = g.Select(c => c.ChildID).Min(),
						Max = g.Select(c => c.ChildID).Max(),
						Avg = (int)g.Select(c => c.ChildID).Average(),
						Cnt = g.Count()
					});
		}

		[Test]
		public void Aggregates3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from  ch in Child
					where ch.ChildID > 30
					group ch by ch.ParentID into g
					select new
					{
						Sum =      g.Select(c => c.ChildID).Where(_ => _ > 30).Sum(),
						Min =      g.Select(c => c.ChildID).Where(_ => _ > 30).Min(),
						Max =      g.Select(c => c.ChildID).Where(_ => _ > 30).Max(),
						Avg = (int)g.Select(c => c.ChildID).Where(_ => _ > 30).Average(),
					},
					from  ch in db.Child
					where ch.ChildID > 30
					group ch by ch.ParentID into g
					select new
					{
						Sum =      g.Select(c => c.ChildID).Where(_ => _ > 30).Sum(),
						Min =      g.Select(c => c.ChildID).Where(_ => _ > 30).Min(),
						Max =      g.Select(c => c.ChildID).Where(_ => _ > 30).Max(),
						Avg = (int)g.Select(c => c.ChildID).Where(_ => _ > 30).Average(),
					});
		}

		[Test]
		public void Aggregates4([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from  ch in Child
					group ch by ch.ParentID into g
					select new
					{
						Count = g.Count(_ => _.ChildID > 30),
						Sum   = g.Where(_ => _.ChildID > 30).Sum(c => c.ChildID),
					},
					from  ch in db.Child
					group ch by ch.ParentID into g
					select new
					{
						Count = g.Count(_ => _.ChildID > 30),
						Sum   = g.Where(_ => _.ChildID > 30).Sum(c => c.ChildID),
					});
		}

		[Test]
		public void Aggregates5([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					select new
					{
						Count1 = g.Count(c => c.ChildID > 30),
						Count2 = g.Select(c => c.ChildID).Where(_ => _ > 30).Count(),
						Count3 = g.Count()
					},
					from ch in db.Child
					group ch by ch.ParentID into g
					select new
					{
						Count1 = g.Count(c => c.ChildID > 30),
						Count2 = g.Select(c => c.ChildID).Where(_ => _ > 30).Count(),
						Count3 = g.Count()
					});
		}


		[Test]
		public void SelectMax([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					select g.Max(c => c.ChildID)
					,
					from ch in db.Child
					group ch by ch.ParentID into g
					select g.Max(c => c.ChildID));
		}

		[Test]
		public void JoinMax([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
						join max in
							from ch1 in Child
							group ch1 by ch1.ParentID into g
							select g.Max(c => c.ChildID)
						on ch.ChildID equals max
					select ch
					,
					from ch in db.Child
						join max in
							from ch1 in db.Child
							group ch1 by ch1.ParentID into g
							select g.Max(c => c.ChildID)
						on ch.ChildID equals max
					select ch);
		}

		[Test]
		public void Min1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Min(c => c.ChildID),
					db.Child.Min(c => c.ChildID));
		}

		[Test]
		public void Min2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Select(c => c.ChildID).Min(),
					db.Child.Select(c => c.ChildID).Min());
		}

		[Test]
		public void Max1([DataSources] string context)
		{
			var expected = Child.Max(c => c.ChildID);
			Assert.AreNotEqual(0, expected);

			using (var db = GetDataContext(context))
				Assert.AreEqual(expected, db.Child.Max(c => c.ChildID));
		}

		[Test]
		public void Max11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Max(c => c.ChildID > 20),
					db.Child.Max(c => c.ChildID > 20));
		}

		[Test]
		public void Max12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Max(c => (bool?)(c.ChildID > 20)),
					db.Child.Max(c => (bool?)(c.ChildID > 20)));
		}

		[Test]
		public void Max2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent
						join c in Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

				var result =
					from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

				Assert.AreEqual(expected.Max(p => p.ParentID), result.Max(p => p.ParentID));
			}
		}

		[Test]
		public void Max3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Select(c => c.ChildID).Max(),
					db.Child.Select(c => c.ChildID).Max());
		}

		[Test]
		public void Max4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					from t1 in Types
					join t2 in
						from sub in Types
						where
							sub.ID == 1 &&
							sub.DateTimeValue <= TestData.Date
						group sub by new
						{
							sub.ID
						} into g
						select new
						{
							g.Key.ID,
							DateTimeValue = g.Max( p => p.DateTimeValue )
						}
					on new { t1.ID, t1.DateTimeValue } equals new { t2.ID, t2.DateTimeValue }
					select t1.MoneyValue,
					from t1 in db.Types
					join t2 in
						from sub in db.Types
						where
							sub.ID == 1 &&
							sub.DateTimeValue <= TestData.Date
						group sub by new
						{
							sub.ID
						} into g
						select new
						{
							g.Key.ID,
							DateTimeValue = g.Max( p => p.DateTimeValue )
						}
					on new { t1.ID, t1.DateTimeValue } equals new { t2.ID, t2.DateTimeValue }
					select t1.MoneyValue
					);
		}

		[Test]
		public void Average1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(int)db.Child.Average(c => c.ChildID),
					(int)   Child.Average(c => c.ChildID));
		}

		[Test]
		public void Average2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(int)   Child.Select(c => c.ChildID).Average(),
					(int)db.Child.Select(c => c.ChildID).Average());
		}

		[Test]
		public void GroupByAssociation1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where g.Count() > 2
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where g.Count() > 2
					select g.Key.Value1);
		}

		[Test]
		public void GroupByAssociation101([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where g.Max(_ => _.ParentID) > 2
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where g.Max(_ => _.ParentID) > 2
					select g.Key.Value1);
		}

		[Test]
		public void GroupByAssociation102([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where g.Count(_ => _.ChildID >= 20) > 2
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where g.Count(_ => _.ChildID >= 20) > 2
					select g.Key.Value1);
		}

		[Test]
		public void GroupByAssociation1022([DataSources(
			ProviderName.SqlCe, TestProvName.AllClickHouse, TestProvName.AllAccess /* Can be fixed*/)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where g.Count(_ => _.ChildID >= 20) > 2 && g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where g.Count(_ => _.ChildID >= 20) > 2 && g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0
					select g.Key.Value1);
		}

		[Test]
		public void GroupByAssociation1023([DataSources(
			ProviderName.SqlCe, TestProvName.AllClickHouse, TestProvName.AllAccess /* Can be fixed.*/)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where
						g.Count(_ => _.ChildID >= 20) > 2 &&
						g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where
						g.Count(_ => _.ChildID >= 20) > 2 &&
						g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0
					select g.Key.Value1);
		}

		[Test]
		public void GroupByAssociation1024([DataSources(
			ProviderName.SqlCe, TestProvName.AllClickHouse, TestProvName.AllAccess) /* Can be fixed. */]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where
						g.Count(_ => _.ChildID >= 20) > 2 &&
						g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 18).Max(p => p.ParentID) > 0
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where
						g.Count(_ => _.ChildID >= 20) > 2 &&
						g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 18).Max(p => p.ParentID) > 0
					select g.Key.Value1);
		}

		[Test]
		public void GroupByAssociation2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where g.Count() > 2 && g.Key.ParentID != 1
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where g.Count() > 2 && g.Key.ParentID != 1
					select g.Key.Value1);
		}

		[Test]
		public void GroupByAssociation3([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var result =
					from p in db.Product
					group p by p.Category into g
					where g.Count() == 12
					select g.Key.CategoryName;

				var list = result.ToList();
				Assert.AreEqual(3, list.Count);
			}
		}

		[Test]
		public void GroupByAssociation4([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var result =
					from p in db.Product
					group p by p.Category into g
					where g.Count() == 12
					select g.Key.CategoryID;

				var list = result.ToList();
				Assert.AreEqual(3, list.Count);
			}
		}

		[Test]
		public void GroupByAggregate1([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
					select g.Key
					,
					from p in db.Parent
					group p by  p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
					select g.Key);
		}

		[Test]
		public void GroupByAggregate11([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.Children.Count > 0
					group p by p.Children.Average(c => c.ParentID) > 3 into g
					select g.Key
					,
					from p in db.Parent
					where p.Children.Count > 0
					group p by p.Children.Average(c => c.ParentID) > 3 into g
					select g.Key);
		}

		[Test]
		public void GroupByAggregate12([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
					select g.Key
					,
					from p in db.Parent
					group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
					select g.Key);
		}

		[Test]
		public void GroupByAggregate2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					(
						from c in dd.Customer
						group c by c.Orders.Count > 0 && c.Orders.Average(o => o.Freight) >= 80
					).ToList().Select(k => k.Key),
					(
						from c in db.Customer
						group c by c.Orders.Average(o => o.Freight) >= 80
					).ToList().Select(k => k.Key));
			}
		}

		[Test]
		public void GroupByAggregate3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3
					).ToList().First(g => !g.Key)
					,
					(
						from p in db.Parent
						group p by p.Children.Average(c => c.ParentID) > 3
					).ToList().First(g => !g.Key));
		}

		[Test]
		public void ByJoin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c1 in Child
					join c2 in Child on c1.ChildID equals c2.ChildID + 1
					group c2 by c1.ParentID into g
					select g.Sum(_ => _.ChildID)
					,
					from c1 in db.Child
					join c2 in db.Child on c1.ChildID equals c2.ChildID + 1
					group c2 by c1.ParentID into g
					select g.Sum(_ => _.ChildID));
		}

		[Test]
		public void SelectMany([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.GroupBy(ch => ch.ParentID).SelectMany(g => g),
					db.Child.GroupBy(ch => ch.ParentID).SelectMany(g => g));
		}

		[Test]
		public void Scalar1([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Max()),
					(from ch in db.Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Max()));
		}

		[Test]
		public void Scalar101([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					select ch.ChildID into id
					group id by id into g
					select g.Max()
					,
					from ch in db.Child
					select ch.ChildID into id
					group id by id into g
					select g.Max());
		}

		[Test]
		public void Scalar2([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in Child
					 group ch by ch.ParentID into g
					 select new
						 {
							 Max1 = g.Select(ch => ch.ChildID              ).Max(),
							 Max2 = g.Select(ch => ch.ChildID + ch.ParentID).Max()
						 }),
					(from ch in db.Child
					 group ch by ch.ParentID into g
					 select new
						 {
							 Max1 = g.Select(ch => ch.ChildID              ).Max(),
							 Max2 = g.Select(ch => ch.ChildID + ch.ParentID).Max()
						 }));
		}

		[Test]
		public void Scalar3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Where(id => id > 0).Max()),
					(from ch in db.Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Where(id => id > 0).Max()));
		}

		[Test]
		public void Scalar4([DataSources(ProviderName.SqlCe, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
					select g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min()
					,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
					select g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min());
		}

		[Test]
		public void Scalar41([DataSources(ProviderName.SqlCe, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					select new { g } into g
					where g.g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
					select g.g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min()
					,
					from ch in db.Child
					group ch by ch.ParentID into g
					select new { g } into g
					where g.g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
					select g.g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min());
		}

		[Test]
		public void Scalar5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					select ch.ParentID into id
					group id by id into g
					select g.Max()
					,
					from ch in db.Child
					select ch.ParentID into id
					group id by id into g
					select g.Max());
		}

		//[Test]
		public void Scalar51([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					select g.Max()
					,
					from ch in db.Child
					group ch by ch.ParentID into g
					select g.Max());
		}

		[Test]
		public void Scalar6([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in Child
					 where ch.ParentID < 3
					 group ch by ch.ParentID into g
					 select g.Where(ch => ch.ParentID < 3).Max(ch => ch.ChildID))
					 ,
					(from ch in db.Child
					 where ch.ParentID < 3
					 group ch by ch.ParentID into g
					 select g.Where(ch => ch.ParentID < 3).Max(ch => ch.ChildID)));
		}

		[Test]
		public void Scalar7([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in Child
					 group ch by ch.ParentID into g
					 select new { max = g.Select(ch => ch.ChildID).Max()}).Select(id => id.max)
					 ,
					(from ch in db.Child
					 group ch by ch.ParentID into g
					 select new { max = g.Select(ch => ch.ChildID).Max()}).Select(id => id.max));
		}

		[Test]
		public void Scalar8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in Child
					group ch by ch.ParentID into g
					select new { max = g.Max(ch => ch.ChildID)}).Select(id => id.max)
					,
					(from ch in db.Child
					group ch by ch.ParentID into g
					select new { max = g.Max(ch => ch.ChildID)}).Select(id => id.max));
		}

		[Test]
		public void Scalar9([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count()),
					(from ch in db.Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count()));
		}

		[Test]
		public void Scalar10([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count(id => id >= 20))
					 ,
					(from ch in db.Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count(id => id >= 20)));
		}

		[Test]
		public void GroupByExtraFieldBugTest([IncludeDataSources(TestProvName.AllMySql, TestProvName.AllClickHouse)] string context)
		{
			// https://github.com/igor-tkachev/LinqToDB/issues/42
			// extra field is generated in the GROUP BY clause, for example:
			// GROUP BY p.LastName, p.LastName <--- the second one is redundant

			using (var db = GetDataConnection(context))
			{
				var q =
					from d in db.Doctor
					join p in db.Person on d.PersonID equals p.ID
					group d by p.LastName into g
					select g.Key;

				var _ = q.ToList();

				const string fieldName = "LastName";

				var lastQuery  = db.LastQuery!;
				var groupByPos = lastQuery.IndexOf("GROUP BY");
				var fieldPos   = lastQuery.IndexOf(fieldName, groupByPos);

				// check that our field does not present in the GROUP BY clause second time.
				//
				Assert.AreEqual(-1, lastQuery.IndexOf(fieldName, fieldPos + 1));
			}
		}

		[Test]
		public void DoubleGroupBy1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in
						from p in Parent
						where p.Value1 != null
						group p by p.ParentID into g
						select new
						{
							ID  = g.Key,
							Max = g.Max(t => t.Value1)
						}
					group t by t.ID into g
					select new
					{
						g.Key,
						Sum = g.Sum(t => t.Max)
					},
					from t in
						from p in db.Parent
						where p.Value1 != null
						group p by p.ParentID into g
						select new
						{
							ID  = g.Key,
							Max = g.Max(t => t.Value1)
						}
					group t by t.ID into g
					select new
					{
						g.Key,
						Sum = g.Sum(t => t.Max)
					});
		}

		[Test]
		public void DoubleGroupBy2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.Value1 != null
					group p by p.ParentID into g
					select new
					{
						ID  = g.Key,
						Max = g.Max(t => t.Value1)
					} into t
					group t by t.ID into g
					select new
					{
						g.Key,
						Sum = g.Sum(t => t.Max)
					},
					from p in db.Parent
					where p.Value1 != null
					group p by p.ParentID into g
					select new
					{
						ID  = g.Key,
						Max = g.Max(t => t.Value1)
					} into t
					group t by t.ID into g
					select new
					{
						g.Key,
						Sum = g.Sum(t => t.Max)
					});
		}

		[Test]
		public void InnerQuery([DataSources(ProviderName.SqlCe, TestProvName.AllSapHana, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Doctor.GroupBy(s => s.PersonID).Select(s => s.Select(d => d.Taxonomy).First()),
					db.Doctor.GroupBy(s => s.PersonID).Select(s => s.Select(d => d.Taxonomy).First()));
			}
		}

		[Test]
		public void CalcMember([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from parent in Parent
					from child  in Person
					where child.ID == parent.ParentID
					let data = new
					{
						parent.Value1,
						Value = child.FirstName == "John" ? child.FirstName : "a"
					}
					group data by data.Value into groupedData
					select new
					{
						groupedData.Key,
						Count = groupedData.Count()
					},
					from parent in db.Parent
					from child  in db.Person
					where child.ID == parent.ParentID
					let data = new
					{
						parent.Value1,
						Value = child.FirstName == "John" ? child.FirstName : "a"
					}
					group data by data.Value into groupedData
					select new
					{
						groupedData.Key,
						Count = groupedData.Count()
					});
			}
		}

		[Test]
		public void GroupByNone([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from parent in Parent
					group parent by Sql.GroupBy.None into gr
					select new
					{
						Min = gr.Min(p => p.ParentID),
						Max = gr.Max(p => p.ParentID),
					},
					from parent in db.Parent
					group parent by Sql.GroupBy.None into gr
					select new
					{
						Min = gr.Min(p => p.ParentID),
						Max = gr.Max(p => p.ParentID),
					});
			}
		}

		[Test]
		public void GroupByExpression([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var defValue = 10;

				AreEqual(
					from parent in Parent
					group parent by Sql.GroupBy.None into gr
					select new
					{
						Min = Sql.AsSql(gr.Min(p => (int?)p.ParentID) ?? defValue),
					},
					from parent in db.Parent
					group parent by Sql.GroupBy.None into gr
					select new
					{
						Min = Sql.AsSql(gr.Min(p => (int?)p.ParentID) ?? defValue),
					});
			}
		}

		[Test]
		public void GroupByDate1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types
					group t by new { t.DateTimeValue.Month, t.DateTimeValue.Year } into grp
					select new
					{
						Total = grp.Sum(_ => _.MoneyValue),
						year  = grp.Key.Year,
						month = grp.Key.Month
					},
					from t in db.Types
					group t by new { t.DateTimeValue.Month, t.DateTimeValue.Year } into grp
					select new
					{
						Total = grp.Sum(_ => _.MoneyValue),
						year  = grp.Key.Year,
						month = grp.Key.Month
					});
			}
		}

		[Test]
		public void GroupByDate2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types2
					group t by new { t.DateTimeValue!.Value.Month, t.DateTimeValue.Value.Year } into grp
					select new
					{
						Total = grp.Sum(_ => _.MoneyValue),
						year  = grp.Key.Year,
						month = grp.Key.Month
					},
					from t in db.Types2
					group t by new { t.DateTimeValue!.Value.Month, t.DateTimeValue.Value.Year } into grp
					select new
					{
						Total = grp.Sum(_ => _.MoneyValue),
						year  = grp.Key.Year,
						month = grp.Key.Month
					});
			}
		}

		[Test]
		public void GroupByDate3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types2
					group t by new { Date = Sql.MakeDateTime(t.DateTimeValue!.Value.Year, t.DateTimeValue.Value.Month, 1) }   into grp
					select new
					{
						Total = grp.Sum(_ => _.MoneyValue),
						year  = grp.Key.Date!.Value.Year,
						month = grp.Key.Date.Value.Month
					},
					from t in db.Types2
					group t by new { Date = Sql.MakeDateTime(t.DateTimeValue!.Value.Year, t.DateTimeValue.Value.Month, 1) } into grp
					select new
					{
						Total = grp.Sum(_ => _.MoneyValue),
						year  = grp.Key.Date!.Value.Year,
						month = grp.Key.Date.Value.Month
					});
			}
		}

		[Test]
		public void GroupByCount([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(
					(from t in    Child group t by t.ParentID into gr select new { gr.Key, List = gr.ToList() }).Count(),
					(from t in db.Child group t by t.ParentID into gr select new { gr.Key, List = gr.ToList() }).Count());
			}
		}

		[Test]
		public void AggregateAssociation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Child
					group t by t.ParentID into grp
					select new
					{
						Value = grp.Sum(c => c.Parent!.Value1 ?? 0)
					},
					from t in db.Child
					group t by t.ParentID into grp
					select new
					{
						Value = grp.Sum(c => c.Parent!.Value1 ?? 0)
					});
			}
		}

		[Test]
		public void FirstGroupBy([DataSources] string context)
		{
			using (new GuardGrouping(false))
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(
					(from t in    Child group t by t.ParentID into gr select gr.OrderByDescending(g => g.ChildID).First()).AsEnumerable().OrderBy(t => t.ChildID),
					(from t in db.Child group t by t.ParentID into gr select gr.OrderByDescending(g => g.ChildID).First()).AsEnumerable().OrderBy(t => t.ChildID));
			}
		}

		public class ChildEntity
		{
			public int ParentID;
			public int ChildID;
			public int RandValue;
		}

		[Test]
		public void GroupByCustomEntity1([DataSources] string context)
		{
			// I definitly selected it by random
			var rand = 3;
			//var rand = new Random().Next(5);
			//var rand = new Random();

			using (var db = GetDataContext(context))
			{
				AreEqual(
					from e in
						from c in Child
						select new ChildEntity
						{
							RandValue = rand//.Next(5)
							,
							ParentID  = c.ParentID,
						}
					group e by new { e.ParentID, e.RandValue } into g
					select new
					{
						Count = g.Count()
					},
					from e in
						from c in db.Child
						select new ChildEntity
						{
							RandValue = rand,
							ParentID  = c.ParentID,
						}
					group e by new { e.ParentID, e.RandValue } into g
					select new
					{
						Count = g.Count()
					});
			}
		}

		static int GetID(int id)
		{
			return id;
		}

		[Test]
		public void GroupByCustomEntity2([DataSources(TestProvName.AllSybase)] string context)
		{
			// pure random
			var rand = 3;

			using (var db = GetDataContext(context))
			{
				AreEqual(
					from e in
						from c in Child
						select new ChildEntity
						{
							RandValue = GetID(rand),
							ParentID  = c.ParentID,
						}
					group e by new { e.ParentID, e.RandValue } into g
					select new
					{
						Count = g.Count()
					},
					from e in
						from c in db.Child
						select new ChildEntity
						{
							RandValue = GetID(rand),
							ParentID  = c.ParentID,
						}
					group e by new { e.ParentID, e.RandValue } into g
					select new
					{
						Count = g.Count()
					});
			}
		}

		[Test]
		public void JoinGroupBy1([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from c in Child
					from g in c.GrandChildren
					group c by g.ParentID into gc
					select gc.Key
					,
					from c in db.Child
					from g in c.GrandChildren
					group c by g.ParentID into gc
					select gc.Key
				);
			}
		}

		[Test]
		public void JoinGroupBy2([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from c in Child
					from g in c.Parent!.Children
					group g by g.ParentID into gc
					select gc.Key
					,
					from c in db.Child
					from g in c.Parent!.Children
					group g by g.ParentID into gc
					select gc.Key
				);
			}
		}

		[Test]
		public void OrderByGroupBy([DataSources()] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = from c in db.Child
							 orderby c.ChildID, c.ParentID
							 select c;

				var query2 = from c1 in query1
							 group c1 by c1.ParentID
					into c2
							 select c2.Key;

				Assert.DoesNotThrow(() => query2.ToArray());

				var orderItems = query2.GetSelectQuery().OrderBy.Items;

				Assert.That(orderItems.Count, Is.EqualTo(1));
				Assert.That(QueryHelper.GetUnderlyingField(orderItems[0].Expression)!.Name, Is.EqualTo("ParentID"));
			}
		}

		void CheckGuardedQuery<TKey, TEntity>(IQueryable<IGrouping<TKey, TEntity>> grouping)
			where TKey: notnull
		{
			Assert.Throws<LinqToDBException>(() =>
			{
				grouping.ToDictionary(_ => _.Key, _ => _.ToList());
			});

			Assert.DoesNotThrow(() =>
			{
				grouping.DisableGuard().ToDictionary(_ => _.Key, _ => _.ToList());
			});
		}

		[Test]
		public void GroupByGuard([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using(new GuardGrouping(true))
			using (var db = GetDataContext(context))
			{
				// group on client
				var dictionary1 = db.Person
					.AsEnumerable()
					.GroupBy(_ => _.Gender)
					.ToDictionary(_ => _.Key, _ => _.ToList());

				var dictionary2 = Person
					.AsEnumerable()
					.GroupBy(_ => _.Gender)
					.ToDictionary(_ => _.Key, _ => _.ToList());

				Assert.AreEqual(dictionary2.Count,               dictionary1.Count);
				Assert.AreEqual(dictionary2.First().Value.Count, dictionary1.First().Value.Count);

				var __ =
				(
					from p in db.Person
					group p by p.Gender into gr
					select new { gr.Key, Count = gr.Count() }
				)
				.ToDictionary(_ => _.Key);

				CheckGuardedQuery(db.Person.GroupBy(_ => _.Gender));
				CheckGuardedQuery(db.Person.GroupBy(_ => _));

				Assert.Throws<LinqToDBException>(() =>
				{
					db.Person
						.GroupBy(_ => _)
						.ToList();
				});

				Assert.DoesNotThrow(() =>
				{
					db.Person
						.GroupBy(_ => _)
						.DisableGuard()
						.ToList();
				});

			}
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static int Noop(int value)
		{
			throw new InvalidOperationException();
		}

		[Test]
		public void GroupByExpression2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Child
					.GroupBy(_ => Noop(_.ChildID))
					.Select(_ => new
					{
						x = _.Key,
						y = _.Average(r => r.ParentID)
					})
					.ToList();
			}
		}

		[Table("Stone")]
		public class Stone
		{
			[PrimaryKey, Identity] public int     Id           { get; set; } // int
			[Column, NotNull     ] public string  Name         { get; set; } = null!; // nvarchar(256)
			[Column, Nullable    ] public bool?   Enabled      { get; set; } // bit
			[Column, Nullable    ] public string? ImageFullUrl { get; set; } // nvarchar(255)
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56", Configurations = new[] { ProviderName.ClickHouseOctonica })]
		[Test]
		public void Issue672Test([DataSources(TestProvName.AllSybase)] string context)
		{
			using (new GuardGrouping(false))
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Stone>())
			{
				db.Insert(new Stone() { Id = 1, Name = "group1", Enabled = true, ImageFullUrl = "123" });
				db.Insert(new Stone() { Id = 2, Name = "group1", Enabled = true, ImageFullUrl = "123" });
				db.Insert(new Stone() { Id = 3, Name = "group2", Enabled = true, ImageFullUrl = "123" });

				IQueryable<Stone> stones;
				stones = from s in db.GetTable<Stone>() where s.Enabled == true select s;

				stones = from s in stones
						 where !s.Name.StartsWith("level - ") && s.ImageFullUrl!.Length > 0
						 group s by s.Name
							  into sG
						 select sG.First();

				var list = stones.ToList();
			}
		}

		[Table]
		sealed class Issue680Table
		{
			[Column] public DateTime TimeStamp;
		}

		[ActiveIssue(680)]
		[Test]
		public void Issue680Test([DataSources(false)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<Issue680Table>())
			{
				var result = (from record in table
							  group record by record.TimeStamp into g
							  select new
							  {
								  res = g.Count(r => r.TimeStamp > TestData.DateTime),
							  }).ToList();

				var index = db.LastQuery!.IndexOf("SELECT");
				Assert.AreNotEqual(-1, index);
				index = db.LastQuery.IndexOf("SELECT", index + 1);
				Assert.AreEqual(-1, index);
			}
		}

		[Test]
		public void Issue434Test1([DataSources] string context)
		{
			var input = "test";

			using (var db = GetDataContext(context))
			{
#pragma warning disable CA1311 // Specify a culture or use an invariant version
				var result = db.Person.GroupJoin(db.Patient, re => re.ID, ri => ri.PersonID, (re, ri) => new
				{
					Name = re.FirstName,
					Roles = ri.ToList().Select(p => p.Diagnosis)
				}).Where(p => p.Name.ToLower().Contains(input.ToLower())).ToList();
#pragma warning restore CA1311 // Specify a culture or use an invariant version
			}
		}

		[Test]
		public void Issue434Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
#pragma warning disable CA1311 // Specify a culture or use an invariant version
				var result = db.Person.GroupJoin(db.Patient, re => re.ID, ri => ri.PersonID, (re, ri) => new
				{
					Name = re.FirstName,
					Roles = ri.ToList().Select(p => p.Diagnosis)
				}).Where(p => p.Name.ToLower().Contains("test".ToLower())).ToList();
#pragma warning restore CA1311 // Specify a culture or use an invariant version
			}
		}

		[Table(Name = "Issue913Test")]
		public class Instrument
		{
			[Column, PrimaryKey, NotNull] public int InstrumentID { get; set; } // int
			[Column(Length = 1), Nullable] public TradingStatus? TradingStatus { get; set; } // char(1)

			public static readonly Instrument[] Data = new[]
			{
				new Instrument() { InstrumentID = 1 },
				new Instrument() { InstrumentID = 2, TradingStatus = GroupByTests.TradingStatus.Active },
				new Instrument() { InstrumentID = 3, TradingStatus = GroupByTests.TradingStatus.Delisted }
			};
		}

		public enum TradingStatus
		{
			[MapValue("A")] Active,
			[MapValue("D")] Delisted,
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56 + https://github.com/ClickHouse/ClickHouse/issues/37999", Configurations = new[] { ProviderName.ClickHouseMySql, ProviderName.ClickHouseOctonica })]
		[Test]
		public void Issue913Test([DataSources] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(Instrument.Data))
			{
				var q = from i in table
					group i by new
					{
						IsDelisted = i.TradingStatus == TradingStatus.Delisted
					}
					into g
						select new
						{
							g.Key.IsDelisted,
							Count = g.Count(),
						};

				var x = q.ToList().OrderBy(_ => _.Count).ToArray();

				Assert.AreEqual(2, x.Length);
				Assert.True(x[0].IsDelisted);
				Assert.AreEqual(1, x[0].Count);
				Assert.False(x[1].IsDelisted);
				Assert.AreEqual(2, x[1].Count);
			}
		}

		sealed class Issue1078Table
		{
			[PrimaryKey]
			public int UserID { get; set; }
			[Column]
			public int SiteID { get; set; }
			[Column]
			public bool Active { get; set; }

			public static readonly Issue1078Table[] TestData = new []
			{
				new Issue1078Table() { UserID = 1, SiteID = 1, Active = true  },
				new Issue1078Table() { UserID = 2, SiteID = 1, Active = false },
				new Issue1078Table() { UserID = 3, SiteID = 1, Active = true  },
				new Issue1078Table() { UserID = 4, SiteID = 2, Active = false },
				new Issue1078Table() { UserID = 5, SiteID = 2, Active = true  },
				new Issue1078Table() { UserID = 6, SiteID = 2, Active = false },
				new Issue1078Table() { UserID = 7, SiteID = 2, Active = false },
				new Issue1078Table() { UserID = 8, SiteID = 3, Active = false },
				new Issue1078Table() { UserID = 9, SiteID = 4, Active = true  },
			};
		}

		[ActiveIssue(1078, Details = "Disabled providers doesn't support sub-query columns and use join (which is not correct right now)", Configurations = new[] { TestProvName.AllAccess, ProviderName.SqlCe })]
		[Test]
		public void Issue1078Test([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Issue1078Table.TestData))
			{
				var query =
					from u in table
					group u.Active ? 1 : 0 by u.SiteID into grp
					select new
					{
						SiteID   = grp.Key,
						Total    = grp.Count(),
						Inactive = grp.Count(_ => _ == 0)
					};

				var res = query.ToList().OrderBy(_ => _.SiteID).ToArray();

				Assert.AreEqual(4, res.Length);

				Assert.AreEqual(1, res[0].SiteID);
				Assert.AreEqual(3, res[0].Total);
				Assert.AreEqual(1, res[0].Inactive);

				Assert.AreEqual(2, res[1].SiteID);
				Assert.AreEqual(4, res[1].Total);
				Assert.AreEqual(3, res[1].Inactive);

				Assert.AreEqual(3, res[2].SiteID);
				Assert.AreEqual(1, res[2].Total);
				Assert.AreEqual(1, res[2].Inactive);

				Assert.AreEqual(4, res[3].SiteID);
				Assert.AreEqual(1, res[3].Total);
				Assert.AreEqual(0, res[3].Inactive);
			}
		}

		sealed class Issue1192Table
		{
			public int IdId { get; internal set; }
			public int MyOtherId { get; internal set; }
			public int Status { get; internal set; }
		}

		[Test]
		public void Issue1198Test([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<Issue1192Table>())
			{
				var stats = (from t in table
							 where t.MyOtherId == 12
							 group t by 1 into g
							 select new
							 {
								 MyGroupedCount = g.Count(i => i.Status == 3),
							 }).FirstOrDefault();
			}
		}

		[Test]
		public void Issue2306Test1([DataSources] string context)
		{
			Query.ClearCaches();
			using (var db = GetDataContext(context))
			using (new GuardGrouping(false))
			{
				db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}

			Query.ClearCaches();
			using (var db = GetDataContext(context))
			using (new GuardGrouping(true))
			{
				Assert.Throws<LinqToDBException>(() => db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList()));
			}

			Query.ClearCaches();
			using (var db = GetDataContext(context))
			using (new GuardGrouping(true))
			{
				db.Person.GroupBy(p => p.ID).DisableGuard().ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}

			Query.ClearCaches();
		}

		[Test]
		public void Issue2306Test2([DataSources] string context)
		{
			Query.ClearCaches();
			using (var db = GetDataContext(context))
			using (new GuardGrouping(false))
			{
				db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}

			using (var db = GetDataContext(context))
			using (new GuardGrouping(true))
			{
				Assert.Throws<LinqToDBException>(() => db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList()));
			}

			using (var db = GetDataContext(context))
			using (new GuardGrouping(true))
			{
				db.Person.GroupBy(p => p.ID).DisableGuard().ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}
			Query.ClearCaches();
		}

		[Test]
		public void Issue2306Test3([DataSources] string context)
		{
			Query.ClearCaches();
			using (var db = GetDataContext(context))
			using (new GuardGrouping(true))
			{
				Assert.Throws<LinqToDBException>(() => db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList()));
			}

			using (var db = GetDataContext(context))
			using (new GuardGrouping(false))
			{
				db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}

			using (var db = GetDataContext(context))
			using (new GuardGrouping(true))
			{
				db.Person.GroupBy(p => p.ID).DisableGuard().ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}
			Query.ClearCaches();
		}

		[Test]
		public void IssueGroupByNonTableColumn([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Select(_ => 1)
					.Concat(db.Person.Select(_ => 2))
					.GroupBy(_ => _)
					.Select(_ => new { _.Key, Count = _.Count() })
					.Where(_ => _.Key == 1)
					.Select(_ => _.Count)
					.Where(_ => _ > 1)
					.Count();
			}
		}

		[Test]
		public void GroupByWithSubquery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var emptyEquery = db.Person
					.Select(_ => 1)
					.Where(_ => false);

				var query = emptyEquery
					.GroupBy(_ => _)
					.Select(_ => new { _.Key, Count = _.Count() })
					.ToList();

				Assert.AreEqual(0, query.Count);
			}
		}

		[Test]
		public void Issue3668Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var id   = 1;
			var name = "test";

			// use two parameters with different types to ensure fix works with positional parameters
			var result = db.Person
				.Where(x => x.ID == id && x.LastName != name || x.FirstName != name && x.ID - 1 == id)
				.GroupBy(x => x.ID, x => x).DisableGuard()
				.ToList();

			foreach (var x in result)
			{
				foreach (var y in x)
				{
				}
			}
		}
	}
}
