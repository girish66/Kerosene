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
	/// Represents the metadata associated with a given column in a table in the database.
	/// </summary>
	[Serializable]
	public class KMetaColumn : IDisposable, ICloneable, ISerializable
	{
		internal KSchema _Owner = null;
		Dictionary<string, object> _Metadata = new Dictionary<string, object>();
		const string _IdFullName = "FullName";
		const string _IdColumnName = "ColumnName";
		const string _IdBaseTableName = "BaseTableName";
		const string _IdIsKey = "IsKey";
		const string _IdIsUnique = "IsUnique";
		const string _IdIsReadOnly = "IsReadOnly";

		/// <summary>
		/// Creates a new <see cref="KMetaColumn"/> intialized with the table and column names given.
		/// <para>Null table names are acceptable to create an anonymous column, one not associated with any table.</para>
		/// </summary>
		/// <param name="tableName">The table name, or null.</param>
		/// <param name="columnName">The column name.</param>
		public KMetaColumn( string tableName, string columnName )
		{
			DEBUG.IndentLine( "\n-- KMetaColumn( TableName={0}, ColumnName={1} )", tableName ?? "null", columnName ?? "null" );
			if( tableName == null ) _Metadata[_IdBaseTableName] = null; else BaseTableName = tableName;
			ColumnName = columnName;
			DEBUG.Unindent();
		}

		/// <summary>
		/// Creates an empty <see cref="KMetaColumn"/> instance, where no metadata properties have been set.
		/// <para>Note that an empty meta column is not usable unless at least its ColumnName property is set, and potentially
		/// its BaseTableName property as well.</para>
		/// </summary>
		public KMetaColumn()
		{
			DEBUG.IndentLine( "\n-- KMetaColumn()" );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KMetaColumn.Dispose( Disposing={0} ) - This={1}", disposing, this );
			if( _Owner != null ) {
				if( _Owner._List != null ) _Owner._List.Remove( this );
				_Owner = null;
			}
			if( _Metadata != null ) { _Metadata.Clear(); _Metadata = null; }
			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KMetaColumn.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KMetaColumn()
		{
			DEBUG.IndentLine( "\n-- KMetaColumn~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public string ToString( bool extended )
		{
			if( _Metadata == null ) return "<KMetaColumn>";

			StringBuilder sb = new StringBuilder( FullName );
			if( extended ) {
				foreach( var kvp in _Metadata ) {
					if( kvp.Key == _IdBaseTableName ) continue;
					if( kvp.Key == _IdColumnName ) continue;
					sb.AppendFormat( ", {0}={1}", kvp.Key, TypeHelper.ToString( kvp.Value ) );
				}
			}
			return sb.ToString();
		}
		public override string ToString()
		{
			return ToString( extended: false );
		}

		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			int i = -1; foreach( var kvp in _Metadata ) {
				string name = kvp.Key;
				object value = kvp.Value;
				Type type = value == null ? null : value.GetType();

				// We only serialize values that use well-known types to avoid exceptions...
				bool serialize = false;
				if( type == null ) serialize = true;
				else if( type == typeof( Int16 ) ) serialize = true;
				else if( type == typeof( Int32 ) ) serialize = true;
				else if( type == typeof( Int64 ) ) serialize = true;
				else if( type == typeof( String ) ) serialize = true;
				else if( type == typeof( Boolean ) ) serialize = true;
				else if( type == typeof( DateTime ) ) serialize = true;
				if( !serialize ) continue;
				i++;

				info.AddValue( "MetaName" + i, name );
				info.AddValue( "MetaType" + i, type == null ? "VOID" : type.AssemblyQualifiedName );
				if( value != null ) info.AddValue( "MetaValue" + i, value );
			}
			info.AddValue( "MetaCount", i + 1 );
		}
		protected KMetaColumn( SerializationInfo info, StreamingContext context )
		{
			int count = (int)info.GetValue( "MetaCount", typeof( int ) );
			for( int i = 0; i < count; i++ ) {
				string name = info.GetString( "MetaName" + i );
				string type = info.GetString( "MetaType" + i );
				object value = type == "VOID" ? null : info.GetValue( "MetaValue" + i, Type.GetType( type ) );

				_Metadata.Add( name, value );
			}
		}

		/// <summary>
		/// Creates a new orphan clone not associated with any schema.
		/// <para>It tries also to clone the values contained in its metadata properties.</para>
		/// </summary>
		/// <returns>A new clone of this instance.</returns>
		public KMetaColumn Clone()
		{
			var cloned = new KMetaColumn();
			foreach( var kvp in _Metadata ) cloned._Metadata.Add( kvp.Key, TypeHelper.TryClone( kvp.Value ) );
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Gets whether this metacolumn is sealed or not.
		/// <para>A sealed metacolumn does not accept any changes in the metadata properties it carries.</para>
		/// <para>Its value is obtained from the Sealed property of the schema this metacolumn is registered with, or false
		/// if it is not registered with any schema.</para>
		/// </summary>
		public bool Sealed
		{
			get { return _Owner == null ? false : _Owner.Sealed; }
		}

		/// <summary>
		/// Gets the full name of this metacolumn. It has the form ".ColumnName" if this metacolumn is an anonymous one, or
		/// the form "TableName.ColumnName" otherwise.
		/// </summary>
		public string FullName
		{
			get
			{
				if( BaseTableName == null ) return string.Format( ".{0}", ColumnName ?? "<null>" );
				else return string.Format( "{0}.{1}", BaseTableName, ColumnName ?? "<null>" );
			}
		}

		/// <summary>
		/// Gets the base table name this metacolumn is created for. It can be null if this metacolumn is an anonymous one.
		/// <para>The setter can be used only once (but it might have been used already by a non-empty constructor).</para>
		/// </summary>
		public string BaseTableName
		{
			get { return !_Metadata.ContainsKey( _IdBaseTableName ) ? null : (string)_Metadata[_IdBaseTableName]; }
			set
			{
				if( _Metadata.ContainsKey( _IdBaseTableName ) ) throw new InvalidOperationException( "BaseTableName is already set." );
				if( Sealed ) throw new InvalidOperationException( "Schema is sealed." );
				_Metadata[_IdBaseTableName] = value.Validated( "Table", invalidChars: TypeHelper.InvalidNameChars );
			}
		}

		/// <summary>
		/// Gets the column name this metacolumn is created for.
		/// <para>The setter can be used only once (but it might have been used already by a non-empty constructor).</para>
		/// </summary>
		public string ColumnName
		{
			get { return !_Metadata.ContainsKey( _IdColumnName ) ? null : (string)_Metadata[_IdColumnName]; }
			set
			{
				if( _Metadata.ContainsKey( _IdColumnName ) ) throw new InvalidOperationException( "ColumnName is already set." );
				if( Sealed ) throw new InvalidOperationException( "Schema is sealed." );
				_Metadata[_IdColumnName] = value.Validated( "Column", invalidChars: TypeHelper.InvalidNameChars );
			}
		}

		/// <summary>
		/// Gets or sets whether this meta column refers to a primary key database column.
		/// <para>If it has not been set previously, a default value of <see cref="false"/> is returned.</para>
		/// </summary>
		public bool IsKey
		{
			get { return !_Metadata.ContainsKey( _IdIsKey ) ? false : (bool)_Metadata[_IdIsKey]; }
			set
			{
				if( Sealed ) throw new InvalidOperationException( "Schema is sealed." );
				_Metadata[_IdIsKey] = value;
			}
		}

		/// <summary>
		/// Gets or sets whether this meta column refers to a unique value database column.
		/// <para>If it has not been set previously, a default value of <see cref="false"/> is returned.</para>
		/// </summary>
		public bool IsUnique
		{
			get { return !_Metadata.ContainsKey( _IdIsUnique ) ? false : (bool)_Metadata[_IdIsUnique]; }
			set
			{
				if( Sealed ) throw new InvalidOperationException( "Schema is sealed." );
				_Metadata[_IdIsUnique] = value;
			}
		}

		/// <summary>
		/// Gets or sets whether this meta column refers to a read only database column.
		/// <para>If it has not been set previously, a default value of <see cref="false"/> is returned.</para>
		/// </summary>
		public bool IsReadOnly
		{
			get { return !_Metadata.ContainsKey( _IdIsReadOnly ) ? false : (bool)_Metadata[_IdIsReadOnly]; }
			set
			{
				if( Sealed ) throw new InvalidOperationException( "Schema is sealed." );
				_Metadata[_IdIsReadOnly] = value;
			}
		}

		/// <summary>
		/// Gets or sets the value associated with the metadata property whose name is given.
		/// <para>If the metadata name refers to a default property, and this one has not been previously set, its default
		/// value is returned by the getter.</para>
		/// <para>Otherwise, if it does not exist, the getter will throw an exception.</para>
		/// </summary>
		/// <param name="property">The name of the metadata property.</param>
		/// <returns>The getter returns the value associated with the metadata property whose name is given.</returns>
		public object this[string property]
		{
			get
			{
				// Intercepting the default values of the standard properties...
				switch( property ) {
					case _IdFullName: return this.FullName;
					case _IdBaseTableName: return this.BaseTableName;
					case _IdColumnName: return this.ColumnName;
					case _IdIsKey: return this.IsKey;
					case _IdIsUnique: return this.IsUnique;
					case _IdIsReadOnly: return this.IsReadOnly;

					default: return _Metadata[property];
				}
			}
			set
			{
				if( Sealed ) throw new InvalidOperationException( "Schema is sealed." );
				switch( property ) {
					case _IdFullName: throw new InvalidOperationException( _IdFullName + " is read only." );
					case _IdBaseTableName: this.BaseTableName = (string)value; return;
					case _IdColumnName: this.ColumnName = (string)value; return;
					case _IdIsKey: this.IsKey = (bool)value; return;
					case _IdIsUnique: this.IsUnique = (bool)value; return;
					case _IdIsReadOnly: this.IsReadOnly = (bool)value; return;

					default: _Metadata[property] = value; break;
				}
			}
		}

		/// <summary>
		/// Returns whether this instance contains metadata for the property whose name (key) is given.
		/// </summary>
		/// <param name="key">The name of the metadata property to check if it exists or not.</param>
		/// <returns>True if this instance contains the metadata property whose name is given, false otherwise.</returns>
		public bool ContainsMetaKey( string key )
		{
			if( key == null ) throw new ArgumentNullException( "key", "MetaKey cannot be null." );
			return _Metadata.ContainsKey( key );
		}

		/// <summary>
		/// Gets a list containing the names of the metadata properties this instance has.
		/// <para>The names of the default properties are automatically included into this list even if they have not been
		/// set previously.</para>
		/// </summary>
		public List<string> MetadataNames
		{
			get
			{
				List<string> list = new List<string>();
				list.Add( _IdFullName );
				list.Add( _IdBaseTableName );
				list.Add( _IdColumnName );
				list.Add( _IdIsKey );
				list.Add( _IdIsUnique );
				list.Add( _IdIsReadOnly );

				foreach( var kvp in _Metadata ) {
					if( kvp.Key == _IdFullName ) continue;
					if( kvp.Key == _IdBaseTableName ) continue;
					if( kvp.Key == _IdColumnName ) continue;
					if( kvp.Key == _IdIsKey ) continue;
					if( kvp.Key == _IdIsUnique ) continue;
					if( kvp.Key == _IdIsReadOnly ) continue;

					list.Add( kvp.Key );
				}
				return list;
			}
		}
	}

	// =====================================================
	/// <summary>
	/// Represents the schema of a record, composed by a collection of <see cref="KMetaColumn"/> instances.
	/// </summary>
	[Serializable]
	public class KSchema : IDisposable, ICloneable, ISerializable, IEnumerable<KMetaColumn>
	{
		internal List<KMetaColumn> _List = new List<KMetaColumn>();
		bool _Sealed = false;
		bool _CaseSensitiveNames = false;
		KTableAliasList _TableAliasList = null;

		/// <summary>
		/// Creates a new <see cref="KSchema"/> instance.
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the names of the tables and columns are considered case sensitive
		/// or not.</param>
		public KSchema( bool caseSensitiveNames )
		{
			DEBUG.IndentLine( "\n-- KSchema( CaseSensitiveNames={0} )", caseSensitiveNames );
			_CaseSensitiveNames = caseSensitiveNames;
			_TableAliasList = new KTableAliasList( caseSensitiveNames );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KSchema.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _List != null ) {
				foreach( var item in _List ) { item._Owner = null; item.Dispose(); }
				_List.Clear(); _List = null;
			}
			_TableAliasList = null; // It might be in use elsewhere...

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KSchema.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KSchema()
		{
			DEBUG.IndentLine( "\n-- KSchema~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public string ToString( bool extended )
		{
			if( _List == null ) return "<KSchema>";

			StringBuilder sb = new StringBuilder();
			sb.Append( "{" ); bool first = true; foreach( var col in _List ) {
				if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
				if( extended ) sb.Append( "[ " ); sb.Append( col.ToString( extended ) );
				if( extended ) sb.Append( " ]" );
			}
			if( !first ) sb.Append( " " ); else sb.Append( "-" );
			sb.Append( "}" );
			return sb.ToString();
		}
		public override string ToString()
		{
			return ToString( extended: false );
		}

		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( "Sealed", _Sealed );
			info.AddValue( "CaseSensitiveNames", _CaseSensitiveNames );

			int count = _List.Count; info.AddValue( "MemberCount", count );
			for( int i = 0; i < count; i++ ) {
				info.AddValue( "Member" + i, _List[i] );
			}

			info.AddValue( "TableAliasList", _TableAliasList );
		}
		protected KSchema( SerializationInfo info, StreamingContext context )
		{
			_Sealed = info.GetBoolean( "Sealed" );
			_CaseSensitiveNames = info.GetBoolean( "CaseSensitiveNames" );

			int count = (int)info.GetValue( "MemberCount", typeof( int ) );
			for( int i = 0; i < count; i++ ) {
				KMetaColumn item = (KMetaColumn)info.GetValue( "Member" + i, typeof( KMetaColumn ) );
				item._Owner = this;
				_List.Add( item );
			}

			_TableAliasList = (KTableAliasList)info.GetValue( "TableAliasList", typeof( KTableAliasList ) );
		}

		/// <summary>
		/// Creates a new clone of this schema, cloning also all its meta columns.
		/// </summary>
		/// <returns>A new cloned schema.</returns>
		public KSchema Clone()
		{
			KSchema cloned = new KSchema( _CaseSensitiveNames );
			foreach( var item in _List ) { cloned._List.Add( item.Clone() ); item._Owner = cloned; }
			cloned._Sealed = _Sealed;
			cloned._TableAliasList = _TableAliasList.Clone();
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public IEnumerator<KMetaColumn> GetEnumerator()
		{
			return _List.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Gets the number of meta columns in this schema, or -1 if it has been disposed.
		/// </summary>
		public int Count
		{
			get { return _List == null ? -1 : _List.Count; }
		}

		/// <summary>
		/// Gets whether the names of tables and columns in this schema are treated case sensitively or not.
		/// </summary>
		public bool CaseSensitiveNames
		{
			get { return _CaseSensitiveNames; }
		}

		/// <summary>
		/// Gets or sets whether this schema is sealed.
		/// <para>A sealed schema does not accept any changes in its structure or in the metadata properties of its
		/// metacolumns.</para>
		/// </summary>
		public bool Sealed
		{
			get { return _Sealed; }
			set { _Sealed = value; }
		}

		/// <summary>
		/// Gets the <see cref="KTableAliasList"/> instance this schema carries.
		/// </summary>
		public KTableAliasList TableAliasList
		{
			get { return _TableAliasList; }
		}

		/// <summary>
		/// Finds the <see cref="KMetaColumn"/> instance whose table and column names are given.
		/// <para>The table name can be an alias, which is converted to the actual table name to perform the search.</para>
		/// <para>The table name (converted or not) can be null to find an anonymous <see cref="KMetaColumn"/>.</para>
		/// <para>The anyTable argument can be set to true when it is acceptable to find a column regardless what table
		/// may it belong to. In this case, if several metacolumns are found, an exception is raised.</para>
		/// </summary>
		/// <param name="tableName">The table or alias name of the metacolumn to find. It can be null when we want to find
		/// an anonymous column.</param>
		/// <param name="columnName">The column name.</param>
		/// <param name="anyTable">True when it is acceptable to find a column regardless what table may it belong to.</param>
		/// <param name="raise">True to raise an exception if the metacolumn is not found.</param>
		/// <returns>The metacolumn, or null if it is not found.</returns>
		public KMetaColumn Find( string tableName, string columnName, bool anyTable = true, bool raise = false )
		{
			if( tableName != null ) tableName.Validated( "Table or Alias name", invalidChars: TypeHelper.InvalidNameChars );
			columnName = columnName.Validated( "Column name", invalidChars: TypeHelper.InvalidNameChars );

			// The association may force the tableName to become null if it was not already...
			KTableAlias assoc = tableName == null ? null : TableAliasList.FindByAlias( tableName, raise: false );
			string table = assoc == null ? tableName : assoc.Table;
			KMetaColumn child = null;

			// Strict search...
			string finder = string.Format( "{0}.{1}", table, columnName );
			child = _List.Find( x => string.Compare( x.FullName, finder, !_CaseSensitiveNames ) == 0 );
			if( child != null ) return child;

			// Treating the anyTable case...
			if( anyTable ) {
				var list = _List.FindAll( x => string.Compare( x.ColumnName, columnName, !_CaseSensitiveNames ) == 0 );
				if( list.Count == 1 ) return list[0];
				if( list.Count > 1 ) throw new KeyNotFoundException( "Several matches found for column name: " + columnName );
			}

			if( child == null && raise ) throw new KeyNotFoundException( "Metacolumn not found: " + string.Format( "{0}.{1}", tableName, columnName ) );
			return child;
		}

		/// <summary>
		/// Gets the <see cref="KMetaColumn"/> stored at the given index. An expection is thrown if the index is invalid.
		/// </summary>
		/// <param name="ix">The index where to find the <see cref="KMetaColumn"/>.</param>
		/// <returns>The <see cref="KMetaColumn"/> stored at the given index.</returns>
		public KMetaColumn this[int ix]
		{
			get { return _List[ix]; }
		}

		/// <summary>
		/// Adds into this collection the association given.
		/// </summary>
		/// <param name="item">The association to add into this collection.</param>
		public void Add( KMetaColumn item )
		{
			if( _Sealed ) throw new InvalidOperationException( "This schema is sealed." );
			if( item == null ) throw new ArgumentNullException( "item", "Item  cannot be null." );
			if( item._Owner != null ) throw new InvalidOperationException( "Item is registered into another schema: " + item );

			item.ColumnName.Validated( "Column Name" ); // To intercept empty instances

			// Duplicate columns are acceptable only if all are fully qualified...
			var list = _List.FindAll( x => string.Compare( x.ColumnName, item.ColumnName, !_CaseSensitiveNames ) == 0 );
			if( list.Count != 0 ) {
				if( item.BaseTableName == null ) throw new ArgumentException( "Duplicate column name not accepted: " + item.ColumnName );
				if( list[0].BaseTableName == null ) throw new ArgumentException( "Duplicate column name already exists: " + list[0] );
			}
			_List.Add( item ); item._Owner = this;
		}

		/// <summary>
		/// Adds a range of meta columns in this schema.
		/// If any of those belong to another schema, a clone is obtained and added instead.
		/// </summary>
		/// <param name="list">The range of meta columns to add.</param>
		public void AddRange( IEnumerable<KMetaColumn> list )
		{
			if( list == null ) throw new ArgumentNullException( "list", "List cannot be null." );
			foreach( var child in list ) Add( child._Owner == null ? child : child.Clone() );
		}

		/// <summary>
		/// Removes the given meta column from this schema.
		/// </summary>
		/// <param name="child">The meta column to remove.</param>
		/// <returns>True if the meta column has been removed, false otherwise.</returns>
		public bool Remove( KMetaColumn child )
		{
			if( _Sealed ) throw new InvalidOperationException( "This schema is sealed." );
			if( child == null ) throw new ArgumentNullException( "item", "Child cannot be null." );
			if( child._Owner != this ) throw new InvalidOperationException( "Child is registered into another schema." );

			bool r = _List.Remove( child ); if( r ) child._Owner = null;
			return r;
		}

		/// <summary>
		/// Clears this schema by removing all its meta columns, and optionally disposing them.
		/// </summary>
		/// <param name="disposeItems">True to dispose the metacolumns removed.</param>
		public void Clear( bool disposeItems = true )
		{
			if( _Sealed ) throw new InvalidOperationException( "This schema is sealed." );

			if( disposeItems ) foreach( var child in _List ) { child._Owner = null; child.Dispose(); }
			_List.Clear();
		}

		/// <summary>
		/// Gets the index of the meta column given, or -1 if it does not belong to this schema.
		/// </summary>
		/// <param name="child">The meta column to find its index.</param>
		/// <returns>The index of the meta column, or -1.</returns>
		public int IndexOf( KMetaColumn child )
		{
			if( child == null ) throw new ArgumentNullException( "child", "Child cannot be null." );
			return _List.IndexOf( child );
		}

		/// <summary>
		/// Gets the index of the <see cref="KMetaColumn"/> whose table and column names are given.
		/// <para>The table name can be an alias, which is converted to the actual table name to perform the search.</para>
		/// <para>The table name (converted or not) can be null to find an anonymous <see cref="KMetaColumn"/>.</para>
		/// <para>The anyTable argument can be set to true when it is acceptable to find a column regardless what table
		/// may it belong to. In this case, if several metacolumns are found, an exception is raised.</para>
		/// </summary>
		/// <param name="tableName">The table or alias name of the metacolumn to find. It can be null when we want to find
		/// an anonymous column.</param>
		/// <param name="columnName">The column name.</param>
		/// <param name="anyTable">True when it is acceptable to find a column regardless what table may it belong to.</param>
		/// <returns>The index of the meta column, or -1.</returns>
		public int IndexOf( string tableName, string columnName, bool anyTable = true )
		{
			KMetaColumn child = Find( tableName, columnName, anyTable: anyTable, raise: false );
			return child == null ? -1 : _List.IndexOf( child );
		}

		/// <summary>
		/// Gets a list with the meta columns in this schema.
		/// </summary>
		public List<KMetaColumn> Columns
		{
			get
			{
				List<KMetaColumn> list = new List<KMetaColumn>(); foreach( var child in _List ) list.Add( child );
				return list;
			}
		}

		/// <summary>
		/// Gets a list with the meta columns in this schema associated with primary key columns.
		/// </summary>
		public IEnumerable<KMetaColumn> PrimaryColumns
		{
			get
			{
				List<KMetaColumn> list = new List<KMetaColumn>(); foreach( var child in _List ) if( child.IsKey ) list.Add( child );
				return list;
			}
		}

		/// <summary>
		/// Gets a list with the meta columns in this schema associated with uniqued value columns.
		/// </summary>
		public IEnumerable<KMetaColumn> UniqueColumns
		{
			get
			{
				List<KMetaColumn> list = new List<KMetaColumn>(); foreach( var child in _List ) if( child.IsUnique ) list.Add( child );
				return list;
			}
		}
	}
}
// ========================================================
