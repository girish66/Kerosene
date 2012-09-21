// ========================================================
namespace MB.KeroseneORM.SQLServer.Direct
{
	using global::System;
	using global::System.Data.Common;
	using global::System.Data.SqlClient;
	using global::MB.KeroseneORM.Direct;

	// =====================================================
	/// <summary>
	///	Implements the <see cref="IKFactoryDirect"/> interface adapted for direct Microsoft SQL server databases.
	/// </summary>
	public class KFactoryDirectSQL : KFactorySQL, IKFactoryDirect
	{
		public override string ToString()
		{
			return "KFactoryDirectSQL";
		}

		/// <summary>
		///	Gets the appropriate <see cref="DbProviderFactory"/> object to use with direct Microsoft SQL Server databases.
		/// </summary>
		public SqlClientFactory DbProviderFactory
		{
			get { return SqlClientFactory.Instance; }
		}
		DbProviderFactory IKFactoryDirect.DbProviderFactory
		{
			get { return this.DbProviderFactory; }
		}
	}
}
// ========================================================
