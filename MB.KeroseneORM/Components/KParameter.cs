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
	/// Represents a parameter to use in a given command in an agnostic way.
	/// </summary>
	[Serializable]
	public class KParameter : IDisposable, ISerializable, ICloneable
	{
		string _Name = null;
		object _Value = null;
		internal KParameterList _Owner = null;

		/// <summary>
		/// Creates a new <see cref="KParameter"/> identified by its name and holding the value given.
		/// </summary>
		/// <param name="name">The name of this parameter.</param>
		/// <param name="value">The value this parameter holds. It can be null, but nulls are treated in a specialized way, so
		/// parameters holding a null value are not recommended.</param>
		public KParameter( string name, object value )
		{
			DEBUG.IndentLine( "\n-- KParameter( Name={0}, Value={1} )", name ?? "null", TypeHelper.ToString( value ) );

			_Name = name.Validated( "Name" );
			_Value = value;

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KParameter.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _Owner != null ) {
				if( _Owner._List != null ) _Owner._List.Remove( this );
				_Owner = null;
			}
			_Value = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KParameter.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KParameter()
		{
			DEBUG.IndentLine( "\n-- KParameter~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( "ParameterName", _Name );
			info.AddValue( "ParameterType", _Value == null ? "VOID" : _Value.GetType().AssemblyQualifiedName );
			info.AddValue( "ParameterValue", _Value );
		}
		protected KParameter( SerializationInfo info, StreamingContext context )
		{
			_Name = info.GetString( "ParameterName" );
			string type = info.GetString( "ParameterType" );
			_Value = type == "VOID" ? null : info.GetValue( "ParameterValue", Type.GetType( type ) );
		}

		/// <summary>
		/// Clones this parameter returning a new orphan one not associated with any collection.
		/// </summary>
		/// <returns>A new clone of this parameter.</returns>
		public KParameter Clone()
		{
			var r = new KParameter( _Name, TypeHelper.TryClone( _Value ) );
			return r;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public override string ToString()
		{
			return string.Format( "{0} = {1}", _Name ?? "null", TypeHelper.ToString( _Value ) );
		}

		/// <summary>
		/// Gets the name of this parameter.
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}

		/// <summary>
		/// Gets or sets the value this parameter holds.
		/// </summary>
		public object Value
		{
			get { return _Value; }
			set { _Value = value; }
		}
	}

	// =====================================================
	/// <summary>
	/// Represents the list of parameters to be used with a given command in an agnostic way.
	/// </summary>
	[Serializable]
	public class KParameterList : IDisposable, ISerializable, ICloneable, IEnumerable<KParameter>
	{
		internal List<KParameter> _List = new List<KParameter>();
		string _Prefix = null;
		int _Next = 0;

		/// <summary>
		/// Creates a new <see cref="KParameterList"/> instance, setting the default prefix to the value given.
		/// <para>The default prefix is used to build the names of the parameters added using only the value, by prepending
		/// this prefix to an internally maintained serial number.</para>
		/// </summary>
		/// <param name="prefix">The default prefix to use. This literal depends on the specific database used.</param>
		public KParameterList( string prefix )
		{
			DEBUG.IndentLine( "\n-- KParameterList( Prefix={0} )", prefix ?? "null" );
			_Prefix = prefix.Validated( "Prefix" );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KParameterList.Dispose( Disposing={0} ) - This={1}", disposing, this );
			if( _List != null ) {
				foreach( var par in _List ) { par._Owner = null; par.Dispose(); }
				_List.Clear(); _List = null;
			}
			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KParameterList.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KParameterList()
		{
			DEBUG.IndentLine( "\n-- KParameterList~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( "Prefix", _Prefix );
			info.AddValue( "NextOrdinal", _Next );

			info.AddValue( "ParameterCount", _List.Count ); for( int i = 0; i < _List.Count; i++ ) {
				info.AddValue( "Parameter" + i, _List[i] );
			}
		}
		protected KParameterList( SerializationInfo info, StreamingContext context )
		{
			_Prefix = info.GetString( "Prefix" );
			_Next = (int)info.GetValue( "NextOrdinal", typeof( int ) );

			int count = (int)info.GetValue( "ParameterCount", typeof( int ) ); for( int i = 0; i < count; i++ ) {
				KParameter par = (KParameter)info.GetValue( "Parameter" + i, typeof( KParameter ) );
				par._Owner = this; _List.Add( par );
			}
		}

		/// <summary>
		/// Creates a clone of this collection, by also cloning all its parameters.
		/// </summary>
		/// <returns>A new clone of this collection.</returns>
		public KParameterList Clone()
		{
			KParameterList cloned = new KParameterList( _Prefix );
			foreach( var item in _List ) { cloned._List.Add( item.Clone() ); item._Owner = cloned; }
			cloned._Next = _Next;
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public IEnumerator<KParameter> GetEnumerator()
		{
			return _List.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public override string ToString()
		{
			if( _List == null ) return "<KParameterList>";

			StringBuilder sb = new StringBuilder();
			sb.Append( "{" ); bool first = true; foreach( var par in _List ) {
				if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
				sb.Append( par.ToString() );
			}
			if( !first ) sb.Append( " " ); else sb.Append( "-" );
			sb.Append( "}" );
			return sb.ToString();
		}

		/// <summary>
		/// Gets the default prefix to be used when building names for new parameters, if needed.
		/// </summary>
		public string Prefix
		{
			get { return _Prefix; }
		}

		/// <summary>
		/// Gets the number of parameters this list has, or -1 if it has been disposed.
		/// </summary>
		public int Count
		{
			get { return _List == null ? -1 : _List.Count; }
		}

		/// <summary>
		/// Gets the parameter whose name is given.
		/// </summary>
		/// <param name="name">The name of the parameter to find.</param>
		/// <param name="raise">True to raise an exception if the parameter is not found.</param>
		/// <returns>The parameter found, or null.</returns>
		public KParameter this[string name, bool raise = false]
		{
			get
			{
				KParameter par = null;
				if( name != null ) par = _List.Find( x => x.Name == name );

				if( par == null && raise ) throw new KeyNotFoundException( "Parameter not found: " + name );
				return par;
			}
		}

		/// <summary>
		/// Gets the parameter stored at the index given.
		/// It raises an exception if the index is invalid.
		/// </summary>
		/// <param name="index">The index of the parameter to return.</param>
		/// <returns>The parameter at the index given.</returns>
		public KParameter this[int index]
		{
			get { return _List[index]; }
		}

		/// <summary>
		/// Adds the given parameter to this collection of parameters.
		/// <para>If it belongs to another collection, an exception will be raised.</para>
		/// <para>For these scenarios you can use Clone() to generate a clone of the parameter to add but that will be not
		/// associated with any other collection.</para>
		/// </summary>
		/// <param name="par">The parameter to add to this collection.</param>
		public void Add( KParameter par )
		{
			if( par == null ) throw new ArgumentNullException( "par", "Parameter cannot be null." );
			if( par._Owner != null ) throw new InvalidOperationException( "Parameter alredy belong to another collection." );

			if( this[par.Name, raise: false] != null )
				throw new ArgumentException( "A parameter with the same name already exists: " + par.Name );

			_List.Add( par ); par._Owner = this;
		}

		/// <summary>
		/// Removes the given parameter from this collection, making it an orphan parameter.
		/// <para>If the parameter does not belong to this collection an exception is raised.</para>
		/// </summary>
		/// <param name="par">The parameter to remove.</param>
		/// <returns>True if the parameter has been removed, false otherwise.</returns>
		public bool Remove( KParameter par )
		{
			if( par == null ) throw new ArgumentNullException( "par", "Parameter cannot be null." );
			if( par._Owner != this ) throw new InvalidOperationException( "Parameter does not belong to this collection: " + par.Name );

			bool r = _List.Remove( par ); if( r ) par._Owner = null;
			return r;
		}
		
		/// <summary>
		/// Clears this collection by removing all its parameters, and opionally by disposing them.
		/// </summary>
		/// <param name="disposeItems">To dispose the child parameters while removing them.</param>
		public void Clear( bool disposeItems = true )
		{
			if( disposeItems ) foreach( var par in _List ) { par._Owner = null; par.Dispose(); }
			_List.Clear();
		}

		/// <summary>
		/// Adds a range of parameters into this collection.
		/// <para>If any of the parameters in the range belongs to another collection, then a clone of it is obtained and
		/// added instead.</para>
		/// </summary>
		/// <param name="list">The range of parameters to add.</param>
		public void AddRange( IEnumerable<KParameter> list )
		{
			if( list == null ) throw new ArgumentNullException( "list", "List cannot be null." );
			foreach( var par in list ) Add( par._Owner == null ? par : par.Clone() );
		}

		/// <summary>
		/// Creates and inserts a new parameter into this collection, using the name and value given.
		/// </summary>
		/// <param name="name">The name of the new parameter to insert.</param>
		/// <param name="value">The value the new parameter will hold.</param>
		/// <returns>The newly created parameter.</returns>
		public KParameter Insert( string name, object value )
		{
			KParameter par = this[name, raise: false];
			if( par != null ) throw new InvalidOperationException( "Parameter name already exists: " + name );

			par = new KParameter( name, value ); _List.Add( par ); par._Owner = this;
			return par;

		}
		
		/// <summary>
		/// Creates and inserts a new parameter to hold the value given, using an automatic name built from the default prefix
		/// and an internal serial number.
		/// </summary>
		/// <param name="value">The value the new parameter will hold.</param>
		/// <returns>The newly created parameter.</returns>
		public KParameter Insert( object value )
		{
			int ix = 0; while( ix < int.MaxValue ) {
				string name = _Prefix + ( ix = ( _Next++ ) ); // Increasing Next for the next iteration
				KParameter par = this[name, raise: false];
				if( par != null ) continue;

				par = new KParameter( name, value ); _List.Add( par ); par._Owner = this;
				return par;
			}
			throw new InvalidOperationException( "Reached maximum identifier for parameters." );
		}
	}
}
// ========================================================
