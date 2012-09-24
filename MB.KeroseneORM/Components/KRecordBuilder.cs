// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::System.Dynamic;
	using global::System.Linq.Expressions;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	/// <summary>
	/// Represents an object able to build a record using a dynamic and handy way.
	/// </summary>
	public class KRecordBuilder : DynamicObject, IDisposable
	{
		KSchema _Schema = null;
		List<object> _Values = new List<object>();

		/// <summary>
		/// Creates a new instance of <see cref="KRecordBuilder"/>.
		/// <para>Its TableAliasList property can be used to add the aliases needed for the schema.</para>
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the names of tables and columns are considered case sensitively or
		/// not.</param>
		public KRecordBuilder( bool caseSensitiveNames )
		{
			DEBUG.IndentLine( "\n-- KRecordBuilder( CaseSensitive={0} )", caseSensitiveNames );
			_Schema = new KSchema( caseSensitiveNames );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KRecordBuilder.Dispose( Disposing={0} ) - This={1}", disposing, this );
			if( _Values != null ) { _Values.Clear(); _Values = null; }
			_Schema = null; // It might be in use elsewhere
			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KRecordBuilder.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KRecordBuilder()
		{
			DEBUG.IndentLine( "\n-- KRecordBuilder~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		/// <summary>
		/// Gets the <see cref="KTableAliasList"/> object in use with the schema to generate the record.
		/// </summary>
		public KTableAliasList TableAliasList
		{
			get { return _Schema.TableAliasList; }
		}

		/// <summary>
		/// Builds and returns a new record using the schema and values stored in this builder.
		/// </summary>
		/// <param name="autoDispose">True to dispose this builder as soon as the new record is generated.</param>
		/// <returns>A new record built using the schema and values stored in this builder.</returns>
		public KRecord Generate( bool autoDispose = true )
		{
			if( _Schema == null ) throw new InvalidOperationException( "Builder Schema is null." );
			if( _Schema.Count < 0 ) throw new InvalidOperationException( "Builder Schema is disposed." );
			if( _Schema.Count == 0 ) throw new InvalidOperationException( "Builder Schema is empty." );

			KRecord record = new KRecord( _Schema );
			for( int i = 0; i < _Values.Count; i++ ) record[i] = _Values[i];

			if( autoDispose ) Dispose();
			return record;
		}

		/// <summary>
		/// Adds a new column to the record using the given table name, column name, and value.
		/// </summary>
		/// <param name="tableName">The table name. Null are acceptable to specify an anonymous meta column.</param>
		/// <param name="columnName">The column name.</param>
		/// <param name="value">The value to set for the new column.</param>
		public void Add( string tableName, string columnName, object value )
		{
			// If the table name is an alias, substitute it with the real table name...
			KTableAlias assoc = tableName == null ? null : TableAliasList.FindByAlias( tableName );
			if( assoc != null ) tableName = assoc.Table;

			_Schema.Add( new KMetaColumn( tableName, columnName ) );
			_Values.Add( value );
		}

		/// <summary>
		/// Adds a new column to the record obtaining its details from the dynamic lambda expression given.
		/// <para>The expression can be of any of the following forms: "x => x.Column = value", "x => x.Column == value",
		/// "x => x.Table.Column = value", or "x => Table.Column == value". Any other form is not supported.</para>
		/// </summary>
		/// <param name="spec">The dynamic lambda expression to obtain the details of the new column to add.</param>
		public void Add( Func<dynamic, object> spec )
		{
			if( spec == null ) throw new ArgumentNullException( "spec", "Column specification cannot be null." );
			var result = DynamicParser.Parse( spec ).Result;
			if( result == null ) throw new ArgumentException( "Column specification cannot resolve to null." );

			// Processing syntax: "x => x.Column = Value" or "x => x.Table.Column = value" ...
			DynamicNode.SetMember setNode = result as DynamicNode.SetMember;
			if( setNode != null ) {
				if( setNode.Host is DynamicNode.Argument ) { Add( null, setNode.Name, setNode.Value ); return; }
				else {
					DynamicNode.GetMember getNode = setNode.Host as DynamicNode.GetMember;
					if( getNode.Host is DynamicNode.Argument ) { Add( getNode.Name, setNode.Name, setNode.Value ); return; }
				}
			}

			// Processing syntax: "x => x.Column == Value" or "x => x.Table.Column == value" ...
			DynamicNode.Binary binNode = result as DynamicNode.Binary;
			if( binNode != null ) {
				if( binNode.Operation != ExpressionType.Equal ) throw new ArgumentException( "Expression is not an equality comparison: " + binNode );
				DynamicNode.GetMember getNode = binNode.Left as DynamicNode.GetMember;
				if( getNode != null ) {
					if( getNode.Host is DynamicNode.Argument ) { Add( null, getNode.Name, binNode.Right ); return; }
					else {
						DynamicNode.GetMember hostNode = getNode.Host as DynamicNode.GetMember;
						if( hostNode != null ) { Add( hostNode.Name, getNode.Name, binNode.Right ); return; }
					}
				}
			}

			// Unsupported specification...
			throw new ArgumentException( "Invalid specification: " + result );
		}

		public override bool TryGetMember( GetMemberBinder binder, out object result )
		{
			// Only invoked in forms of type "x => x.Table.Column" ...
			result = new KRecordBuilderDynamicTable( this, binder.Name );
			return true;
		}
		public override bool TrySetMember( SetMemberBinder binder, object value )
		{
			Add( null, binder.Name, value );
			return true;
		}
	}

	// =====================================================
	public class KRecordBuilderDynamicTable : DynamicObject, IDisposable
	{
		KRecordBuilder _Builder = null;
		string _Table = null;

		internal KRecordBuilderDynamicTable( KRecordBuilder builder, string table )
		{
			_Builder = builder;
			_Table = table.Validated( "Table Name" );
		}

		protected virtual void Dispose( bool disposing )
		{
			_Builder = null;
			_Table = null;
		}
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		~KRecordBuilderDynamicTable()
		{
			Dispose( false );
		}

		public override bool TrySetMember( SetMemberBinder binder, object value )
		{
			_Builder.Add( _Table, binder.Name, value );
			return true;
		}
	}
}
// ========================================================
