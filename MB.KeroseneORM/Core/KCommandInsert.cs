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
	public static class KCommandInsertHelper
	{
		/// <summary>
		/// Creates a new <see cref="KCommandInsert"/> instance associated with this link object for the table given.
		/// </summary>
		/// <param name="link">The link the command will be associated to.</param>
		/// <param name="table">The table this command will refer to.</param>
		/// <returns>The new created command.</returns>
		public static KCommandInsert Insert( this IKLink link, Func<dynamic, object> table )
		{
			var cmd = new KCommandInsert( link, table );
			return cmd;
		}
	}

	// =====================================================
	/// <summary>
	/// Represents an Insert command.
	/// </summary>
	public class KCommandInsert : KCommand, IKCommand, IKCommandEnumerable, IKCommandExecutable, IKTableNameProvider, IKTableAliasListProvider
	{
		KTableAliasList _TableAliasList = null;
		protected string _TextTable = null;
		protected string _TextColumn = null;
		protected string _TextValues = null;

		/// <summary>
		/// Creates a new <see cref="KCommandInsert"/> instance associating it to the given link for the table given.
		/// <para>The dynamic lambda specification for the table should have the form "x => x.Table".</para>
		/// </summary>
		/// <param name="link">The link this command will be associated to.</param>
		/// <param name="table">The table this command refers to.</param>
		public KCommandInsert( IKLink link, Func<dynamic, object> table ) : base( link )
		{
			DEBUG.IndentLine( "\n-- KCommandInsert( Link, Table=?? )" );

			_TableAliasList = new KTableAliasList( Link.DbCaseSensitiveNames );

			if( table == null ) throw new ArgumentNullException( "Table" );
			_TextTable = Parser.Parse( table, rawstr: true );
			_TextTable = _TextTable.Validated( "Table", invalidChars: TypeHelper.InvalidNameChars );
			DEBUG.WriteLine( "   -- Table: {0}", _TextTable );

			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KCommandInsert.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _TableAliasList != null ) { _TableAliasList.Dispose(); _TableAliasList = null; }
			base.Dispose( disposing );

			DEBUG.Unindent();
		}

		/// <summary>
		/// Clones this command.
		/// </summary>
		/// <returns></returns>
		public KCommandInsert Clone()
		{
			var cloned = new KCommandInsert( this.Link, x => _TextTable ); OnClone( cloned );
			cloned._TableAliasList.AddRange( this._TableAliasList );
			cloned._TextColumn = _TextColumn;
			cloned._TextValues = _TextValues;
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
			sb.AppendFormat( "INSERT INTO {0}", _TextTable );
			if( _TextColumn != null ) sb.AppendFormat( " ( {0} )", _TextColumn );
			if( iterable ) sb.Append( " OUTPUT INSERTED.*" );
			if( _TextValues != null ) sb.AppendFormat( " VALUES ( {0} )", _TextValues );
			return sb.ToString();
		}

		/// <summary>
		/// Permits the specification of the columns of this command.
		/// <para>They are dynamic lambda expressions in the form of assignations as in "x => x.Table.Column = value", where
		/// the table part is optional and can be an alias, and the value part can be any valid SQL statement.</para>
		/// </summary>
		/// <param name="specs">The list of column specifications for this command,</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KCommandInsert Column( params Func<dynamic, object>[] specs )
		{
			if( specs == null ) throw new ArgumentNullException( "specs", "List of column asignations cannot be null." );
			foreach( var spec in specs ) {
				if( spec == null ) throw new ArgumentNullException( "spec", "Column asignation cannot be null." );

				object obj = DynamicParser.Parse( spec ).Result;
				if( obj == null ) throw new ArgumentException( "Assign expression resolves to null." );

				DynamicNode.SetMember node = obj as DynamicNode.SetMember;
				if( node == null ) throw new ArgumentException( "Expression is not an assign expression: " + obj.ToString() );

				string main = KParser.ExtractTag( string.Format( "{0}.{1}", node.Host, node.Name ) );
				string value = Parser.Parse( node.Value, Parameters, nulls: true );

				// Building the columns' string...
				_TextColumn = _TextColumn == null
					? main
					: string.Format( "{0}, {1}", _TextColumn, main );

				// No need to adjust "value" for strings or other formats, as it should be parametrized...
				_TextValues = _TextValues == null
					? value
					: string.Format( "{0}, {1}", _TextValues, value );
			}
			return this;
		}
	}
}
// ========================================================
