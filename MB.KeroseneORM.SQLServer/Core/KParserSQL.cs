// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.SQLServer
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	/// <summary>
	/// Represents a parser adapted for Microsoft SQL Server databases. A parser is an object able to translate the nodes
	/// given, typically but not only in the form of dynamic lambda expressions, into the syntax expected by the actual
	/// database.
	/// </summary>
	public class KParserSQL : KParser
	{
		/// <summary>
		/// Creates a new <see cref="KParserSQL"/> instance associated with the factory object given.
		/// </summary>
		/// <param name="factory">The factory object this parser will be associated to.</param>
		public KParserSQL( KFactorySQL factory ) : base( factory )
		{
			DEBUG.IndentLine( "\n-- KParserSQL( Factory )" );
			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KParserSQL.Dispose( Disposing={0} ) - This={1}", disposing, this );
			base.Dispose( disposing );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return "KParserSQL";
		}

		/// <summary>
		/// Gets the factory object this parser is associated with.
		/// </summary>
		public new KFactorySQL Factory
		{
			get { return (KFactorySQL)base.Factory; }
		}

		protected override string ParseMethod( DynamicNode.Method node, KParameterList pars )
		{
			string host = Parse( node.Host, pars );
			string name = node.Name.ToUpper();
			string item = null;
			string alias = null;
			string extra = null;

			// ROOT-LEVEL...
			if( host == null ) {
				switch( name ) {
					case "CAST":
						if( node.Arguments == null || node.Arguments.Length != 2 ) throw new ArgumentException( "CAST operator expects two arguments." );
						item = Parse( node.Arguments[0], pars ); // No raw strings
						alias = Parse( node.Arguments[1], null, rawstr: true ); // pars=null to avoid parametrize aliases
						alias = alias.Validated( "Alias", invalidChars: TypeHelper.InvalidNameChars );
						return string.Format( "CAST( {0} AS {1} )", item, alias );
				}
			}
			// COLUMN-LEVEL...
			else {
				switch( name ) {
					case "LEFT":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "LEFT operator expects one argument." );
						item = Parse( node.Arguments[0], pars );
						return string.Format( "LEFT( {0}, {1} )", host, item );
					case "RIGHT":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "RIGHT operator expects one argument." );
						item = Parse( node.Arguments[0], pars );
						return string.Format( "RIGHT( {0}, {1} )", host, item );

					case "LEN":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "LEN operator does not expect any arguments." );
						return string.Format( "LEN( {0} )", host );
					case "LOWER":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "LOWER operator does not expect any arguments." );
						return string.Format( "LOWER( {0} )", host );
					case "UPPER":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "UPPER operator does not expect any arguments." );
						return string.Format( "UPPER( {0} )", host );

					case "YEAR":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "YEAR operator does not expect any arguments." );
						return string.Format( "DATEPART( YEAR, {0} )", host );
					case "MONTH":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "MONTH operator does not expect any arguments." );
						return string.Format( "DATEPART( MONTH, {0} )", host );
					case "DAY":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "DAY operator does not expect any arguments." );
						return string.Format( "DATEPART( DAY, {0} )", host );
					case "HOUR":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "HOUR operator does not expect any arguments." );
						return string.Format( "DATEPART( HOUR, {0} )", host );
					case "MINUTE":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "MINUTE operator does not expect any arguments." );
						return string.Format( "DATEPART( MINUTE, {0} )", host );
					case "SECOND":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "SECOND operator does not expect any arguments." );
						return string.Format( "DATEPART( SECOND, {0} )", host );
					case "MILLISECOND":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "MILLISECOND operator does not expect any arguments." );
						return string.Format( "DATEPART( MILLISECOND, {0} )", host );
					case "OFFSET":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "OFFSET operator does not expect any arguments." );
						return string.Format( "DATEPART( TZ, {0} )", host );

					case "LIKE":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "LIKE operator expects one argument." );
						item = Parse( node.Arguments[0], pars );
						return string.Format( "( {0} LIKE {1} )", host, item );
					case "NOTLIKE":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "NOT LIKE operator expects one argument." );
						item = Parse( node.Arguments[0], pars );
						return string.Format( "( {0} NOT LIKE {1} )", host, item );
					case "CONTAINS":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "CONTAINS operator expects one argument." );
						item = Parse( node.Arguments[0], pars );
						return string.Format( "CONTAINS( {0}, {1} )", host, item );
					case "PATINDEX":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "PATINDEX operator expects one argument." );
						item = Parse( node.Arguments[0], pars );
						return string.Format( "PATINDEX( {1}, {0} )", host, item ); // Note {0} and {1} are reversed
					case "SUBSTRING":
						if( node.Arguments == null || node.Arguments.Length != 2 ) throw new ArgumentException( "SUBSTRING operator expects two arguments." );
						item = Parse( node.Arguments[0], pars );
						extra = Parse( node.Arguments[1], pars );
						return string.Format( "SUBSTRING( {0}, {1}, {2} )", host, item, extra );

					case "LTRIM":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "LTRIM operator does not expect any arguments." );
						return string.Format( "LTRIM( {0} )", host );
					case "RTRIM":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "RTRIM operator does not expect any arguments." );
						return string.Format( "RTRIM( {0} )", host );
					case "TRIM":
						if( node.Arguments != null && node.Arguments.Length != 0 ) throw new ArgumentException( "TRIM operator does not expect any arguments." );
						return string.Format( "LTRIM( RTRIM( {0} ) )", host );
				}
			}

			// DEFAULT case...
			return base.ParseMethod( node, pars );
		}
	}
}
// ========================================================
