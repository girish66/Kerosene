// ========================================================
namespace MB.KeroseneORM.SQLServer
{
	using global::System;

	// =====================================================
	/// <summary>
	/// Represents a Microsoft SQL Server database personality.
	/// </summary>
	public class KFactorySQL : IKFactory
	{
		public override string ToString()
		{
			return "KFactorySQL";
		}

		/// <summary>
		/// Creates a new <see cref="KParameterList"/> instance adapted for Microsoft SQL Server databases.
		/// </summary>
		/// <returns>A new parameter list adapted for Microsoft SQL Server databases.</returns>
		public KParameterList CreateParameterList()
		{
			return new KParameterList( "@p" );
		}

		/// <summary>
		/// Creates a new <see cref="KParserSQL"/> instance for a parser adapted to Microsoft SQL Server databases.
		/// </summary>
		/// <returns>A new parser adapted for this database type.</returns>
		public KParserSQL CreateParser()
		{
			return new KParserSQL( this );
		}
		KParser IKFactory.CreateParser()
		{
			return this.CreateParser();
		}
	}
}
// ========================================================
