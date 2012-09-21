// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Text;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::System.Runtime.Serialization;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents an association between a table name and its alias.
	/// </summary>
	[Serializable]
	public class KTableAlias : IDisposable, ICloneable, ISerializable
	{
		internal KTableAliasList _Owner = null;
		string _Table = null;
		string _Alias = null;

		/// <summary>
		/// Creates a new instance of <see cref="KTableAlias"/> to associate the given table with the given alias.
		/// </summary>
		/// <param name="table">The table name.</param>
		/// <param name="alias">The alias name.</param>
		public KTableAlias( string table, string alias )
		{
			DEBUG.IndentLine( "\n-- KTableAlias( Table={0}, Alias={1} )", table ?? "<null>", alias ?? "<null>" );
			Table = table;
			Alias = alias;
			DEBUG.Unindent();
		}

		/// <summary>
		/// Creates a new instance of <see cref="KTableAlias"/> to associate the given alias with the anonymous table.
		/// </summary>
		/// <param name="alias">The alias name.</param>
		public KTableAlias( string alias )
		{
			DEBUG.IndentLine( "\n-- KTableAlias( Alias={0} )", alias ?? "<null>" );
			Alias = alias;
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KTableAlias.Dispose( Disposing={0} ) - This={1}", disposing, this );
			if( _Owner != null ) {
				if( _Owner._List != null ) { _Owner._List.Remove( this ); }
				_Owner = null;
			}
			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KTableAlias.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KTableAlias()
		{
			DEBUG.IndentLine( "\n-- KTableAlias~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			string s = _Table == null
				? string.Format( ". => {0}", _Alias ?? "<null>" )
				: string.Format( "{0} => {1}", _Table, _Alias ?? "<null>" );
			return s;
		}

		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( "Table", _Table );
			info.AddValue( "Alias", _Alias );
		}
		protected KTableAlias( SerializationInfo info, StreamingContext context )
		{
			_Table = info.GetString( "Table" );
			_Alias = info.GetString( "Alias" );
		}

		/// <summary>
		/// Clones this instance returning a new orphan one (not associated with any collection).
		/// </summary>
		/// <returns>The newly created clone.</returns>
		public KTableAlias Clone()
		{
			var cloned = new KTableAlias( _Table, _Alias );
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Gets the table name, or null if this association is for the anonymous table.
		/// </summary>
		public string Table
		{
			get { return _Table; }
			private set { _Table = value.Validated( "Table Name", invalidChars: TypeHelper.InvalidNameChars ); }
		}

		/// <summary>
		/// Gets the alias name.
		/// </summary>
		public string Alias
		{
			get { return _Alias; }
			private set { _Alias = value.Validated( "Alias", invalidChars: TypeHelper.InvalidNameChars ); }
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a collection of associations among table names and their respective aliases.
	/// </summary>
	[Serializable]
	public class KTableAliasList : IDisposable, ICloneable, ISerializable, IEnumerable<KTableAlias>
	{
		internal List<KTableAlias> _List = new List<KTableAlias>();
		bool _CaseSensitiveNames = true;

		/// <summary>
		/// Creates a new <see cref="KTableAliasList"/> instance.
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the names of table and aliases shall be treated case sensitively or
		/// not.</param>
		public KTableAliasList( bool caseSensitiveNames )
		{
			DEBUG.IndentLine( "\n-- KTableAliasList( CaseSensitive={0} )", caseSensitiveNames );
			_CaseSensitiveNames = caseSensitiveNames;
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KTableAliasList.Dispose( Disposing={0} ) - This={1}", disposing, this );
			if( _List != null ) {
				foreach( var child in _List ) { child._Owner = null; child.Dispose(); }
				_List.Clear(); _List = null;
			}
			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KTableAliasList.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KTableAliasList()
		{
			DEBUG.IndentLine( "\n-- KTableAliasList~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			if( _List == null ) return "<KTableAliasList>";

			StringBuilder sb = new StringBuilder();
			sb.Append( "{" ); bool first = true; foreach( var child in _List ) {
				if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
				sb.Append( child.ToString() );
			}
			if( !first ) sb.Append( " " ); else sb.Append( "-" );
			sb.Append( "}" );
			return sb.ToString();
		}

		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( "CaseSensitive", _CaseSensitiveNames );

			info.AddValue( "ChildCount", _List.Count ); for( int i = 0; i < _List.Count; i++ ) {
				info.AddValue( "Child" + i, _List[i] );
			}
		}
		protected KTableAliasList( SerializationInfo info, StreamingContext context )
		{
			_CaseSensitiveNames = info.GetBoolean( "CaseSensitive" );

			int count = (int)info.GetValue( "ChildCount", typeof( int ) ); for( int i = 0; i < count; i++ ) {
				KTableAlias child = (KTableAlias)info.GetValue( "Child" + i, typeof( KTableAlias ) );
				child._Owner = this; _List.Add( child );
			}
		}

		public IEnumerator<KTableAlias> GetEnumerator()
		{
			return _List.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Gets the number of associations in this collection, or -1 if it has been disposed.
		/// </summary>
		public int Count
		{
			get { return _List == null ? -1 : _List.Count; }
		}

		/// <summary>
		/// Creates a new clone of this collection by also cloning all its original associations.
		/// </summary>
		/// <returns>A new clone of this instance.</returns>
		public KTableAliasList Clone()
		{
			var cloned = new KTableAliasList( _CaseSensitiveNames );
			foreach( var item in _List ) { cloned._List.Add( item.Clone() ); item._Owner = cloned; }
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Gets whether the names of tables and aliases are treated case sensitively or not.
		/// </summary>
		public bool CaseSensitiveNames
		{
			get { return _CaseSensitiveNames; }
		}

		/// <summary>
		/// Returns a list with all the associations registered for the given table name, or null if no associations exists.
		/// </summary>
		/// <param name="table">The table name. Null is acceptable to retrieve anonymous table associations.</param>
		/// <param name="raise">True to raise an exception if no associations are found.</param>
		/// <returns>A list with all the associations for the given table name, or null if no associations exists.</returns>
		public List<KTableAlias> FindByTableName( string table, bool raise = false )
		{
			if( table != null ) table = table.Validated( "Table Name", invalidChars: TypeHelper.InvalidNameChars );
			List<KTableAlias> list = new List<KTableAlias>();

			foreach( var child in _List ) {
				if( child.Table == null ) { if( table == null ) list.Add( child ); }
				else {
					if( table == null ) continue;
					if( string.Compare( table, child.Table, !_CaseSensitiveNames ) == 0 ) list.Add( child );
				}
			}

			if( list.Count == 0 && raise ) throw new KeyNotFoundException( "Table not found: " + ( table ?? "<null>" ) );
			return list.Count == 0 ? null : list;
		}

		/// <summary>
		/// Gets the association where the alias given is used, or null if such association does not exist.
		/// </summary>
		/// <param name="alias">The alias name.</param>
		/// <param name="raise">True to raise an exception if no associations are found.</param>
		/// <returns>The associations where the given alias is used, or null if no such association exists.</returns>
		public KTableAlias FindByAlias( string alias, bool raise = false )
		{
			alias = alias.Validated( "Alias", invalidChars: TypeHelper.InvalidNameChars );
			KTableAlias assoc = null;

			foreach( var child in _List ) {
				if( string.Compare( child.Alias, alias, !_CaseSensitiveNames ) == 0 ) { assoc = child; break; }
			}

			if( assoc == null && raise ) throw new KeyNotFoundException( "Alias not found: " + alias );
			return assoc;
		}

		/// <summary>
		/// Gets the association stored at the given index. Throws an exception if the index is invalid.
		/// </summary>
		/// <param name="index">The index to find the association at.</param>
		/// <returns>The association stored at the given index.</returns>
		public KTableAlias this[int index]
		{
			get { return _List[index]; }
		}

		/// <summary>
		/// Adds the given association to this list.
		/// <para>Despite that a table name (or null for anonymous associations) can be used as many times as needed, the
		/// aliases must be unique in this collection.</para>
		/// </summary>
		/// <param name="assoc">The association to add.</param>
		public void Add( KTableAlias assoc )
		{
			if( assoc == null ) throw new ArgumentNullException( "assoc", "Association cannot be null." );
			if( assoc._Owner != null ) throw new InvalidOperationException( "Association alredy belongs to another collection:" + assoc );

			var child = FindByAlias( assoc.Alias, raise: false );
			if( child != null ) throw new InvalidOperationException( "The alias name is already used: " + assoc );

			_List.Add( assoc ); assoc._Owner = this;
		}

		/// <summary>
		/// Adds a range of associations into this collection.
		/// <para>If any of the associations in the range belong to another collection, a clone is obtained and added instead.</para>
		/// </summary>
		/// <param name="list">The range of associations to add.</param>
		public void AddRange( IEnumerable<KTableAlias> list )
		{
			if( list == null ) throw new ArgumentNullException( "list", "List cannot be null." );
			foreach( var child in list ) Add( child._Owner == null ? child : child.Clone() );
		}

		/// <summary>
		/// Creates and adds into this collection an association for the table and alias given.
		/// </summary>
		/// <param name="table">The table name, or null to create an anonymous association.</param>
		/// <param name="alias">The alias.</param>
		/// <returns>The newly created and added association.</returns>
		public KTableAlias Add( string table, string alias )
		{
			KTableAlias child = new KTableAlias( table, alias );
			Add( child );
			return child;
		}

		/// <summary>
		/// Creates and adds into this collection an anonymous association for the alias given.
		/// </summary>
		/// <param name="alias">The alias.</param>
		/// <returns>The newly created and added association.</returns>
		public KTableAlias Add( string alias )
		{
			KTableAlias child = new KTableAlias( alias );
			Add( child );
			return child;
		}

		/// <summary>
		/// Removes the given association from this collection, making it an orphan association.
		/// </summary>
		/// <param name="item">The association to remove from this collection.</param>
		/// <returns>True if the association has been removed, false otherwise.</returns>
		public bool Remove( KTableAlias item )
		{
			if( item == null ) throw new ArgumentNullException( "item", "TableAlias cannot be null." );
			if( item._Owner != this ) throw new InvalidOperationException( "TableAlias does not belong to this collection: " + item );

			bool r = _List.Remove( item ); if( r ) item._Owner = null;
			return r;
		}

		/// <summary>
		/// Clears this collection by removing all the associations registered into it, and optionally disposing them.
		/// </summary>
		/// <param name="disposeItems">To dispose the removed associations or not.</param>
		public void Clear( bool disposeItems = true )
		{
			if( disposeItems ) foreach( var child in _List ) { child._Owner = null; child.Dispose(); }
			_List.Clear();
		}

		/// <summary>
		/// Gets a list with the tables used in this collection.
		/// <para>This list can include a null element if the collection contains anonymous associations.</para>
		/// <para>Despite how many times a given table can appear (as they can be associated with many aliases), this list
		/// will contain just one ocurrence per table.</para>
		/// </summary>
		public List<string> Tables
		{
			get
			{
				bool added = false; List<string> list = new List<string>(); foreach( var child in _List ) {
					if( child.Table == null ) {
						if( !added ) { list.Add( null ); added = true; }
						continue;
					}
					if( list.Find( x => string.Compare( x, child.Table, !_CaseSensitiveNames ) == 0 ) != null ) continue;
					list.Add( child.Table );
				}
				return list;
			}
		}

		/// <summary>
		/// Gets a list with the aliases used in this collection.
		/// </summary>
		public List<string> Aliases
		{
			get
			{
				List<string> list = new List<string>(); foreach( var child in _List ) list.Add( child.Alias );
				return list;
			}
		}
	}
}
// ========================================================
