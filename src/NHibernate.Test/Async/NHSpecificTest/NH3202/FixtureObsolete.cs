﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Data.Common;
using System.Text.RegularExpressions;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Util;
using NUnit.Framework;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Test.NHSpecificTest.NH3202
{
	using System.Threading.Tasks;
	[TestFixture]
	[Obsolete("Uses old driver")]
	public class FixtureObsoleteAsync : BugTestCase
	{
		protected override void Configure(Configuration configuration)
		{
			if (!(Dialect is MsSql2008Dialect))
				Assert.Ignore("Test is for MS SQL Server dialect only (custom dialect).");

			if (!ReflectHelper.ClassForName(cfg.GetProperty(Environment.ConnectionDriver)).IsSqlClientDriver())
				Assert.Ignore("Test is for MS SQL Server driver only (custom driver is used).");

			cfg.SetProperty(Environment.Dialect, typeof(OffsetStartsAtOneTestDialect).AssemblyQualifiedName);
			cfg.SetProperty(Environment.ConnectionDriver, typeof(OffsetTestObsoleteDriver).AssemblyQualifiedName);
		}

		private OffsetStartsAtOneTestDialect OffsetStartsAtOneTestDialect
		{
			get { return (OffsetStartsAtOneTestDialect)Sfi.Dialect; }
		}

		private OffsetTestObsoleteDriver CustomDriver
		{
			get { return (OffsetTestObsoleteDriver)Sfi.ConnectionProvider.Driver; }
		}

		protected override void OnSetUp()
		{
			CustomDriver.OffsetStartsAtOneTestDialect = OffsetStartsAtOneTestDialect;

			base.OnSetUp();

			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				session.Save(new SequencedItem(1));
				session.Save(new SequencedItem(2));
				session.Save(new SequencedItem(3));

				session.Flush();
				transaction.Commit();
			}
		}

		protected override void OnTearDown()
		{
			using (ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				s.Delete("from SequencedItem");
				t.Commit();
			}
			base.OnTearDown();
		}


		[Test]
		public async Task OffsetNotStartingAtOneSetsParameterToSkipValueAsync()
		{
			OffsetStartsAtOneTestDialect.ForceOffsetStartsAtOne = false;

			using (var session = OpenSession())
			{
				var item2 =
				await (session.QueryOver<SequencedItem>()
					.OrderBy(i => i.I).Asc
					.Take(1).Skip(2)
					.SingleOrDefaultAsync<SequencedItem>());

				Assert.That(CustomDriver.OffsetParameterValueFromCommand, Is.EqualTo(2));
			}
		}

		[Test]
		public async Task OffsetStartingAtOneSetsParameterToSkipValuePlusOneAsync()
		{
			OffsetStartsAtOneTestDialect.ForceOffsetStartsAtOne = true;

			using (var session = OpenSession())
			{
				var item2 =
				await (session.QueryOver<SequencedItem>()
					.OrderBy(i => i.I).Asc
					.Take(1).Skip(2)
					.SingleOrDefaultAsync<SequencedItem>());

				Assert.That(CustomDriver.OffsetParameterValueFromCommand, Is.EqualTo(3));
			}
		}
	}
}
