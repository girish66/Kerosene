// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Text;
	using global::System.Linq.Expressions;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	/// <summary>
	/// Represents an object able to translate the nodes given to it given, typically but not only in the form of dynamic
	/// lambda expressions, into the syntax expected by the personality of the database defined by its Factory property.
	/// </summary>
	public class KParser : IDisposable
	{
		/// <summary>
		/// Extracts the tag from the given string, returning a new string without the tag.
		/// <para>The tag is, by default, defined as the left most part of the source string till the first dot is found.</para>
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="tag">If not null, the string that shall be considered the tag. If it does not end with a dot, then
		/// a dot is added to its end to perform the extraction.</param>
		/// <returns>A new string without the tag part.</returns>
		public static string ExtractTag( string source, string tag = null )
		{
			source = source.Validated( "Source" );

			if( tag == null ) {
				int n = source.IndexOf( '.' ); // Everything till the first dot considered as a tag...

				if( n >= 0 )
					if( n < ( source.Length - 1 ) )
						source = source.Substring( n + 1 );
			}
			else {
				if( tag.StartsWith( "." ) ) throw new ArgumentException( "Tag '.' is invalid." );
				if( !tag.EndsWith( "." ) ) tag = tag + ".";

				if( source.StartsWith( tag ) ) source = source.Remove( 0, tag.Length );
			}
			return source;
		}
		
		/// <summary>
		/// Extracts the alias part from the source string with the form "main AS alias".
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="alias">Out argument where to place the alias if found, or will be set to null.</param>
		/// <returns>A new string containing the main part.</returns>
		public static string ExtractAlias( string source, out string alias )
		{
			source = source.Validated( "Source" );
			int n = source.LastIndexOf( " AS ", StringComparison.OrdinalIgnoreCase );

			if( n < 0 ) alias = null;
			else {
				alias = source.Substring( n + 4 );
				source = source.Substring( 0, n );
			}
			return source;
		}

		/// <summary>
		/// Splits a multipart name and returns a tuple with its table and column parts.
		/// <para>The table part can be null if the source string is not a multipart one.</para>
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="sourceWithTag">Whether the source string contains a tag that should be ignored.</param>
		/// <returns>A tuple containing the table and column parts.</returns>
		public static Tuple<string, string> ExtractMultipart( string source, bool sourceWithTag = true )
		{
			if( source == null ) throw new ArgumentNullException( "source", "Source cannot be null." );

			if( sourceWithTag ) source = ExtractTag( source );
			source = source.Validated( "Source", invalidChars: TypeHelper.InvalidMultiPartNameChars );

			string table = null;
			string column = null;
			string[] items = source.Split( '.' );

			if( items.Length == 1 ) { column = items[0]; }
			else if( items.Length == 2 ) { table = items[0]; column = items[1]; }
			else throw new ArgumentException( "Invalid multipart source specification: " + source );

			return new Tuple<string, string>( table, column );
		}

		// --------------------------------------------------
		IKFactory _Factory = null;

		/// <summary>
		/// Creates a new instance of <see cref="KParser"/> associated with the given factory.
		/// </summary>
		/// <param name="factory">The factory this parser will be associated with.</param>
		public KParser( IKFactory factory )
		{
			DEBUG.IndentLine( "\n-- KParser( Factory={0} )", factory == null ? "null" : factory.ToString() );
			if( ( _Factory = factory ) == null ) throw new ArgumentNullException( "factory", "Factory cannot be null." );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KParser.Dispose( Disposing={0} ) - This={1}", disposing, this );
			_Factory = null;
			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KParser.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KParser()
		{
			DEBUG.IndentLine( "\n-- KParser~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KParser[ {0} ]", _Factory == null ? "null" : _Factory.ToString() );
		}

		/// <summary>
		/// Gets the factory this parser is associated with.
		/// </summary>
		public IKFactory Factory
		{
			get { return _Factory; }
		}

		/// <summary>
		/// Parsers the given node and returns a string with its contents translated.
		/// <para>The node is typically a dynamic lambda expression, but can also be any valid C# object.</para>
		/// <para>In this case, typically a new parameter is created to contain  its value, who is stored in the parameter
		/// list given (if it is not null), and the parameter's name is placed into the returned string.</para>
		/// </summary>
		/// <param name="node">The object to parse.</param>
		/// <param name="pars">The parameters list where to store the parameters created.</param>
		/// <param name="rawstr">If raw strings are considered valid, or not acceptable.</param>
		/// <param name="nulls">If null nodes are considered valid, or not acceptable.</param>
		/// <returns>The parsed string.</returns>
		public virtual string Parse( object node, KParameterList pars = null, bool rawstr = false, bool nulls = false )
		{
			// Null nodes are accepted or not depending upon the "nulls" flag...
			if( node == null ) {
				if( !nulls ) throw new ArgumentNullException( "node", "Null nodes are not accepted." );
				return Dispatch( node, pars );
			}

			// Nodes that are strings are parametrized or not depending the "rawstr" flag...
			if( node is string ) {
				if( rawstr ) return (string)node;
				else return Dispatch( node, pars );
			}

			// If node is a delegate, parse it to create the logical tree...
			if( node is Delegate ) {
				node = DynamicParser.Parse( (Delegate)node ).Result;
				return Parse( node, pars, rawstr ); // Intercept containers as in (x => "string")
			}

			return Dispatch( node, pars );
		}
		string Dispatch( object node, KParameterList pars = null )
		{
			if( node != null ) {
				// Flag to indicate the root of a dynamic argument...
				if( node.GetType() == typeof( DynamicNode.Argument ) ) return null;

				// Other node types and elements to parse...
				else if( node is IKCommand ) return ParseCommand( (IKCommand)node, pars );
				else if( node is DynamicNode.GetMember ) return ParseGetMember( (DynamicNode.GetMember)node, pars );
				else if( node is DynamicNode.SetMember ) return ParseSetMember( (DynamicNode.SetMember)node, pars );
				else if( node is DynamicNode.Unary ) return ParseUnary( (DynamicNode.Unary)node, pars );
				else if( node is DynamicNode.Binary ) return ParseBinary( (DynamicNode.Binary)node, pars );
				else if( node is DynamicNode.Method ) return ParseMethod( (DynamicNode.Method)node, pars );
				else if( node is DynamicNode.Invoke ) return ParseInvoke( (DynamicNode.Invoke)node, pars );
				else if( node is DynamicNode.Convert ) return ParseConvert( (DynamicNode.Convert)node, pars );
			}

			// All other cases are considered constant parameters...
			return ParseConstant( node, pars );
		}

		protected virtual string ParseConstant( object node, KParameterList pars = null )
		{
			if( node == null ) return ParseNull();

			if( pars != null ) { // If we have a list of parameters to store it, let's parametrize it
				var par = pars.Insert( node );
				return par.Name;
			}
			return node.ToString(); // Last resort case
		}
		protected virtual string ParseNull()
		{
			// When participating in a comparison, ParseBinary() shall intercept with "IS NULL" or "IS NOT NULL"...
			// So this is a fallback for assignations...

			return "NULL"; // Override if needed
		}

		protected virtual string ParseCommand( IKCommand node, KParameterList pars = null )
		{
			// Getting the command's text...
			string str = node.CommandText( iterable: false ); // Avoiding spurious "OUTPUT XXX" statements

			// If there are parameters to transform, but cannot store them, it is an error
			if( node.Parameters.Count != 0 && pars == null )
				throw new InvalidOperationException( "Cannot parse IKCommand because the receiving parameters collection is null." );

			// Transforming the parameters using names compatible with this command string and context
			foreach( var parameter in node.Parameters ) {
				KParameter neo = pars.Insert( parameter.Value );
				str = str.Replace( parameter.Name, neo.Name );
			}

			return str;
		}
		protected virtual string ParseGetMember( DynamicNode.GetMember node, KParameterList pars = null )
		{
			string parent = Parse( node.Host, pars );
			string name = parent == null ? node.Name : string.Format( "{0}.{1}", parent, node.Name );
			return name;
		}
		protected virtual string ParseSetMember( DynamicNode.SetMember node, KParameterList pars = null )
		{
			string parent = Parse( node.Host, pars );
			string name = parent == null ? node.Name : string.Format( "{0}.{1}", parent, node.Name );
			string value = Parse( node.Value, pars, nulls: true );
			return string.Format( "{0} = ( {1} )", name, value );
		}
		protected virtual string ParseUnary( DynamicNode.Unary node, KParameterList pars = null )
		{
			switch( node.Operation ) {
				// Artifacts from the DynamicParser class that are not usefull here...
				case ExpressionType.IsFalse:
				case ExpressionType.IsTrue: return Parse( node.Target, pars );

				// Unary supported operations...
				case ExpressionType.Not: return string.Format( "( NOT {0} )", Parse( node.Target, pars ) );
				case ExpressionType.Negate: return string.Format( "!( {0} )", Parse( node.Target, pars ) );
			}
			throw new ArgumentException( "Not supported unary operation: " + node );
		}
		protected virtual string ParseBinary( DynamicNode.Binary node, KParameterList pars = null )
		{
			string op = ""; switch( node.Operation ) {
				case ExpressionType.Add: op = "+"; break;
				case ExpressionType.Subtract: op = "-"; break;
				case ExpressionType.Multiply: op = "*"; break;
				case ExpressionType.Divide: op = "/"; break;
				case ExpressionType.Modulo: op = "%"; break;
				case ExpressionType.Power: op = "^"; break;

				case ExpressionType.And: op = "AND"; break;
				case ExpressionType.Or: op = "OR"; break;

				// Treating NULL targets as special cases...
				case ExpressionType.Equal: op = node.Right == null ? "IS" : "="; break;
				case ExpressionType.NotEqual: op = node.Right == null ? "IS NOT" : "!="; break;

				case ExpressionType.GreaterThan: op = ">"; break;
				case ExpressionType.GreaterThanOrEqual: op = ">="; break;
				case ExpressionType.LessThan: op = "<"; break;
				case ExpressionType.LessThanOrEqual: op = "<="; break;

				default: throw new ArgumentException( "Not supported operator: '" + node.Operation + "'." );
			}
			string left = Parse( node.Left, pars ); // Not nulls: left is assumed to be an object
			string right = Parse( node.Right, pars, nulls: true );
			return string.Format( "( {0} {1} {2} )", left, op, right );
		}
		protected virtual string ParseMethod( DynamicNode.Method node, KParameterList pars = null )
		{
			string parent = Parse( node.Host, pars );
			string method = node.Name.ToUpper();
			string item = null;

			// ROOT-LEVEL...
			if( parent == null ) {
				switch( method ) {
					case "NOT":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "NOT operator expects one argument." );
						item = Parse( node.Arguments[0], pars );
						return string.Format( "( NOT {0} )", item );
				}
			}
			// COLUMN-LEVEL...
			else {
				switch( method ) {
					case "IN":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "IN operator expects one argument." );
						item = Parse( node.Arguments[0], pars );
						return string.Format( "{0} IN ( {1} )", parent, item );
					case "AS":
						if( node.Arguments == null || node.Arguments.Length != 1 ) throw new ArgumentException( "AS operator expects one argument." );
						item = Parse( node.Arguments[0], null, rawstr: true ); // pars=null to avoid parametrize aliases
						item = item.Validated( "Alias", invalidChars: TypeHelper.InvalidNameChars );
						return string.Format( "{0} AS {1}", parent, item );
				}
			}

			// DEFAULT-CASE...
			method = parent == null ? node.Name : string.Format( "{0}.{1}", parent, node.Name );
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat( "{0}(", method ); if( node.Arguments != null && node.Arguments.Length != 0 ) {
				sb.Append( " " );
				bool first = true; foreach( object argument in node.Arguments ) {
					if( !first ) sb.Append( ", " ); else first = false;
					sb.Append( Parse( argument, pars, nulls: true ) ); // We don't accept raw strings here!!!
				}
				sb.Append( " " );
			}
			sb.Append( ")" );
			return sb.ToString();

		}
		protected virtual string ParseInvoke( DynamicNode.Invoke node, KParameterList pars = null )
		{
			// This is used as an especial syntax to merely concatenate its arguments separated by spaces if needed...
			// It is used as a way to extend the supported syntax without the need of treating all the possible cases...

			if( node.Arguments == null ) return string.Empty;
			if( node.Arguments.Length == 0 ) return string.Empty;

			StringBuilder sb = new StringBuilder();
			bool first = true;

			foreach( object arg in node.Arguments ) {

				if( !first ) {
					bool space = true;
					if( ( arg is string ) && ( (string)arg ).StartsWith( " " ) ) space = false;
					if( space ) sb.Append( " " );
				}
				else first = false;

				if( arg is string ) sb.Append( (string)arg );
				else sb.Append( Parse( arg, pars, rawstr: true, nulls: true ) );
			}

			return sb.ToString();
		}

		protected virtual string ParseConvert( DynamicNode.Convert node, KParameterList pars = null )
		{
			DEBUG.WriteLine( "\n-- PENDING: KParser.ParseConvert() requires an override..." );

			string r = Parse( node.SourceNode, pars );
			return r;
		}
	}
}
// ========================================================
