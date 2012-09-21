// ========================================================
#undef DEBUG
namespace MB.Tools.Dynamics
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Text;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::System.Dynamic;
	using global::System.Runtime.Serialization;

	// =====================================================
	public partial class DeepObject
	{
		/// <summary>
		/// Permits the direct manipulation of the information a <see cref="DeepObject"/> is carrying.
		/// </summary>
		public class DeepCarrier
		{
			internal DeepObject _Owner = null;

			internal DeepObject _Host = null;
			internal string _Name = null;
			internal bool _IsIndex = false;
			internal object _Value = null;
			internal bool _HasValue = false;
			internal bool _Sealed = false;
			internal bool _CaseSensitiveNames = true;
			internal List<DeepObject> _Members = null;

			internal DeepCarrier( DeepObject owner )
			{
				_Owner = owner;
			}
			internal void Destroy()
			{
				_Owner = null;
				_Host = null;
				_Value = null;
				if( _Members != null ) _Members.Clear(); _Members = null;
			}

			// --------------------------------------------------
			public string ToString( bool extended )
			{
				StringBuilder sb = new StringBuilder();
				sb.Append( Name );
				if( _HasValue ) sb.AppendFormat( " = {0}", TypeHelper.ToString( _Value ) );

				if( extended ) {
					if( _Members != null ) {
						if( sb.ToString().Length != 0 ) sb.Append( " " );

						sb.Append( "{" ); bool first = true; foreach( var member in _Members ) {
							if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
							sb.Append( member.ToString( extended: true ) );
						}
						if( !first ) sb.Append( " " ); else sb.Append( "-" );
						sb.Append( "}" );
					}
				}
				return sb.ToString();
			}
			public override string ToString()
			{
				return ToString( extended: true );
			}

			/// <summary>
			/// Gets the name of the DeepObject. It can be null if it has been created without assigning to it a name in its
			/// constructor, otherwise is represents the property name of this object in its hosting object, or a concatenation
			/// of the indexes used to create it.
			/// </summary>
			public string Name
			{
				get { return _Name; }
			}
			
			/// <summary>
			/// Gets the full name of this DeepObject, composed by the name of its hosting instance, if exists, plus a dot and
			/// the proper name of this DeepObject.
			/// </summary>
			public string FullName
			{
				get
				{
					string host = _Host == null ? null : _Host.Deep.FullName;
					string name = _Name ?? ( host == null ? null : "#" );
					return host == null ? name : string.Format( "{0}{1}{2}", host, _IsIndex ? "" : ".", name );
				}
			}

			/// <summary>
			/// Gets whether this DeepObject is an indexed property of its hosting instance or not.
			/// </summary>
			public bool IsIndex
			{
				get { return _IsIndex; }
			}

			/// <summary>
			/// Gets whether a value has been assigned to this DeepObject. By default DeepObjects are in an empty state when no
			/// value has been assigned to them, and in these cases this property returns false.
			/// </summary>
			public bool HasValue
			{
				get { return _HasValue; }
			}

			/// <summary>
			/// Gets or sets the value assigned to this DeepObject. When assigning a value with the setter, it modifies the
			/// its internal state so that its HasValue property will return true.
			/// </summary>
			public object Value
			{
				get { return _Value; }
				set
				{
					_Value = value;
					_HasValue = true;
				}
			}
			
			/// <summary>
			/// Resets the state of this DeepObject cleaning its value to a default value of null, so returning false from the
			/// next invocation of its HasValue property.
			/// </summary>
			public void ResetValue()
			{
				_Value = null;
				_HasValue = false;
			}

			/// <summary>
			/// Gets the hosting instance of this DeepObject. It can be null if this DeepObject is not hosted.
			/// </summary>
			public DeepObject Host
			{
				get { return _Host; }
			}
			
			/// <summary>
			/// Gets the level a this DeepObject, defined as 0 if it is not hosted, 1 if its host has no parent, and so forth.
			/// </summary>
			public int Level
			{
				get { return _Host == null ? 0 : _Host.Deep.Level + 1; }
			}

			/// <summary>
			/// Gets whether the names of the properties are treated case sensitively (the default standard C# mechanism) or
			/// not.
			/// </summary>
			public bool CaseSensitiveNames
			{
				get { return _CaseSensitiveNames; }
			}

			/// <summary>
			/// Gets or sets whether this DeepObject is sealed. A sealed DeepObject will not accept any further changes in its
			/// structure, but there will be no problems to getting or setting the values in its dynamic properties.
			/// </summary>
			public bool Sealed
			{
				get { return _Sealed; }
				set { _Sealed = value; }
			}

			/// <summary>
			/// Permits to enumerate through the dynamic members (properties) this DeepObject has.
			/// </summary>
			public IEnumerable<DeepObject> Members
			{
				get
				{
					if( _Members == null ) yield return null;
					foreach( var member in _Members ) yield return member;
				}
			}
			
			/// <summary>
			/// Gets the number of dynamic members (properties) this DeepObject has.
			/// </summary>
			public int Count
			{
				get { return _Members == null ? -1 : _Members.Count; }
			}

			/// <summary>
			/// Returns the index at which the given member is stored.
			/// </summary>
			/// <param name="member">The member to obtain its index for.</param>
			/// <returns>The index at which the given member is stored, or -1.</returns>
			public int IndexOf( DeepObject member )
			{
				if( member == null ) throw new ArgumentNullException( "member", "Member cannot be null." );
				if( _Members == null ) return -1;
				int r = _Members.IndexOf( member );
				return r;
			}

			/// <summary>
			/// Clears this DeepObject by removing all its members, and optionally disposing them.
			/// </summary>
			/// <param name="disposeMembers">True to dispose its members while removing them.</param>
			public void Clear( bool disposeMembers = true )
			{
				if( _Sealed ) throw new InvalidOperationException( "This object is sealed." );
				if( _Members != null ) {
					if( disposeMembers ) foreach( var member in _Members ) member.Dispose();
					_Members.Clear();
					_Members = null;
				}
			}
			
			/// <summary>
			/// Removes the given member from this DeepObject.
			/// </summary>
			/// <param name="member">The member to remove.</param>
			/// <returns>True if the member has been removed, false otherwise.</returns>
			public bool RemoveMember( DeepObject member )
			{
				if( _Sealed ) throw new InvalidOperationException( "This object is sealed." );
				if( member == null ) throw new ArgumentNullException( "member", "Member to remove cannot be null." );
				bool r = false;

				if( _Members != null ) {
					r = _Members.Remove( member ); if( r ) member.Deep._Host = null;
					if( _Members.Count == 0 ) _Members = null;
				}
				return r;
			}
			
			/// <summary>
			/// Finds the member at the index given.
			/// </summary>
			/// <param name="index">The index where to find the member at.</param>
			/// <param name="raise">True to raise an exception if the member is not found - the index is invalid.</param>
			/// <returns>The member found at the index, or null.</returns>
			public DeepObject FindMember( int index, bool raise = false )
			{
				if( index < 0 ) {
					if( raise ) throw new IndexOutOfRangeException( "Index is less than cero." );
					return null;
				}
				if( _Members == null ) {
					if( raise ) throw new InvalidOperationException( "Array of members is null." );
					return null;
				}
				if( index >= _Members.Count ) {
					if( raise ) throw new IndexOutOfRangeException( "Index is too big." );
					return null;
				}
				return _Members[index];
			}

			/// <summary>
			/// Finds the member whose name is given.
			/// </summary>
			/// <param name="name">The name of the member to find.</param>
			/// <param name="raise">True to raise an exception if the member is not found.</param>
			/// <returns>The member whose name is given, or null.</returns>
			public DeepObject FindMember( string name, bool raise = false )
			{
				if( _Members == null ) {
					if( raise ) throw new InvalidOperationException( "Array of members is null." );
					return null;
				}

				name = name.Validated( "Name", invalidChars: TypeHelper.InvalidNameChars );
				var member = _Members.Find( x => string.Compare( name, x.Deep._Name, !_CaseSensitiveNames ) == 0 );
				if( member == null && raise ) throw new KeyNotFoundException( "Member not found: " + name );
				return member;
			}

			/// <summary>
			/// Finds the indexed member using the indexes given.
			/// </summary>
			/// <param name="indexes">The indexes to use to find the member.</param>
			/// <param name="raise">True to raise an exception if the member is not found.</param>
			/// <returns>The member whose indexed were given, or null.</returns>
			public DeepObject FindMember( object[] indexes, bool raise = false )
			{
				if( _Members == null ) {
					if( raise ) throw new InvalidOperationException( "Array of members is null." );
					return null;
				}

				if( indexes == null ) throw new ArgumentNullException( "indexes", "Indexes array cannot be null." );
				if( indexes.Length == 0 ) throw new ArgumentException( "Indexes array cannot be empty." );

				string name = TypeHelper.ToString( indexes, "[]" );
				var member = _Members.Find( x => name == x.Deep._Name && _IsIndex );
				if( member == null && raise ) throw new KeyNotFoundException( "Member not found: " + name );
				return member;
			}

			/// <summary>
			/// Adds the given DeepObject into the collection of members of this instance.
			/// </summary>
			/// <param name="deep">The DeepObject member to add.</param>
			public void AddMember( DeepObject deep )
			{
				if( _Sealed ) throw new InvalidOperationException( "This object is sealed." );
				if( deep == null ) throw new ArgumentNullException( "deep", "Member to add cannot be null." );
				if( deep.Deep._Host != null ) throw new InvalidOperationException( "Member to add is already a member in other host." );
				deep.Deep._Name.Validated( "Deep Member Name" );

				if( _Members == null ) _Members = new List<DeepObject>();

				var member = FindMember( deep.Deep._Name, raise: false );
				if( member != null ) throw new InvalidOperationException( "A member already exists with the same name: " + deep.Deep._Name );

				_Members.Add( deep ); deep.Deep._Host = this._Owner;
			}

			/// <summary>
			/// Creates and adds a new member with the name given.
			/// </summary>
			/// <param name="name">The name of the new member to create and add.</param>
			/// <returns>The newly created member.</returns>
			public DeepObject AddMember( string name )
			{
				if( _Sealed ) throw new InvalidOperationException( "This object is sealed." );
				name = name.Validated( "Name", invalidChars: TypeHelper.InvalidNameChars );

				if( _Members == null ) _Members = new List<DeepObject>();

				var member = FindMember( name, raise: false );
				if( member != null ) throw new InvalidOperationException( "A member already exists with the same name: " + name );

				member = new DeepObject( name, _CaseSensitiveNames );
				member.Deep._Host = this._Owner; _Members.Add( member );
				return member;
			}

			/// <summary>
			/// Creates and add a new indexed member using the indexes given.
			/// </summary>
			/// <param name="indexes">The indexes to use to create and add the new member.</param>
			/// <returns>The newly created and added member.</returns>
			public DeepObject AddMember( object[] indexes )
			{
				if( _Sealed ) throw new InvalidOperationException( "This object is sealed." );
				if( indexes == null ) throw new ArgumentNullException( "Indexes cannot be a null array." );
				if( indexes.Length == 0 ) throw new ArgumentException( "Indexes cannot be an empty array." );

				if( _Members == null ) _Members = new List<DeepObject>();

				var member = FindMember( indexes, raise: false );
				if( member != null ) throw new InvalidOperationException( "A member already exists with the same index: " + TypeHelper.ToString( indexes, "[]" ) );

				string name = TypeHelper.ToString( indexes, "[]" );

				member = new DeepObject(); // to avoid name validation
				member.Deep._CaseSensitiveNames = _CaseSensitiveNames;
				member.Deep._Name = name;
				member.Deep._IsIndex = true;
				member.Deep._Host = this._Owner; _Members.Add( member );
				return member;
			}
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a dynamic multi-level object.
	/// </summary>
	[Serializable]
	public partial class DeepObject : DynamicObject, IDisposable, ICloneable, ISerializable
	{
		DeepCarrier _DeepCarrier = null;

		/// <summary>
		/// Creates a new instance of <see cref="DeepObject"/>.
		/// </summary>
		/// <param name="name">The name of this new instance, or null to indicate it is an orphan object (not being a member of
		/// any other one)</param>
		/// <param name="caseSensitiveNames">Whether the names of its future members (properties) are to be considered case
		/// sensitively or not. This value is propagated through its childs members when they are automatically created).</param>
		public DeepObject( string name = null, bool caseSensitiveNames = true )
		{
			DEBUG.IndentLine( "\n-- DeepObject( Name={0}, Case={1} )", name ?? "null", caseSensitiveNames );

			_DeepCarrier = new DeepCarrier( this );
			_DeepCarrier._CaseSensitiveNames = caseSensitiveNames;
			if( name != null ) _DeepCarrier._Name = name.Validated( "Name", invalidChars: TypeHelper.InvalidNameChars );

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- DeepObject.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _DeepCarrier != null ) { _DeepCarrier.Destroy(); _DeepCarrier = null; }

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- DeepObject.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~DeepObject()
		{
			DEBUG.IndentLine( "\n-- DeepObject~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public string ToString( bool extended )
		{
			return _DeepCarrier == null ? "<DeepObject>" : _DeepCarrier.ToString( extended );
		}
		public override string ToString()
		{
			return ToString( extended: true );
		}

		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( "DeepName", _DeepCarrier._Name );
			info.AddValue( "DeepCaseSensitiveNames", _DeepCarrier._CaseSensitiveNames );
			info.AddValue( "DeepIsIndex", _DeepCarrier._IsIndex );
			info.AddValue( "DeepIsSealed", _DeepCarrier._Sealed );

			info.AddValue( "DeepHasValue", _DeepCarrier._HasValue );
			info.AddValue( "DeepValueType", _DeepCarrier._Value == null ? "VOID" : _DeepCarrier._Value.GetType().AssemblyQualifiedName );
			if( _DeepCarrier._Value != null ) info.AddValue( "DeepValue", _DeepCarrier._Value );

			int count = _DeepCarrier._Members == null ? 0 : _DeepCarrier._Members.Count; info.AddValue( "DeepCount", count );
			if( count != 0 ) {
				for( int i = 0; i < count; i++ ) {
					info.AddValue( "DeepMemberType" + i, _DeepCarrier._Members[i].GetType().AssemblyQualifiedName );
					info.AddValue( "DeepMember" + i, _DeepCarrier._Members[i] );
				}
			}
		}
		protected DeepObject( SerializationInfo info, StreamingContext context )
		{
			_DeepCarrier = new DeepCarrier( this );

			_DeepCarrier._Name = info.GetString( "DeepName" );
			_DeepCarrier._CaseSensitiveNames = info.GetBoolean( "DeepCaseSensitiveNames" );
			_DeepCarrier._IsIndex = info.GetBoolean( "DeepIsIndex" );
			_DeepCarrier._Sealed = info.GetBoolean( "DeepIsSealed" );

			_DeepCarrier._HasValue = info.GetBoolean( "DeepHasValue" );
			string typeName = info.GetString( "DeepValueType" ); if( typeName == "VOID" ) _DeepCarrier._Value = null;
			else {
				Type type = Type.GetType( typeName );
				_DeepCarrier._Value = info.GetValue( "DeepValue", type );
			}

			int count = (int)info.GetValue( "DeepCount", typeof( int ) );
			if( count != 0 ) {
				_DeepCarrier._Members = new List<DeepObject>();

				for( int i = 0; i < count; i++ ) {
					typeName = info.GetString( "DeepMemberType" + i );
					DeepObject obj = (DeepObject)info.GetValue( "DeepMember" + i, Type.GetType( typeName ) );
					_DeepCarrier._Members.Add( obj );
					obj._DeepCarrier._Host = this;
				}
			}
		}

		public DeepObject Clone()
		{
			var cloned = new DeepObject();

			cloned._DeepCarrier._Name = _DeepCarrier._Name;
			cloned._DeepCarrier._CaseSensitiveNames = _DeepCarrier._CaseSensitiveNames;
			cloned._DeepCarrier._IsIndex = _DeepCarrier._IsIndex;
			cloned._DeepCarrier._HasValue = _DeepCarrier._HasValue;
			cloned._DeepCarrier._Value = TypeHelper.TryClone( _DeepCarrier._Value );
			cloned._DeepCarrier._Sealed = _DeepCarrier._Sealed;

			if( _DeepCarrier._Members != null ) {
				cloned._DeepCarrier._Members = new List<DeepObject>();

				foreach( var member in _DeepCarrier._Members ) {
					var temp = member.Clone();
					cloned._DeepCarrier._Members.Add( temp );
					temp._DeepCarrier._Host = cloned;
				}
			}
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Gets the carrier object this instance has, that permits the direct manipulation of its state and structure instead
		/// of using the dynamic mechanism, if needed.
		/// </summary>
		public DeepCarrier Deep
		{
			get { return _DeepCarrier; }
		}

		// --------------------------------------------------
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			List<string> list = new List<string>();
			foreach( DeepObject member in Deep.Members ) list.Add( member.Deep.Name );
			return list;
		}

		public override bool TryGetMember( GetMemberBinder binder, out object result )
		{
			DEBUG.IndentLine( "\n-- DeepObject.TryGetMember() Name={0} This={1}", binder.Name, this );

			var member = Deep.FindMember( binder.Name, raise: false );
			if( member == null ) result = Deep.AddMember( binder.Name );
			else result = member.Deep.HasValue ? member.Deep.Value : member;

			DEBUG.Unindent();
			return true;
		}
		public override bool TrySetMember( SetMemberBinder binder, object value )
		{
			DEBUG.IndentLine( "\n--DeepObject.TrySetMember() Name={0} Value={1} This={2}",
				binder.Name,
				TypeHelper.ToString( value ),
				this );

			var member = Deep.FindMember( binder.Name, raise: false );
			if( member == null ) member = Deep.AddMember( binder.Name );
			member.Deep.Value = value;

			DEBUG.Unindent();
			return true;
		}
		public override bool TryGetIndex( GetIndexBinder binder, object[] indexes, out object result )
		{
			string name = TypeHelper.ToString( indexes, "[]" );

			DEBUG.IndentLine( "\n-- DeepObject.TryGetIndex() Index={0} This={1}", name, this );

			var member = Deep.FindMember( indexes, raise: false );
			if( member == null ) result = Deep.AddMember( indexes );
			else result = member.Deep.HasValue ? member.Deep.Value : member;

			DEBUG.Unindent();
			return true;
		}
		public override bool TrySetIndex( SetIndexBinder binder, object[] indexes, object value )
		{
			string name = TypeHelper.ToString( indexes, "[]" );

			DEBUG.IndentLine( "\n-- DeepObject.TrySetIndex() Index={0} Value={1} This={2}", name, TypeHelper.ToString( value ), this );

			var member = Deep.FindMember( indexes, raise: false );
			if( member == null ) member = Deep.AddMember( indexes );
			member.Deep.Value = value;

			DEBUG.Unindent();
			return true;
		}
		public override bool TryConvert( ConvertBinder binder, out object result )
		{
			DEBUG.IndentLine( "\n-- DeepObject.TryConvert() Type={0} This={1}", binder.ReturnType, this );

			result = TypeHelper.ConvertTo( Deep.HasValue ? Deep.Value : this, binder.ReturnType );

			DEBUG.Unindent();
			return true;
		}
	}
}
// ========================================================
