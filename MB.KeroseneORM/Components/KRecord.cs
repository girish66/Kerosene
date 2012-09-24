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
	using global::System.Linq;
	using global::System.Dynamic;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents a record: a dynamic object used to transfer information back and forth a database (among other duties).
	/// <para>A record is associated with a given schema, used to identify the columns in the record in order to get and set
	/// their contents.</para>
	/// <para>Records are dynamic, so they support either an indexed access syntax, or a dynamic one.</para>
	/// </summary>
	[Serializable]
	public class KRecord : DynamicObject, IDisposable, IEnumerable, ICloneable, ISerializable
	{
		object[] _Columns = null;
		KSchema _Schema = null;
		bool _Sealed = false;
		bool _SerializeSchema = false;

		private KRecord() { } // For the specialized Clone() and Changes() methods...

		/// <summary>
		/// Creates a new <see cref="KRecord"/> instance using the given schema to define its columns.
		/// <para>The schema will be sealed in order to avoid changes to it (because the structure of the record is created
		/// in this constructor, but the record does not keep track of any future changes in the schema).</para>
		/// </summary>
		/// <param name="schema">The schema defining the columns in this record.</param>
		public KRecord( KSchema schema )
		{
			DEBUG.IndentLine( "\n-- KRecord( Schema={0} )", schema == null ? "null" : schema.ToString() );

			if( ( _Schema = schema ) == null ) throw new ArgumentNullException( "schema", "Schema cannot be null." );
			if( _Schema.Count <= 0 ) throw new ArgumentException( "Invalid number of columns in schema: " + _Schema );
			_Schema.Sealed = true;
			_Columns = new object[_Schema.Count];

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KRecord.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _Columns != null ) {
				for( int i = 0; i < _Columns.Length; i++ ) _Columns[i] = null;
				_Columns = null;
			}
			_Schema = null; // Might be in use elsewhere

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KRecord.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KRecord()
		{
			DEBUG.IndentLine( "\n-- KRecord~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			if( _Columns == null ) return "<KRecord>";
			if( _Schema == null || _Schema.Count <= 0 ) return TypeHelper.ToString( _Columns, "[]" ); // Don't forget "<= 0"

			StringBuilder sb = new StringBuilder();
			sb.Append( "[" ); bool first = true; for( int i = 0; i < _Columns.Length; i++ ) {
				if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
				if( _Schema == null ) sb.AppendFormat( "{0}={1}", i, TypeHelper.ToString( _Columns[i] ) );
				else sb.AppendFormat( "{0}={1}", _Schema[i].FullName, TypeHelper.ToString( _Columns[i] ) );
			}
			if( !first ) sb.Append( " " ); else sb.Append( "-" );
			sb.Append( "]" );
			return sb.ToString();
		}

		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			int count = _Columns.Length; info.AddValue( "ColumnCount", count );

			for( int i = 0; i < count; i++ ) {
				info.AddValue( "ColumnType" + i, _Columns[i] == null ? "VOID" : _Columns[i].GetType().AssemblyQualifiedName );
				if( _Columns[i] != null ) info.AddValue( "ColumnValue" + i, _Columns[i] );
			}

			info.AddValue( "RecordSealed", _Sealed );

			info.AddValue( "SerializeSchema", _SerializeSchema );
			if( _SerializeSchema ) info.AddValue( "Schema", _Schema );
		}
		protected KRecord( SerializationInfo info, StreamingContext context )
		{
			int count = (int)info.GetValue( "ColumnCount", typeof( int ) );
			_Columns = new object[count];

			for( int i = 0; i < count; i++ ) {
				string type = info.GetString( "ColumnType" + i );
				object value = type == "VOID" ? null : info.GetValue( "ColumnValue" + i, Type.GetType( type ) );
				_Columns[i] = value;
			}

			_Sealed = info.GetBoolean( "RecordSealed" );

			_SerializeSchema = info.GetBoolean( "SerializeSchema" );
			if( _SerializeSchema ) _Schema = (KSchema)info.GetValue( "Schema", typeof( KSchema ) );
		}

		/// <summary>
		/// Gets or sets whether to serialize the schema this record is associated to.
		/// <para>For performance reasons, the record's schema is not serialized by default, so when it is deserialized, its
		/// Schema property will be null and should be set manually to a valid instance obtained from elsewhere.</para>
		/// <para>This property can be set to <see cref="true"/> to serialize the schema along with the record.</para>
		/// </summary>
		public bool SerializeSchema
		{
			get { return _SerializeSchema; }
			set { _SerializeSchema = value; }
		}

		/// <summary>
		/// Returns a new clone of this record, optionally cloning the schema it was associated to.
		/// <para>If cloning the schema was not requested, then the new clone will be associated whith the original schema
		/// instance. Otherwise, it will be associated with a clone of the original schema.</para>
		/// <para>It also tries to clone the contents of the original record, if possible.</para>
		/// </summary>
		/// <param name="cloneSchema">True to also clone the schema.</param>
		/// <returns>The newly created clone.</returns>
		public KRecord Clone( bool cloneSchema )
		{
			KRecord cloned = new KRecord();

			if( _Schema == null ) throw new InvalidOperationException( "Cannot clone from a record with a null schema." );
			if( _Schema.Count == 0 ) throw new InvalidOperationException( "Cannot clone from a record with an empty schema." );
			cloned._Columns = new object[_Schema.Count];

			for( int i = 0; i < _Columns.Length; i++ ) cloned._Columns[i] = TypeHelper.TryClone( _Columns[i] );
			cloned._Schema = cloneSchema ? _Schema.Clone() : _Schema;
			cloned._Sealed = _Sealed;
			cloned._SerializeSchema = _SerializeSchema;
			return cloned;
		}

		/// <summary>
		/// Gets a clone of this record, associated with the original schema. Its contents are also cloned, if possible.
		/// </summary>
		/// <returns>The newly created clone.</returns>
		public KRecord Clone()
		{
			return Clone( cloneSchema: false ); // By default clones are associated with the same schema
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Gets the schema this record is associated to.
		/// <para>The setter can only be used in specialized serialization scenarios when the SerializeSchema property
		/// is <see cref="false"/>.</para>
		/// <para>In these cases, the schema has not been deserialized with the record, and it should be set with a valid
		/// reference obtained from elsewhere.</para>
		/// </summary>
		public KSchema Schema
		{
			get { return _Schema; }
			set
			{
				if( _Schema != null ) throw new InvalidOperationException( "Schema is already set." );
				if( value.Count <= 0 ) throw new ArgumentException( "Schema has no columns or has been disposed." );
				if( value.Count != _Columns.Length ) throw new ArgumentException( "Number of columns in the schema and in the record don't match: " + value.Count + "/" + _Columns.Length );
				_Schema = value;
			}
		}

		public IEnumerator GetEnumerator()
		{
			return _Columns.GetEnumerator();
		}

		/// <summary>
		/// Gets the number of columns this record has, or -1 if it has been disposed.
		/// </summary>
		public int Count
		{
			get { return _Columns == null ? -1 : _Columns.Length; }
		}

		/// <summary>
		/// Gets or sets whether this record is sealed, meaning it won't accept any changes in the values it stores.
		/// </summary>
		public bool Sealed
		{
			get { return _Sealed; }
			set { _Sealed = value; }
		}

		/// <summary>
		/// Gets or sets the value of the column whose table and column names are given.
		/// <para>The table name can be an alias, which is converted to the actual table name to perform the search.</para>
		/// <para>The table name (converted or not) can be null to find an anonymous <see cref="KMetaColumn"/>.</para>
		/// <para>The anyTable argument can be set to true when it is acceptable to find a column regardless what table
		/// may it belong to. In this case, if several metacolumns are found, an exception is raised.</para>
		/// </summary>
		/// <param name="tableName">The table or alias name of the metacolumn to find. It can be null when we want to find
		/// an anonymous column.</param>
		/// <param name="columnName">The column name.</param>
		/// <param name="anyTable">True when it is acceptable to find a column regardless what table may it belong to.</param>
		/// <returns>The getter returns the value stored by the column found.</returns>
		public object this[string tableName, string columnName, bool anyTable = true]
		{
			get
			{
				int ix = _Schema.IndexOf( tableName, columnName, anyTable );
				if( ix < 0 ) throw new ArgumentException( "Column not found: " + string.Format( "{0}.{1}", tableName, columnName ) );
				return _Columns[ix];
			}
			set
			{
				if( _Sealed ) throw new InvalidOperationException( "This record is sealed." );
				int ix = _Schema.IndexOf( tableName, columnName, anyTable );
				if( ix < 0 ) throw new ArgumentException( "Column not found: " + string.Format( "{0}.{1}", tableName, columnName ) );
				_Columns[ix] = value;
			}
		}

		/// <summary>
		/// Gets or sets the value of the column whose name is given.
		/// <para>This method will only accept the cases where those column names are unique among all possible columns.</para>
		/// <para>Otherwise an exception will be thrown.</para>
		/// </summary>
		/// <param name="columnName">The column name.</param>
		/// <returns>The getter returns the value stored by the column found.</returns>
		public object this[string columnName]
		{
			get
			{
				int ix = _Schema.IndexOf( null, columnName, anyTable: true );
				if( ix < 0 ) throw new ArgumentException( "Column not found: " + string.Format( ".{0}", columnName ) );
				return _Columns[ix];
			}
			set
			{
				if( _Sealed ) throw new InvalidOperationException( "This record is sealed." );
				int ix = _Schema.IndexOf( null, columnName, anyTable: true );
				if( ix < 0 ) throw new ArgumentException( "Column not found: " + string.Format( ".{0}", columnName ) );
				_Columns[ix] = value;
			}
		}

		/// <summary>
		/// Gets or sets the contents of the column whose index is given. If the index is invalid, an exception is thrown.
		/// </summary>
		/// <param name="index">The index of the column.</param>
		/// <returns>The getter returns the contents of the column specified.</returns>
		public object this[int index]
		{
			get { return _Columns[index]; }
			set
			{
				if( _Sealed ) throw new InvalidOperationException( "This record is sealed." );
				_Columns[index] = value;
			}
		}

		/// <summary>
		/// Returns a new ad-hoc record containing the changes from this record compared against the other record given.
		/// <para>Strict mode can be requeste, in which case all columns in both records are taken in consideration.</para>
		/// <para>When not in strict mode, only the columns of the source record are compared.</para>
		/// </summary>
		/// <param name="other">The other record to use in the comparison.</param>
		/// <param name="strict">True to activate strict mode, false otherwise.</param>
		/// <returns>A new record containing the changes and its schema, or null if no changes were detected.</returns>
		public KRecord Changes( KRecord other, bool strict = false )
		{
			if( this._Schema == null ) throw new InvalidOperationException( "This record's schema is null." );
			if( this.Schema.Count < 0 ) throw new InvalidOperationException( "This record's schema is disposed." );

			if( other == null ) throw new ArgumentNullException( "other", "Other record cannot be null." );
			if( other.Schema == null ) throw new InvalidOperationException( "Other record's schema is null." );
			if( other.Schema.Count < 0 ) throw new InvalidOperationException( "Other record's schema is disposed." );

			// If any record is empty...
			if( this.Schema.Count == 0 ) {
				if( other.Schema.Count == 0 ) return null; // Both are empty records...
				if( !strict ) return null; // We have no changes to inform about...
				return other.Clone( cloneSchema: true ); // All other contents are consideres as changed...
			}
			if( other.Schema.Count == 0 ) {
				if( !strict ) return null; // We have no changes to inform about...
				return this.Clone( cloneSchema: true ); // All other contents are consideres as changed...
			}

			// Preparing for returning a new record containing the changes...
			KSchema schema = new KSchema( _Schema.CaseSensitiveNames );
			List<object> values = new List<object>();
			bool anyTable = strict ? false : true;

			// First round: using this record's columns...
			for( int thisIx = 0; thisIx < this.Schema.Count; thisIx++ ) {
				var thisMeta = this.Schema[thisIx];
				var otherMeta = other.Schema.Find( thisMeta.BaseTableName, thisMeta.ColumnName, anyTable, raise: false );

				if( otherMeta == null ) { // Columns not found in other are considered only in strict mode...
					if( strict ) {
						object thisValue = this[thisIx];
						schema.Add( thisMeta.Clone() );
						values.Add( TypeHelper.TryClone( thisValue ) );
					}
				}
				else { // Only annotating when their contents are not equivalent...
					int otherIx = other.Schema.IndexOf( otherMeta );
					object thisValue = this[thisIx];
					object otherValue = other[otherIx];
					bool eq = TypeHelper.AreEquivalent( thisValue, otherValue ); if( !eq ) {
						schema.Add( thisMeta.Clone() );
						values.Add( TypeHelper.TryClone( thisValue ) );
					}
				}
			}

			// Second round: in strict we need to consider also the columns in other that do not appear in this...
			if( strict ) {
				for( int otherIx = 0; otherIx < other.Schema.Count; otherIx++ ) {
					var otherMeta = other.Schema[otherIx];
					var thisMeta = this.Schema.Find( otherMeta.BaseTableName, otherMeta.ColumnName, anyTable: false, raise: false );

					if( thisMeta == null ) { // Adding from a new column in the other record...
						var temp = schema.Find( otherMeta.BaseTableName, otherMeta.ColumnName, anyTable: true, raise: false );
						
						if( temp == null ) { // Avoiding exception per duplicate columns...
							object otherValue = other[otherIx];
							schema.Add( otherMeta.Clone() );
							values.Add( TypeHelper.TryClone( otherValue ) );
						}
					}
				}
			}

			// If no changes return null to facilitate comparisons...
			if( schema.Count == 0 ) { values = null; schema.Dispose(); schema = null; return null; }

			// Otherwise, generating the actual record containing the changes...
			KRecord record = new KRecord();
			record._Schema = schema;
			record._Columns = values.ToArray();
			return record;
		}

		/// <summary>
		/// Returns whether this record can be considered equivalent to the other record.
		/// <para>Strict mode can be requested, in which case all columns in both records are taken in consideration.</para>
		/// <para>When not in strict mode, only the columns of the source record are compared.</para>
		/// </summary>
		/// <param name="other">The other record to use in the comparison.</param>
		/// <param name="strict">True to activate strict mode, false otherwise.</param>
		/// <returns>Whether this record can be considered equivalent to the other record.</returns>
		public bool EquivalentTo( KRecord other, bool strict = true )
		{
			KRecord changes = Changes( other, strict );
			bool r = changes == null ? true : false;

			if( changes != null ) { changes.Schema.Dispose(); changes.Dispose(); }
			return r;
		}

		/// <summary>
		/// Clear the contents of this record setting all its columns to a default value of <see cref="null"/>.
		/// </summary>
		public void Clear()
		{
			if( _Sealed ) throw new InvalidOperationException( "This record is sealed." );
			if( _Columns != null ) for( int i = 0; i < _Columns.Length; i++ ) _Columns[i] = null;
		}

		public override bool TryGetMember( GetMemberBinder binder, out object result )
		{
			var meta = _Schema.Find( null, binder.Name, anyTable: true, raise: false );
			if( meta != null ) { int ix = _Schema.IndexOf( meta ); result = _Columns[ix]; }

			// We defer the getter to be located using the binder's name as the table name...
			else result = new KRecordDynamicTable( this, binder.Name );
			return true;
		}
		public override bool TrySetMember( SetMemberBinder binder, object value )
		{
			var meta = _Schema.Find( null, binder.Name, anyTable: true, raise: false );
			if( meta != null ) { int ix = _Schema.IndexOf( meta ); _Columns[ix] = value; }

			// For compose specifications, the dynamic getter has taken care of the Table part...
			else throw new KeyNotFoundException( "Member not found: " + binder.Name );
			return true;
		}
	}

	// =====================================================
	public class KRecordDynamicTable : DynamicObject, IDisposable
	{
		KRecord _Record = null;
		string _Table = null;

		internal KRecordDynamicTable( KRecord record, string table )
		{
			_Record = record;
			_Table = table.Validated( "Table Name" );
		}

		protected virtual void Dispose( bool disposing )
		{
			_Record = null;
			_Table = null;
		}
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		~KRecordDynamicTable()
		{
			Dispose( false );
		}

		public override bool TryGetMember( GetMemberBinder binder, out object result )
		{
			try {
				int ix = _Record.Schema.IndexOf( _Table, binder.Name, anyTable: false );
				if( ix >= 0 ) { result = _Record[ix]; return true; }
				throw new KeyNotFoundException( "Column not found: " + string.Format( "{0}.{1}", _Table, binder.Name ) );
			}
			finally { Dispose(); }
		}
		public override bool TrySetMember( SetMemberBinder binder, object value )
		{
			try {
				int ix = _Record.Schema.IndexOf( _Table, binder.Name, anyTable: false );
				if( ix >= 0 ) { _Record[ix] = value; return true; }
				throw new KeyNotFoundException( "Column not found: " + string.Format( "{0}.{1}", _Table, binder.Name ) );
			}
			finally { Dispose(); }
		}
	}
}
// ========================================================
