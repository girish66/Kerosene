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
	public static class KCommandDeleteHelper
	{
		/// <summary>
		/// Creates a new <see cref="KCommandDelete"/> instance associated with this link object for the table given.
		/// </summary>
		/// <param name="link">The link the command will be associated to.</param>
		/// <param name="table">The table this command will refer to.</param>
		/// <returns>The new created command.</returns>
		public static KCommandDelete Delete( this IKLink link, Func<dynamic, object> table )
		{
			var cmd = new KCommandDelete( link, table );
			return cmd;
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a Delete command.
	/// </summary>
	public class KCommandDelete : KCommand, IKCommand, IKCommandEnumerable, IKCommandExecutable, IKTableNameProvider, IKTableAliasListProvider
	{
		KTableAliasList _TableAliasList = null;
		protected string _TextTable = null;
		protected string _TextWhere = null;

		/// <summary>
		/// Creates a new <see cref="KCommandDelete"/> instance associating it to the given link for the table given.
		/// <para>The dynamic lambda specification for the table should have the form "x => x.Table".</para>
		/// </summary>
		/// <param name="link">The link this command will be associated to.</param>
		/// <param name="table">The table this command refers to.</param>
		public KCommandDelete( IKLink link, Func<dynamic, object> table ) : base( link )
		{
			DEBUG.IndentLine( "\n-- KCommandDelete( Link, Table=?? )" );

			_TableAliasList = new KTableAliasList( Link.DbCaseSensitiveNames );

			if( table == null ) throw new ArgumentNullException( "Table" );
			_TextTable = Parser.Parse( table, rawstr: true );
			_TextTable = _TextTable.Validated( "Table", invalidChars: TypeHelper.InvalidNameChars );
			DEBUG.WriteLine( "\n-- Table: {0}", _TextTable );

			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KCommandDelete.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _TableAliasList != null ) { _TableAliasList.Dispose(); _TableAliasList = null; }
			base.Dispose( disposing );

			DEBUG.Unindent();
		}

		/// <summary>
		/// Clones this command.
		/// </summary>
		/// <returns></returns>
		public KCommandDelete Clone()
		{
			var cloned = new KCommandDelete( this.Link, x => _TextTable ); OnClone( cloned );
			cloned._TableAliasList.AddRange( this._TableAliasList );
			cloned._TextWhere = _TextWhere;
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
		/// Creates a new executor associated with this command.
		/// </summary>
		/// <returns>The new created executor.</returns>
		public IKExecutor GetExecutor()
		{
			return Link.CreateExecutor( this );
		}

		/// <summary>
		/// Gets the list of associations specified for this command.
		/// </summary>
		public KTableAliasList TableAliasList
		{
			get { return _TableAliasList; }
		}

		/// <summary>
		/// Gets the table this command is associated with.
		/// </summary>
		public string TableName
		{
			get { return _TextTable; }
		}

		// --------------------------------------------------
		/// <summary>
		/// Returns the command's text as it has been specified by its methods.
		/// </summary>
		/// <param name="iterable">Whether to generate the iterable version of the command or the executable one.</param>
		/// <returns>Returns the command's text as it has been specified.</returns>
		public override string CommandText( bool iterable )
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat( "DELETE FROM {0}", _TextTable );
			if( iterable ) sb.Append( " OUTPUT DELETED.*" );
			if( _TextWhere != null ) sb.AppendFormat( " WHERE {0}", _TextWhere );
			return sb.ToString();
		}

		/// <summary>
		/// Appends to the Where clause the contents obtained from parsing the dynamic lambda expression given.
		/// <para>They are typically binary expressions specifying the filter condition of the Where clause.</para>
		/// <para>If any previous contents exists, they are appended with an AND logical operator by default. To append them
		/// with an OR logical operator use the "x => x.Or(...)" syntax.</para>
		/// </summary>
		/// <param name="spec">The dynamic lambda expression to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandDelete Where( Func<dynamic, object> spec )
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
	}
}
// ========================================================
