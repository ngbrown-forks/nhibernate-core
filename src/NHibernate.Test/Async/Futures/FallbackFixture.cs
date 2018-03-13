﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Linq;
using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Criterion;
using NHibernate.Driver;
using NHibernate.Linq;
using NUnit.Framework;

using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Test.Futures
{
	using System.Threading.Tasks;
	using System.Threading;

	/// <summary>
	/// I'm using a Driver which derives from SqlServer2000Driver to
	/// return false for the SupportsMultipleQueries property. This is purely to test the way NHibernate
	/// will behave when the driver that's being used does not support multiple queries... so even though
	/// the test is using MsSql, it's only relevant for databases that don't support multiple queries
	/// but this way it's just much easier to test this
	/// </summary>
	[TestFixture]
	public class FallbackFixtureAsync : FutureFixture
	{
		protected override bool AppliesTo(Dialect.Dialect dialect)
		{
			using (var cp = ConnectionProviderFactory.NewConnectionProvider(cfg.Properties))
			{
				return !cp.Driver.SupportsMultipleQueries;
			}
		}

		protected override void Configure(Configuration configuration)
		{
			base.Configure(configuration);
			using (var cp = ConnectionProviderFactory.NewConnectionProvider(cfg.Properties))
			{
				if (cp.Driver.IsSqlClientDriver())
				{
					configuration.Properties[Environment.ConnectionDriver] =
						typeof(TestDriverThatDoesntSupportQueryBatching).AssemblyQualifiedName;
				}
			}
		}

		protected override void OnTearDown()
		{
			using (var session = Sfi.OpenSession())
			{
				session.Delete("from Person");
				session.Flush();
			}

			base.OnTearDown();
		}

		[Test]
		public async Task FutureOfCriteriaFallsBackToListImplementationWhenQueryBatchingIsNotSupportedAsync()
		{
			using (var session = Sfi.OpenSession())
			{
				var results = session.CreateCriteria<Person>().Future<Person>();
				(await (results.GetEnumerableAsync())).GetEnumerator().MoveNext();
			}
		}

		[Test]
		public async Task FutureValueOfCriteriaCanGetSingleEntityWhenQueryBatchingIsNotSupportedAsync()
		{
			int personId = await (CreatePersonAsync());

			using (var session = Sfi.OpenSession())
			{
				var futurePerson = session.CreateCriteria<Person>()
					.Add(Restrictions.Eq("Id", personId))
					.FutureValue<Person>();
				Assert.IsNotNull(await (futurePerson.GetValueAsync()));
			}
		}

		[Test]
		public async Task FutureValueOfCriteriaCanGetScalarValueWhenQueryBatchingIsNotSupportedAsync()
		{
			await (CreatePersonAsync());

			using (var session = Sfi.OpenSession())
			{
				var futureCount = session.CreateCriteria<Person>()
					.SetProjection(Projections.RowCount())
					.FutureValue<int>();
				Assert.That(await (futureCount.GetValueAsync()), Is.EqualTo(1));
			}
		}

		[Test]
		public async Task FutureOfQueryFallsBackToListImplementationWhenQueryBatchingIsNotSupportedAsync()
		{
			using (var session = Sfi.OpenSession())
			{
				var results = session.CreateQuery("from Person").Future<Person>();
				(await (results.GetEnumerableAsync())).GetEnumerator().MoveNext();
			}
		}

		[Test]
		public async Task FutureValueOfQueryCanGetSingleEntityWhenQueryBatchingIsNotSupportedAsync()
		{
			int personId = await (CreatePersonAsync());

			using (var session = Sfi.OpenSession())
			{
				var futurePerson = session.CreateQuery("from Person where Id = :id")
					.SetInt32("id", personId)
					.FutureValue<Person>();
				Assert.IsNotNull(await (futurePerson.GetValueAsync()));
			}
		}

		[Test]
		public async Task FutureValueOfQueryCanGetScalarValueWhenQueryBatchingIsNotSupportedAsync()
		{
			await (CreatePersonAsync());

			using (var session = Sfi.OpenSession())
			{
				var futureCount = session.CreateQuery("select count(*) from Person")
					.FutureValue<long>();
				Assert.That(await (futureCount.GetValueAsync()), Is.EqualTo(1L));
			}
		}

		[Test]
		public async Task FutureOfLinqFallsBackToListImplementationWhenQueryBatchingIsNotSupportedAsync()
		{
			using (var session = Sfi.OpenSession())
			{
				var results = session.Query<Person>().ToFuture();
				(await (results.GetEnumerableAsync())).GetEnumerator().MoveNext();
			}
		}

		[Test]
		public async Task FutureValueOfLinqCanGetSingleEntityWhenQueryBatchingIsNotSupportedAsync()
		{
			var personId = await (CreatePersonAsync());

			using (var session = Sfi.OpenSession())
			{
				var futurePerson = session.Query<Person>()
					.Where(x => x.Id == personId)
					.ToFutureValue();
				Assert.IsNotNull(await (futurePerson.GetValueAsync()));
			}
		}

		[Test]
		public async Task FutureValueWithSelectorOfLinqCanGetSingleEntityWhenQueryBatchingIsNotSupportedAsync()
		{
			var personId = await (CreatePersonAsync());

			using (var session = OpenSession())
			{
				var futurePerson = session
					.Query<Person>()
					.Where(x => x.Id == personId)
					.ToFutureValue(q => q.FirstOrDefault());
				Assert.IsNotNull(await (futurePerson.GetValueAsync()));
			}
		}

		private async Task<int> CreatePersonAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var session = Sfi.OpenSession())
			{
				var person = new Person();
				await (session.SaveAsync(person, cancellationToken));
				await (session.FlushAsync(cancellationToken));
				return person.Id;
			}
		}
	}
}
