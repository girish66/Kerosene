// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.SQLServer.Direct
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Data.SqlClient;
	using global::MB.Tools;
	using global::MB.KeroseneORM.Direct;

	// =====================================================
	public class KLinkDirectSQL : KLinkDirect, IKLink
	{
		/// <summary>
		/// Creates a new <see cref="KLinkDirectSQL"/> instance to be used with direct Microsoft SQL Server databases.
		/// It automatically creates an instance of <see cref="KFactoryDirectSQL"/> in order to specify its personality.
		/// </summary>
		/// <param name="connString">The connection string to be used to create connections when needed.</param>
		/// <param name="caseSensitiveNames">Whether the names of tables and columns in the database are to be consider case
		/// sensitive or not.</param>
		/// <param name="mode">The default transactional mode to use when creating automatic transactions.</param>
		public KLinkDirectSQL( string connString, bool caseSensitiveNames = false,
			KTransactionMode mode = KTransactionMode.Scope )
			: base( new KFactoryDirectSQL(), connString, caseSensitiveNames, mode )
		{
			DEBUG.IndentLine( "\n-- KLinkDirectSQL( Factory, Connection, CaseSensitive, Mode )" );
			DEBUG.Unindent();
		}

		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KLinkDirectSQL.Dispose( Disposing={0} ) - This={1}", disposing, this );
			base.Dispose( disposing );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KLinkDirectSQL[{0}.{1}]", _Server ?? "null", _Catalog ?? "null" );
		}

		/// <summary>
		/// Gets the <see cref="KFactoryDirectSQL"/> that describes that the personality of the direct database this link
		/// object will communicate with is a Microsoft SQL Server database.
		/// </summary>
		public new KFactoryDirectSQL Factory
		{
			get { return (KFactoryDirectSQL)base.Factory; }
		}

		/// <summary>
		/// Gets the connection in use by this link instance, or null. If it is not null, it has been created automatically
		/// by this link object, and it will be managed automatically by it as needed. it is useful if you wish to obtain
		/// information from it, or for instance to create your own transactions.
		/// </summary>
		public new SqlConnection DbConnection
		{
			get { return (SqlConnection)base.DbConnection; }
		}

		/// <summary>
		/// Clones this link object, cloning also all the relevant elements it conceptually contains. If any of those
		/// implements the <see cref="IKCloneable"/> interface, they are cloned using its Clone(link) method where the
		/// link instance is this one. Otherwise, they are cloned only if they implement the <see cref="ICloneable"/>
		/// interface and, if not, their original value/reference is used for the new cloned link.
		/// </summary>
		/// <returns></returns>
		public new KLinkDirectSQL Clone()
		{
			var cloned = new KLinkDirectSQL( DbConnectionString, DbCaseSensitiveNames, TransactionMode ); OnClone( cloned );
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}
	}
}
// ========================================================
