using System.Data;
using NHibernate.Mapping;
using NHibernate.SqlCommand;

namespace NHibernate.Dialect
{
	public class MsSql2005Dialect : MsSql2000Dialect
	{
		///<remarks>http://stackoverflow.com/a/7264795/259946</remarks>
		public const int MaxSizeForXml = 2147483647; // int.MaxValue

		public MsSql2005Dialect()
		{
			RegisterColumnType(DbType.Xml, "XML");
		}

		protected override void RegisterCharacterTypeMappings()
		{
			base.RegisterCharacterTypeMappings();
			RegisterColumnType(DbType.String, MaxSizeForClob, "NVARCHAR(MAX)");
			RegisterColumnType(DbType.AnsiString, MaxSizeForAnsiClob, "VARCHAR(MAX)");
		}

		protected override void RegisterLargeObjectTypeMappings()
		{
			base.RegisterLargeObjectTypeMappings();
			RegisterColumnType(DbType.Binary, "VARBINARY(MAX)");
			RegisterColumnType(DbType.Binary, MaxSizeForLengthLimitedBinary, "VARBINARY($l)");
			RegisterColumnType(DbType.Binary, MaxSizeForBlob, "VARBINARY(MAX)");
		}

		protected override void RegisterKeywords()
		{
			base.RegisterKeywords();
			RegisterKeyword("xml");
		}

		public override SqlString GetLimitString(SqlString queryString, SqlString offset, SqlString limit)
		{
			return new MsSql2005DialectQueryPager(queryString).PageBy(offset, limit);
		}

		/// <summary>
		/// Sql Server 2005 supports a query statement that provides <c>LIMIT</c>
		/// functionality.
		/// </summary>
		/// <value><c>true</c></value>
		public override bool SupportsLimit
		{
			get { return true; }
		}

		/// <summary>
		/// Sql Server 2005 supports a query statement that provides <c>LIMIT</c>
		/// functionality with an offset.
		/// </summary>
		/// <value><c>true</c></value>
		public override bool SupportsLimitOffset
		{
			get { return true; }
		}

		public override bool SupportsVariableLimit
		{
			get { return true; }
		}

		protected override string GetSelectExistingObject(string name, Table table)
		{
			string schema = table.GetQuotedSchemaName(this);
			if (schema != null)
			{
				schema += ".";
			}
			string objName = string.Format("{0}{1}", schema, Quote(name));
			string parentName = string.Format("{0}{1}", schema, table.GetQuotedName(this));
			return
				string.Format(
					"select 1 from sys.objects where object_id = OBJECT_ID(N'{0}') AND parent_object_id = OBJECT_ID('{1}')", objName,
					parentName);
		}

		/// <summary>
		/// Sql Server 2005 supports a query statement that provides <c>LIMIT</c>
		/// functionality with an offset.
		/// </summary>
		/// <value><c>false</c></value>
		public override bool UseMaxForLimit
		{
			get { return false; }
		}

		public override string AppendLockHint(LockMode lockMode, string tableName)
		{
			if (NeedsLockHint(lockMode))
			{
				if (lockMode == LockMode.UpgradeNoWait)
				{
					return tableName + " with (updlock, rowlock, nowait)";
				}

				return tableName + " with (updlock, rowlock)";
			}

			return tableName;
		}

		// SQL Server 2005 supports 128.
		/// <inheritdoc />
		public override int MaxAliasLength => 128;

		#region Overridden informational metadata

		/// <summary>
		/// We assume that applications using this dialect are using
		/// SQL Server 2005 snapshot isolation modes.
		/// </summary>
		public override bool DoesReadCommittedCauseWritersToBlockReaders => false;

		/// <summary>
		/// We assume that applications using this dialect are using
		/// SQL Server 2005 snapshot isolation modes.
		/// </summary>
		public override bool DoesRepeatableReadCauseReadersToBlockWriters => false;

		#endregion
	}
}
