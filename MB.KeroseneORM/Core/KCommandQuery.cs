// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Text;
	using global::System.Collections;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	public static class KCommandQueryHelper
	{
		/// <summary>
		/// Creates a new <see cref="KCommandQuery"/> instance associated with this link object.
		/// </summary>
		/// <param name="link">The link the comand will be associated to.</param>
		/// <returns>The new created command.</returns>
		public static KCommandQuery Query( this IKLink link )
		{
			var cmd = new KCommandQuery( link );
			return cmd;
		}

		/// <summary>
		/// Creates a new <see cref="KCommandQuery"/> instance associated with this link object, and sets the initial
		/// contents of the From clause using the the given string, in the form "main AS alias" where the
		/// alias part is optional.
		/// </summary>
		/// <param name="link">The link the comand will be associated to.</param>
		/// <param name="fromSpec">The string to append to the From clause.</param>
		/// <returns>The new created command.</returns>
		public static KCommandQuery From( this IKLink link, string fromSpec )
		{
			var cmd = link.Query().From( fromSpec );
			return cmd;
		}
		
		/// <summary>
		/// Creates a new <see cref="KCommandQuery"/> instance associated with this link object, and sets the initial
		/// contents of the From clause by from parsing the dynamic lamnda expression given.
		/// <para>They typically use the syntax "x => x.Table.As( alias )", where the alias part is optional.</para>
		/// </summary>
		/// <param name="link">The link the comand will be associated to.</param>
		/// <param name="fromSpecs">The dynamic lambda expressions to be parsed.</param>
		/// <returns>The new created command.</returns>
		public static KCommandQuery From( this IKLink link, params Func<dynamic, object>[] fromSpecs )
		{
			var cmd = link.Query().From( fromSpecs );
			return cmd;
		}
		
		/// <summary>
		/// Creates a new <see cref="KCommandQuery"/> instance associated with this link object, and sets the initial
		/// contents of the From clause by parsing the command object given, and associating it to a given alias.
		/// </summary>
		/// <param name="link">The link the comand will be associated to.</param>
		/// <param name="command">The command object.</param>
		/// <param name="alias">The alias.</param>
		/// <returns>The new created command.</returns>
		public static KCommandQuery From( this IKLink link, IKCommand command, Func<dynamic, object> alias )
		{
			var cmd = link.Query().From( command, alias );
			return cmd;
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a Query/Select command.
	/// </summary>
	public class KCommandQuery : KCommand, IKCommand, IKCommandEnumerable, IKTableAliasListProvider
	{
		KTableAliasList _TableAliasList = null;
		protected string _TextTop = null;
		protected string _TextSelect = null;
		protected string _TextFrom = null;
		protected string _TextJoin = null;
		protected string _TextWhere = null;
		protected string _TextGroupBy = null;
		protected string _TextOrderBy = null;

		/// <summary>
		/// Creates a new instance of <see cref="KCommandQuery"/> associating it to the given link.
		/// </summary>
		/// <param name="link">The link this command will be associated to.</param>
		public KCommandQuery( IKLink link ) : base( link )
		{
			DEBUG.IndentLine( "\n-- KCommandQuery( Link )" );

			_TableAliasList = new KTableAliasList( Link.DbCaseSensitiveNames );

			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KCommandQuery.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _TableAliasList != null ) { _TableAliasList.Dispose(); _TableAliasList = null; }
			base.Dispose( disposing );

			DEBUG.Unindent();
		}

		/// <summary>
		/// Clones this command.
		/// </summary>
		/// <returns></returns>
		public KCommandQuery Clone()
		{
			var cloned = new KCommandQuery( this.Link ); OnClone( cloned );
			cloned._TableAliasList.AddRange( this._TableAliasList );
			cloned._TextTop = this._TextTop;
			cloned._TextSelect = this._TextSelect;
			cloned._TextFrom = this._TextFrom;
			cloned._TextJoin = this._TextJoin;
			cloned._TextWhere = this._TextWhere;
			cloned._TextGroupBy = this._TextGroupBy;
			cloned._TextOrderBy = this._TextOrderBy;
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Creates a new enumerator associated with this command.
		/// </summary>
		/// <returns>The new created enumerator.</returns>
		public IKEnumerator GetEnumerator()
		{
			return Link.CreateEnumerator( this );
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Gets the list of associations specified for this command.
		/// </summary>
		public KTableAliasList TableAliasList
		{
			get { return _TableAliasList; }
		}

		// --------------------------------------------------
		/// <summary>
		/// Returns the command's text as it has been specified by its methods.
		/// </summary>
		/// <param name="iterable">No use.</param>
		/// <returns>Returns the command's text as it has been specified.</returns>
		public override string CommandText( bool iterable )
		{
			StringBuilder sb = new StringBuilder( "SELECT" );
			if( _TextTop != null ) sb.AppendFormat( " TOP {0}", _TextTop );
			if( _TextSelect != null ) sb.AppendFormat( " {0}", _TextSelect ); else sb.Append( " *" );
			if( _TextFrom != null ) sb.AppendFormat( " FROM {0}", _TextFrom );
			if( _TextJoin != null ) sb.AppendFormat( " {0}", _TextJoin );
			if( _TextWhere != null ) sb.AppendFormat( " WHERE {0}", _TextWhere );
			if( _TextGroupBy != null ) sb.AppendFormat( " GROUP BY {0}", _TextGroupBy );
			if( _TextOrderBy != null ) sb.AppendFormat( " ORDER BY {0}", _TextOrderBy );
			return sb.ToString();
		}

		/// <summary>
		/// Appends to the From clause the contents specified in the given string, in the form "main AS alias" where the
		/// alias part is optional.
		/// </summary>
		/// <param name="spec">The string to append to the From clause.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery From( string spec )
		{
			spec = spec.Validated( "Table" );

			string nick = null;
			string main = KParser.ExtractAlias( spec, out nick );

			if( nick != null ) {
				if( !main.ContainsAny( TypeHelper.InvalidNameChars ) ) _TableAliasList.Add( main, nick );
				spec = string.Format( "{0} AS {1}", main, nick );
			}
			else spec = main;

			// Building the string...
			_TextFrom = _TextFrom == null
				? string.Format( "{0}", spec )
				: string.Format( "{0}, {1}", _TextFrom, spec );

			return this;
		}
		
		/// <summary>
		/// Appends to the From clause the contents obtained from parsing the dynamic lamnda expression given.
		/// <para>They typically use the syntax "x => x.Table.As( alias )", where the alias part is optional.</para>
		/// </summary>
		/// <param name="spec">The dynamic lambda expressions to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery From( params Func<dynamic, object>[] specs )
		{
			if( specs == null ) throw new ArgumentNullException( "specs", "List of specifications cannot be null." );
			foreach( var spec in specs ) {
				// Parse the specification given...
				string str = Parser.Parse( spec, pars: Parameters );

				// Verify if we have to take note of an alias that may exist...
				string nick = null;
				string main = KParser.ExtractAlias( str, out nick );

				if( nick != null ) {
					if( !main.ContainsAny( TypeHelper.InvalidNameChars ) ) _TableAliasList.Add( main, nick );
					str = string.Format( "{0} AS {1}", main, nick );
				}
				else str = main;

				// Building the string...
				_TextFrom = _TextFrom == null
					? string.Format( "{0}", str )
					: string.Format( "{0}, {1}", _TextFrom, str );
			}
			return this;
		}
		
		/// <summary>
		/// Appends to the From clause the contents obtained by parsing the command object given, and associating
		/// it to a given alias.
		/// </summary>
		/// <param name="command">The command object.</param>
		/// <param name="alias">The alias.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery From( IKCommand command, Func<dynamic, object> alias )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command specification cannot be null." );
			if( alias == null ) throw new ArgumentNullException( "alias", "Alias specification cannot be null." );

			string main = Parser.Parse( command, pars: Parameters );
			string nick = Parser.Parse( alias );
			nick = nick.Validated( "Alias", invalidChars: TypeHelper.InvalidNameChars );

			string str = string.Format( "( {0} ) AS {1}", main, nick );
			_TextFrom = _TextFrom == null
				? string.Format( "{0}", str )
				: string.Format( "{0}, {1}", _TextFrom, str );

			return this;
		}

		/// <summary>
		/// Appends a Join clause specifying its type, and using the contents obtained from parsing the dynamic lamda
		/// expression given.
		/// <para>They use the syntax "x => x.Table.As( alias ).On( condition )", where the As() method is optional, and the
		/// On() method is used to specify the conditions of the Join clause.</para>
		/// </summary>
		/// <param name="type">A string containing the type of the Join clause, as "JOIN", "LEFT JOIN", etc.</param>
		/// <param name="spec">The dynamic lambda expression to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery Join( string type, Func<dynamic, object> spec )
		{
			// We can use "LEFT JOIN", but also "LEFT"...
			type = type.Validated( "Join type" );
			if( !type.ToUpper().Contains( "JOIN" ) ) type += " JOIN";

			if( spec == null ) throw new ArgumentNullException( "spec", "Join specification cannot be null." );
			string str = null;

			var item = DynamicParser.Parse( spec ).Result;
			if( item == null ) throw new ArgumentNullException( "Specification resolves to null." );
			if( item is string ) {
				str = string.Format( "{0} {1}", type, (string)item );
			}

			// A dynamic JOIN specification...
			else {
				string table = null;
				string nick = null;
				string on = null;

				// This loop is to intercept the On(...) and As(...) methods in any order: "Join( x => x.Table.As(...).On(...) );"
				while( true ) {

					// The AS specification...
					if( item is DynamicNode.Method && ( (DynamicNode.Method)item ).Name.ToUpper() == "AS" ) {
						if( nick != null ) throw new InvalidOperationException( "Alias is already set." );

						object[] args = ( (DynamicNode.Method)item ).Arguments;
						if( args == null ) throw new ArgumentNullException( "AS()", "AS does not accept an empty parameter." );
						if( args.Length != 1 ) throw new ArgumentException( "AS requires one and only one parameter." );
						nick = Parser.Parse( args[0], rawstr: true );
						nick = nick.Validated( "Alias", invalidChars: TypeHelper.InvalidNameChars );

						item = ( (DynamicNode)item ).Host; // Procesing the rest
						continue;
					}

					//The ON specification...
					if( item is DynamicNode.Method && ( (DynamicNode.Method)item ).Name.ToUpper() == "ON" ) {
						if( on != null ) throw new InvalidOperationException( "On specification is already set." );

						object[] args = ( (DynamicNode.Method)item ).Arguments;
						if( args == null ) throw new ArgumentNullException( "ON", "AS does not accept an empty parameter." );
						if( args.Length != 1 ) throw new ArgumentException( "ON requires one and only one parameter." );
						on = Parser.Parse( args[0], pars: Parameters );

						item = ( (DynamicNode)item ).Host; // Procesing the rest
						continue;
					}

					// And this is the table to join...
					if( item is DynamicNode.GetMember ) {
						table = ( (DynamicNode.GetMember)item ).Name;
						table = table.Validated( "Table", invalidChars: TypeHelper.InvalidNameChars );
						if( on == null ) throw new InvalidOperationException( "On condition is not specified." );

						str = string.Format( "{0} {1}", type, table );
						if( nick != null ) str = string.Format( "{0} AS {1}", str, nick );
						str = string.Format( "{0} ON ({1})", str, on );

						// Taking note of the alias...
						if( nick != null ) {
							if( !table.ContainsAny( TypeHelper.InvalidNameChars ) )
								_TableAliasList.Add( table, nick );
						}

						break;
					}

					throw new ArgumentException( "Unknown item: " + item.ToString() );
				}
			}

			// Building the string...
			_TextJoin = _TextJoin == null
				? string.Format( "{0}", str )
				: string.Format( "{0} {1}", _TextJoin, str );

			return this;
		}

		/// <summary>
		/// Appends a Join clause using the contents obtained from parsing the dynamic lambda expression given.
		/// <para>They use the syntax "x => x.Table.As( alias ).On( condition )", where the As() method is optional, and the
		/// On() method is used to specify the conditions of the Join clause.</para>
		/// </summary>
		/// <param name="spec">The dynamic lambda expression to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery Join( Func<dynamic, object> spec )
		{
			return Join( "JOIN", spec );
		}

		/// <summary>
		/// Appends to the Where clause the contents obtained from parsing the dynamic lambda expression given.
		/// <para>They are typically binary expressions specifying the filter condition of the Where clause.</para>
		/// <para>If any previous contents exists, they are appended with an AND logical operator by default. To append them
		/// with an OR logical operator use the "x => x.Or(...)" syntax.</para>
		/// </summary>
		/// <param name="spec">The dynamic lambda expression to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery Where( Func<dynamic, object> spec )
		{
			if( spec == null ) throw new ArgumentNullException( "spec", "Where specification cannot be null." );
			bool and = true;

			var item = DynamicParser.Parse( spec ).Result;
			if( item == null ) throw new ArgumentNullException( "Specification resolves to null." );

			// The following is to intercept "x => x.And(...)" and "x => x.Or(...)" to concatenate expressions...
			if( item is DynamicNode.Method ) {
				string name = ( (DynamicNode.Method)item ).Name.ToUpper();
				if( name == "AND" || name == "OR" ) {
					object[] args = ( (DynamicNode.Method)item ).Arguments;
					if( args == null ) throw new ArgumentNullException( name, name + " does not accept an empty parameter." );
					if( args.Length != 1 ) throw new ArgumentException( name + " requires one and only one parameter." );

					item = args[0];
					and = name == "OR" ? false : true;
				}
			}
			string str = Parser.Parse( item, pars: Parameters );
			str = str.Validated( "Where" );

			_TextWhere = _TextWhere == null
				? string.Format( "{0}", str )
				: string.Format( "({0} {1} {2})", _TextWhere, and ? "AND" : "OR", str );

			return this;
		}

		/// <summary>
		/// Appends to the Group By clause the contents obtained from parsing the dynamic lambda expressions given.
		/// <para>These expressions typically have the form "x => x.Table.Column", where the table part is optional and can
		/// also be an alias.</para>
		/// <para>They can also use the dynamic scape syntax, using the form "x => x(...)", to include any specific
		/// component needed.</para>
		/// </summary>
		/// <param name="specs">The dynamic lambda expressions to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery GroupBy( params Func<dynamic, object>[] specs )
		{
			if( specs == null ) throw new ArgumentNullException( "specs", "List of specifications cannot be null." );
			foreach( var spec in specs ) {
				if( spec == null ) throw new ArgumentNullException( "spec", "Specification cannot be null." );
				string main = Parser.Parse( spec, pars: Parameters );

				_TextGroupBy = _TextGroupBy == null
				? string.Format( "{0}", main )
				: string.Format( "{0}, {1}", _TextGroupBy, main );
			}
			return this;
		}

		/// <summary>
		/// Appends to the Order By clause the contents obtained from parsing the dynamic lambda expression given, and with
		/// the ordering mode specified.
		/// <para>These expressions typically have the form "x => x.Table.Column", where the table part is optional and can
		/// also be an alias.</para>
		/// </summary>
		/// <param name="column">The dynamic lambda expression to be parsed.</param>
		/// <param name="ascending">Whether the ordering requested will be ascending or descending.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery OrderBy( Func<dynamic, object> column, bool ascending = true )
		{
			if( column == null ) throw new ArgumentNullException( "column", "Column specification cannot be null." );
			string main = Parser.Parse( column );

			string str = string.Format( "{0} {1}", main, ascending ? "ASC" : "DESC" );
			_TextOrderBy = _TextOrderBy == null
				? string.Format( "{0}", str )
				: string.Format( "{0}, {1}", _TextOrderBy, str );

			return this;
		}

		/// <summary>
		/// Appends to the Select clause the contents specified in the given string, in the form "main AS alias" where the
		/// alias part is optional.
		/// </summary>
		/// <param name="spec">The string to append to the Select clause.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery Select( string spec )
		{
			spec = spec.Validated( "Select" );

			string nick = null;
			string main = KParser.ExtractAlias( spec, out nick );

			if( nick != null ) spec = string.Format( "{0} AS {1}", main, nick );
			else spec = main;

			// Building the string...
			_TextSelect = _TextSelect == null
				? string.Format( "{0}", spec )
				: string.Format( "{0}, {1}", _TextSelect, spec );

			return this;
		}

		/// <summary>
		/// Appends to the Select clause the contents obtained from parsing the dynamic lambda expressions given. They
		/// can specify:
		/// <para>- A column using the "x => x.Table.Column" syntax, where tha table part is optional and can also be a
		/// table alias. An optional "As( alias )" method can be appended to specify the alias to assign to it.</para>
		/// <para>- All the columns of a given table using the "x => x.Table.All()" syntax.</para>
		/// <para>- Any other valid expression resolving into contents to be returned by the Select clause.</para>
		/// </summary>
		/// <param name="specs">The dynamic lambda expressions to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery Select( params Func<dynamic, object>[] specs )
		{
			if( specs == null ) throw new ArgumentNullException( "specs", "List of specifications cannot be null." );
			foreach( var spec in specs ) {
				if( spec == null ) throw new ArgumentNullException( "spec", "Specification cannot be null." );

				string str = null;
				var node = DynamicParser.Parse( spec ).Result;
				if( node == null ) throw new ArgumentException( "Specification resolves to null." );

				if( node is string ) str = (string)node;
				else {
					bool all = false;
					string nick = null;

					// Intercepting All() and As()...
					while( true ) {
						if( node is DynamicNode.Method && ( (DynamicNode.Method)node ).Name.ToUpper() == "ALL" ) {
							all = true;

							node = ( (DynamicNode)node ).Host;
							continue;
						}
						if( node is DynamicNode.Method && ( (DynamicNode.Method)node ).Name.ToUpper() == "AS" ) {
							if( nick != null ) throw new ArgumentException( "Alias is already set." );

							object[] args = ( (DynamicNode.Method)node ).Arguments;
							if( args == null ) throw new ArgumentNullException( "AS()", "AS does not accept an empty parameter." );
							if( args.Length != 1 ) throw new ArgumentException( "AS requires one and only one parameter." );

							nick = Parser.Parse( args[0], rawstr: true );
							nick = nick.Validated( "Alias", invalidChars: TypeHelper.InvalidNameChars );

							node = ( (DynamicNode)node ).Host;
							continue;
						}

						str = Parser.Parse( node, pars: Parameters );
						break;
					}

					// If using ALL() we need a valid table specification...
					if( all ) {
						str = str.Validated( "Table", invalidChars: TypeHelper.InvalidNameChars );
						str += ".*";
					}

					if( nick != null ) str = string.Format( "{0} AS {1}", str, nick );
				}

				// Building the string...
				_TextSelect = _TextSelect == null
					? string.Format( "{0}", str )
					: string.Format( "{0}, {1}", _TextSelect, str );
			}
			return this;
		}

		/// <summary>
		/// Appends to the Select clause the contents obtained by parsing the command object given, and associating
		/// it to a given alias.
		/// </summary>
		/// <param name="command">The command object.</param>
		/// <param name="alias">The alias.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery Select( IKCommand command, Func<dynamic, object> alias )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command specification cannot be null." );
			if( alias == null ) throw new ArgumentNullException( "alias", "Alias specification cannot be null." );

			string main = Parser.Parse( command, pars: Parameters );
			string nick = Parser.Parse( alias );
			nick = nick.Validated( "Alias", invalidChars: TypeHelper.InvalidNameChars );

			string str = string.Format( "( {0} ) AS {1}", main, nick );
			_TextSelect = _TextSelect == null
				? string.Format( "{0}", str )
				: string.Format( "{0}, {1}", _TextSelect, str );

			return this;
		}

		/// <summary>
		/// Sets the contents of the Top clause. The previous contents of this clause are discarded.
		/// </summary>
		/// <param name="top">The number of TOP records to return.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandQuery Top( int top )
		{
			if( top <= 0 ) throw new ArgumentException( "Top specification should be greater than cero." );

			_TextTop = string.Format( "{0}", top.ToString() );
			return this;
		}
	}
}
// ========================================================
