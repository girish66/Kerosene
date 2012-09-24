// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Maps
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::System.Reflection;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	public static partial class KMaps
	{
		internal const string MapPrefix = "KMap<T>::";
		internal static string GetMapKey( Type type )
		{
			string key = MapPrefix + type.FullName;
			return key;
		}

		// --------------------------------------------------
		/// <summary>
		/// Returns a list with the types registered with the Maps system in the given link.
		/// </summary>
		/// <param name="link">The link instance where to find the types registered with.</param>
		/// <param name="onlyValidated">Whether to return only the types whose maps are validated, or all maps.</param>
		/// <returns>A list with the types whose maps are registered in this link.</returns>
		public static List<Type> GetMappedTypes( this IKLink link, bool onlyValidated = true )
		{
			if( link == null ) throw new ArgumentNullException( "link", "IKLink cannot be null." );

			List<Type> list = new List<Type>(); foreach( var kvp in link.ExtendedInfo ) {
				if( !kvp.Key.StartsWith( MapPrefix ) ) continue;
				IKMap map = kvp.Value as IKMap; if( map == null ) continue;
				if( map.IsValidated || !onlyValidated ) list.Add( map.EntityType );
			}
			return list;
		}

		/// <summary>
		/// Returns the <see cref="IKMap"/> instance associated with the given type in this link.
		/// </summary>
		/// <param name="link">This link.</param>
		/// <param name="entityType">The type whose map is to be found.</param>
		/// <param name="raise">True to raise an exception if the map is not found.</param>
		/// <param name="forceValidation">True to force the validation of the map if it is not validated yet.</param>
		/// <returns>The map associated with the given type, or null if it is not found.</returns>
		public static IKMap GetMap( this IKLink link, Type entityType, bool raise = true, bool forceValidation = true )
		{
			if( link == null ) throw new ArgumentNullException( "link", "IKLink cannot be null." );
			if( entityType == null ) throw new ArgumentNullException( "entityType", "Entity Type cannot be null." );

			string key = GetMapKey( entityType );
			object obj = null;

			if( link.ExtendedInfo.TryGetValue( key, out obj ) ) {
				IKMap map = obj as IKMap; if( map != null ) {
					if( !map.IsValidated && forceValidation ) map.Validate();
					return map;
				}
			}
			if( raise ) throw new KeyNotFoundException( "Map not found for type: " + entityType );
			return null;
		}
		
		/// <summary>
		/// Removes from this link the map whose type is given.
		/// </summary>
		/// <param name="link">This link.</param>
		/// <param name="entityType">The type whose map is to be removed.</param>
		/// <returns>True if the map is removed, and disposed, false otherwise.</returns>
		public static bool RemoveMap( this IKLink link, Type entityType )
		{
			IKMap map = GetMap( link, entityType, raise: false, forceValidation: false );
			if( map != null ) {
				map.Dispose(); // Dispose() takes care of unregistering...
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes and disposes all maps registered with this link object.
		/// </summary>
		/// <param name="link">This link.</param>
		public static void ClearMaps( this IKLink link )
		{
			if( link == null ) throw new ArgumentNullException( "link", "IKLink cannot be null." );

			List<IKMap> list = new List<IKMap>();
			foreach( var kvp in link.ExtendedInfo ) if( kvp.Key.StartsWith( MapPrefix ) ) list.Add( (IKMap)kvp.Value );
			foreach( var map in list ) map.Dispose(); // Dispose() takes care of unregistering...
			list.Clear();
			list = null;
		}

		// --------------------------------------------------
		/// <summary>
		/// Gets the map associated with the business type given, and registered into this link object.
		/// </summary>
		/// <typeparam name="T">The type of the map to return.</typeparam>
		/// <param name="link">This link object.</param>
		/// <param name="raise">True to raise an exception if the map is not found.</param>
		/// <param name="forceValidation">To force the validation of the map if it is not validated yet.</param>
		/// <returns>The map in this link associated with the given type.</returns>
		public static KMap<T> GetMap<T>( this IKLink link, bool raise = true, bool forceValidation = true ) where T : class
		{
			KMap<T> map = (KMap<T>)GetMap( link, typeof( T ), raise: raise, forceValidation: forceValidation );
			return map;
		}

		/// <summary>
		/// Removes from this link the map associated with the business type given, and disposes it.
		/// </summary>
		/// <typeparam name="T">The type of the map to remove.</typeparam>
		/// <param name="link">This link object.</param>
		/// <returns>True if the map has been removed and disposed, false otherwise.</returns>
		public static bool RemoveMap<T>( this IKLink link ) where T : class
		{
			return RemoveMap( link, typeof( T ) );
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a map, an association between a given business type with at least one primary table in the database.
	/// </summary>
	public interface IKMap : IDisposable, IKCloneable
	{
		/// <summary>
		/// Gets the business type this map refers to.
		/// </summary>
		Type EntityType { get; }
		
		/// <summary>
		/// Gets the link this map is registered with.
		/// </summary>
		IKLink Link { get; }
		
		/// <summary>
		/// Gets the primary table where to find the columns to map with the properties and fields of the business type.
		/// </summary>
		string Table { get; }
		
		/// <summary>
		/// Gets the dynamic lambda expression that contains the filter to apply when querying the primary table.
		/// </summary>
		Func<dynamic, object> WhereFilter { get; }

		/// <summary>
		/// Gets whether this map has been validated or not.
		/// </summary>
		bool IsValidated { get; }
		
		/// <summary>
		/// Validates this map and sets its internal state.
		/// </summary>
		void Validate();

		/// <summary>
		/// Gets a list with the columns in the primary table that have been discarded because no analogous property or field
		/// has been found automatically in the business type.
		/// </summary>
		IEnumerable<string> DiscardedColumns { get; }
		
		/// <summary>
		/// Gets the schema of the records to fetch from the primary table on the database.
		/// </summary>
		KSchema Schema { get; }
		
		/// <summary>
		/// Gets a partial schema, containing either the primary key columns or the unique columns, to be used to uniquely
		/// locate the records associated with the business type in the primary table.
		/// </summary>
		KSchema SchemaId { get; }

		// --------------------------------------------------
		/// <summary>
		/// Creates and return a mapped query command.
		/// </summary>
		/// <param name="alias">The alias to use with the primary table, if needed.</param>
		/// <returns>The new created mapped query command.</returns>
		IKMapEnumerable Query( Func<dynamic, object> alias = null );
		
		/// <summary>
		/// Creates and return a mapped insert command.
		/// </summary>
		/// <param name="obj">The object to be inserted when this command is executed.</param>
		/// <returns>The new created mapped insert command.</returns>
		IKMapExecutable Insert( object obj );

		/// <summary>
		/// Creates and return a mapped delete command.
		/// </summary>
		/// <param name="obj">The object to be deleted when this command is executed.</param>
		/// <returns>The new created mapped delete command.</returns>
		IKMapExecutable Delete( object obj );

		/// <summary>
		/// Creates and return a mapped update command.
		/// </summary>
		/// <param name="obj">The object to be updated when this command is executed.</param>
		/// <returns>The new created mapped update command.</returns>
		IKMapExecutable Update( object obj );

		/// <summary>
		/// Refreshes the given business instance obtaining its most current contents from the database.
		/// </summary>
		/// <param name="obj">The object to be refreshed.</param>
		/// <returns>The object refreshed, or null if it is not found in the database.</returns>
		object Refresh( object obj );
		
		/// <summary>
		/// Tries to find in the internal cache the business instance identified by the record given. If it is not found in
		/// the cache, then a query is executed against the database. In both cases, only the first match is returned, or
		/// null if the object is not found.
		/// </summary>
		/// <param name="find">The record containing the columns to locate the business instance.</param>
		/// <returns>The object found, or null.</returns>
		object Find( KRecord find );
		
		/// <summary>
		/// Tries to find in the internal cache the business instance identified by the columns and values specified by the
		/// dynamic lambda expressions given. If it is not found in the cache, then a query is executed against the database.
		/// In both cases, only the first match is returned, or null if the object is not found.
		/// </summary>
		/// <param name="specs">The dynamic lambda expressions containing the columns and values to use to locate the business
		/// instance.</param>
		/// <returns>The object found, or null.</returns>
		object Find( params Func<dynamic, object>[] specs );

		// --------------------------------------------------
		/// <summary>
		/// The delegate to invoke to create an empty instance of the business type when needed.
		/// </summary>
		Func<object> CreateInstance { get; }
		
		/// <summary>
		/// The delegate to invoke to clear the instance of the business type given as its argument.
		/// </summary>
		Action<object> ClearInstance { get; }
		
		/// <summary>
		/// The delegate to invoke to extract from the business type the values needed to set the contents to the record
		/// given as its argument. Note that the set of columns in this record may be different in each call.
		/// </summary>
		Action<object, KRecord> WriteRecord { get; }
		
		/// <summary>
		/// The delegate to invoke to load the contents of the business type from the columns on the record given as its
		/// argument. Note that the set of columns in this record may be different in each call.
		/// <para>The best practice is to load only those contents that are directly related to the columns in the record;
		/// the OnRefresh delegate is used to load other dependencies as needed.</para>
		/// </summary>
		Action<KRecord, object> LoadRecord { get; }

		/// <summary>
		/// The delegate to invoke to refresh the dependencies of this business object when needed. Typically returns the
		/// object that has been refreshed.
		/// </summary>
		Func<object, object> OnRefresh { get; }
		
		/// <summary>
		/// The delegate to invoke to actually perform the insertion of the new business instance in the database. Typically
		/// returns the object that has been inserted.
		/// </summary>
		Func<object, object> OnInsert { get; }

		/// <summary>
		/// The delegate to invoke to actually perform the deletion of the business instance from the database. Typically
		/// returns null meaning that the object has been deleted from the database, or the original object in case of any
		/// errors.
		/// </summary>
		Func<object, object> OnDelete { get; }

		/// <summary>
		/// The delegate to invoke to actually perform the update of new business instance in the database. Typically returns
		/// the object that has been updated.
		/// </summary>
		Func<object, object> OnUpdate { get; }
	}

	// =====================================================
	/// <summary>
	/// Represents a map for the given business type. The only restriction is that this type should be a class. If it has
	/// a parameterless constructor, it is used by default. Otherwise the OnCreate delegate shall be set with the appropriate
	/// delegate to create an empty instance.
	/// </summary>
	/// <typeparam name="T">The business type this map is created for.</typeparam>
	public partial class KMap<T> : IKMap where T : class
	{
		string _Table = null;
		Func<dynamic, object> _WhereFilter = null;

		List<string> _Managed = new List<string>();
		List<string> _UnManaged = new List<string>();
		bool _SelectAll = true;

		Func<T, T> _OnRefresh = null;
		Func<T, T> _OnInsert = null;
		Func<T, T> _OnDelete = null;
		Func<T, T> _OnUpdate = null;

		Func<T> _CreateInstance = null;
		Action<T> _ClearInstance = null;
		Action<T, KRecord> _WriteRecord = null;
		Action<KRecord, T> _LoadRecord = null;

		/// <summary>
		/// Clones the contents of this map registering the new cloned one with the link given.
		/// </summary>
		/// <param name="link">The link where to register the new created clone.</param>
		/// <returns>The new clone created.</returns>
		public KMap<T> Clone( IKLink link )
		{
			var cloned = new KMap<T>( link, x => _Table );

			cloned._WhereFilter = _WhereFilter;
			cloned._Managed.AddRange( _Managed );
			cloned._UnManaged.AddRange( _UnManaged );
			cloned._SelectAll = _SelectAll;

			cloned._OnRefresh = _OnRefresh;
			cloned._OnInsert = _OnInsert;
			cloned._OnDelete = _OnDelete;
			cloned._OnUpdate = _OnUpdate;

			cloned._CreateInstance = _CreateInstance;
			cloned._ClearInstance = _ClearInstance;
			cloned._WriteRecord = _WriteRecord;
			cloned._LoadRecord = _LoadRecord;

			return cloned;
		}
		object IKCloneable.Clone( IKLink link )
		{
			return this.Clone( link );
		}

		// --------------------------------------------------
		/// <summary>
		/// Creates a new map for the business type given.
		/// </summary>
		/// <param name="link">The link where this map is going to be registered at.</param>
		/// <param name="table">The dynamic lambda expression specifying the primary table in the database to associate with
		/// this business type.</param>
		public KMap( IKLink link, Func<dynamic, object> table )
		{
			// Validating the collector has been started...
			if( KMetaEntity._Timer == null ) KMaps.StartCollector();

			// Now let's process with the constructor...
			DEBUG.IndentLine( "\n-- KMap<{0}>( Link={1}, Table=?? )", typeof( T ).Name, link == null ? "<null>" : link.ToString() );

			if( ( _Link = link ) == null ) throw new ArgumentNullException( "link", "IKLink cannot be null." );
			string key = KMaps.GetMapKey( EntityType );
			if( link.ExtendedInfo.ContainsKey( key ) ) throw new InvalidOperationException( "Map for type: " + EntityType.Name + " is already registered in link: " + link );
			link.ExtendedInfo.Add( key, this );

			if( table == null ) throw new ArgumentNullException( "table", "Table specification cannot be null." );
			var parser = DynamicParser.Parse( table );
			if( parser.Result == null ) throw new ArgumentException( "Table specification cannot resolve to null." );
			DEBUG.WriteLine( string.Format( "\n-- Table: {0}", parser.Result ) );

			if( parser.Result is string ) _Table = (string)parser.Result;
			else if( parser.Result is DynamicNode.GetMember ) {
				_Table = ( (DynamicNode.GetMember)parser.Result ).ToString();
				_Table = KParser.ExtractTag( _Table );
			}
			else throw new ArgumentException( "Table specification has not a valid format: " + parser.Result.ToString() );
			_Table = _Table.Validated( "Table", invalidChars: TypeHelper.InvalidNameChars );

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KMap<{0}>.Dispose( Disposing={1} ) - This={2}", typeof( T ).Name, disposing, this );

			// Unregistering this map...
			if( _Link != null ) {
				if( _Link.ExtendedInfo != null ) _Link.ExtendedInfo.Remove( KMaps.GetMapKey( EntityType ) );
				_Link = null;
				_IsValidated = false;
			}

			// Cleaning its managed entities...
			KMetaEntity.ClearMap( this );

			// And cleaning the internal objects...
			_WhereFilter = null;
			_Schema = null; // Do not dispose, it might be in use elsewhere
			_SchemaId = null; // Do not dispose, it might be in use elsewhere

			_CreateInstance = null;
			_ClearInstance = null;
			_WriteRecord = null;
			_LoadRecord = null;

			_OnRefresh = null;
			_OnInsert = null;
			_OnDelete = null;
			_OnUpdate = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KMap<{0}>.Dispose() - This={1}", typeof( T ).Name, this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KMap()
		{
			DEBUG.IndentLine( "\n-- ~KMap<{0}>() - This={1}", typeof( T ).Name, this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "{0} => [{1}].{2}",
				EntityType.Name,
				_Link == null ? "<null>" : _Link.ToString(),
				_Table ?? "null" );
		}

		/// <summary>
		/// Gets the business type this map refers to.
		/// </summary>
		public Type EntityType
		{
			get { return typeof( T ); }
		}

		/// <summary>
		/// Gets the link this map is registered with.
		/// </summary>
		public IKLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// Gets the primary table where to find the columns to map with the properties and fields of the business type.
		/// </summary>
		public string Table
		{
			get { return _Table; }
		}

		/// <summary>
		/// Gets or sets the the dynamic lambda expression containing the filter to apply when querying the primary table. 
		/// <para>If the map is already validated, the setter will throw an exception.</para>
		/// <para>If the map is not validated yet, the getter will throw an exception.</para>
		/// </summary>
		public Func<dynamic, object> WhereFilter
		{
			set
			{
				if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
				_WhereFilter = value;
			}
			get
			{
				if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
				return _WhereFilter;
			}
		}

		// --------------------------------------------------
		IKLink _Link = null;
		KSchema _Schema = null;
		KSchema _SchemaId = null;

		bool _IsValidated = false;
		ConstructorInfo _Constructor = null;
		List<MemberInfo> _Members = new List<MemberInfo>();
		List<string> _DiscardedColumns = new List<string>();

		/// <summary>
		/// Gets the schema of the records to fetch from the primary table on the database.
		/// <para>If the map is not validated yet, the getter will throw an exception.</para>
		/// </summary>
		public KSchema Schema
		{
			get
			{
				if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
				return _Schema;
			}
		}

		/// <summary>
		/// Gets a partial schema, containing either the primary key columns or the unique columns, to be used to uniquely
		/// locate the records associated with the business type in the primary table.
		/// <para>If the map is not validated yet, the getter will throw an exception.</para>
		/// </summary>
		public KSchema SchemaId
		{
			get
			{
				if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
				return _SchemaId;
			}
		}

		/// <summary>
		/// Gets whether this map has been validated or not.
		/// </summary>
		public bool IsValidated
		{
			get
			{
				if( _Link == null ) throw new ObjectDisposedException( "KMap<" + EntityType.Name + ">" );
				return _IsValidated;
			}
		}

		/// <summary>
		/// Gets a list with the columns in the primary table that have been discarded because no analogous property or field
		/// has been found automatically in the business type.
		/// <para>If the map is not validated yet, the getter will throw an exception.</para>
		/// </summary>
		public IEnumerable<string> DiscardedColumns
		{
			get
			{
				if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
				return _DiscardedColumns;
			}
		}

		// --------------------------------------------------
		void AddColumnSpec( string spec, bool managed )
		{
			if( spec == null ) throw new ArgumentException( "Column specification cannot be null." );
			spec = spec.Validated( "Column", invalidChars: TypeHelper.InvalidNameChars );

			if( _Managed.Find( x => string.Compare( x, spec, !_Link.DbCaseSensitiveNames ) == 0 ) != null )
				throw new ArgumentException( "Column '" + spec + "' is already registered in managed columns." );

			if( _UnManaged.Find( x => string.Compare( x, spec, !_Link.DbCaseSensitiveNames ) == 0 ) != null )
				throw new ArgumentException( "Column '" + spec + "' is already registered in un-managed columns." );

			if( managed ) _Managed.Add( spec ); else _UnManaged.Add( spec );
		}
		void AddColumnSpec( Func<dynamic, object> spec, bool managed )
		{
			if( spec == null ) throw new ArgumentException( "Column specification cannot be null." );

			var parser = DynamicParser.Parse( spec );
			if( parser.Result == null ) throw new ArgumentException( "Column specification cannot resolve to null." );

			string name = null;
			if( parser.Result is string ) name = (string)parser.Result;
			else if( parser.Result is DynamicNode.GetMember ) {
				name = ( (DynamicNode.GetMember)parser.Result ).ToString();
				name = KParser.ExtractTag( name );
				name = name.Validated( "Name", invalidChars: TypeHelper.InvalidNameChars );
			}
			else throw new ArgumentException( "Column specification has not a valid format: " + parser.Result.ToString() );

			AddColumnSpec( name, managed );
		}

		/// <summary>
		/// Permits to manually specify what columns in the primary table in the database are to be fetched and automatically
		/// managed by this map. Their names typically match the names of a given property or field in the business type, or
		/// it will be added to the list of discarded columns.
		/// </summary>
		/// <param name="specs">A list of string containing the names of the columns in the database to be managed
		/// automatically.</param>
		public void ManagedColumns( params string[] specs )
		{
			if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
			if( specs == null ) throw new ArgumentNullException( "specs", "Columns specifications cannot be null." );
			foreach( var spec in specs ) AddColumnSpec( spec, managed: true );
		}

		/// <summary>
		/// Permits to manually specify what columns in the primary table in the database are to be fetched and automatically
		/// managed by this map. Their names typically match the names of a given property or field in the business type, or
		/// it will be added to the list of discarded columns.
		/// </summary>
		/// <param name="specs">A list with the dynamic lambda expressions specifying the names of the columns in the database
		/// to be managed automatically.</param>
		public void ManagedColumns( params Func<dynamic, object>[] specs )
		{
			if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
			if( specs == null ) throw new ArgumentNullException( "specs", "Columns specifications cannot be null." );
			foreach( var spec in specs ) AddColumnSpec( spec, managed: true );
		}

		/// <summary>
		/// Permits to specify whether we want to validate all posible columns or only those that have been specified in
		/// either managed columns or unmanaged.
		/// </summary>
		/// <param name="all">True to validate all possible columns, false otherwise.</param>
		public void SelectAll( bool all )
		{
			if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
			_SelectAll = all;
		}

		/// <summary>
		/// Permits to specify what columns are to be retrieved from the primary table, so making them available for advanced
		/// scenarios in the schema and the records obtained, but not automatically managed by the maps system.
		/// </summary>
		/// <param name="specs">A list with the names of the unmanaged columns.</param>
		public void UnManagedColumns( params string[] specs )
		{
			if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
			if( specs == null ) throw new ArgumentNullException( "specs", "Columns specifications cannot be null." );
			foreach( var spec in specs ) AddColumnSpec( spec, managed: false );
		}

		/// <summary>
		/// Permits to specify what columns are to be retrieved from the primary table, so making them available for advanced
		/// scenarios in the schema and the records obtained, but not automatically managed by the maps system.
		/// </summary>
		/// <param name="specs">A list with the dynamic lambda expressions specifying the names of the unmanaged columns.</param>
		public void UnManagedColumns( params Func<dynamic, object>[] specs )
		{
			if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
			if( specs == null ) throw new ArgumentNullException( "specs", "Columns specifications cannot be null." );
			foreach( var spec in specs ) AddColumnSpec( spec, managed: false );
		}

		/// <summary>
		/// Validates this map by verifying the schema against the database.
		/// </summary>
		public void Validate()
		{
			if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
			if( _Link == null ) throw new InvalidOperationException( "This map has been disposed." );

			// Finding the default constructor, if any...
			_Constructor = EntityType.GetConstructor( Type.EmptyTypes );

			// Obtaining the schema from the database through a first query...
			var cmd = _Link.From( _Table );
			cmd.Top( 1 );

			if( _Managed.Count != 0 ) {
				foreach( var str in _Managed ) cmd.Select( str ); // If any columns specified, avoiding a "SELECT *"...
				foreach( var str in _UnManaged ) cmd.Select( str ); // And also the unmanaged ones...
			}
			else if( !_SelectAll ) { // It may happen that we only want un-managed columns...
				foreach( var str in _UnManaged ) cmd.Select( str );
			}
			if( _WhereFilter != null ) cmd.Where( _WhereFilter ); // To validate the WHERE filter

			var reader = cmd.GetEnumerator();
			reader.MoveNext(); _Schema = reader.Schema; reader.Dispose();
			if( _Schema.Count == 0 ) throw new InvalidOperationException( "Schema is empty." );

			// Loading the managed members and discarded columns...
			foreach( var col in _Schema ) {
				// Unmanaged columns are not removed, but not added to the reflection cache...
				if( _UnManaged.Find( x => string.Compare( x, col.ColumnName, !_Link.DbCaseSensitiveNames ) == 0 ) != null )
					continue;

				// If no member found, do not add to the reflection cached but to the discarded columns...
				MemberInfo mi = TypeHelper.GetElementInfo( EntityType, col.ColumnName, raise: false );
				if( mi == null ) { _DiscardedColumns.Add( col.ColumnName ); continue; }

				// Because CaseSensitive, we need to intercept the case of multiple additions...
				if( _Members.Find( x => x.Name == col.ColumnName ) != null )
					throw new ArgumentException( "Member '" + mi.Name + "' is already managed." );

				// Adding to the reflection cache...
				_Members.Add( mi );
			}

			// Removing the discarded columns...
			_Schema.Sealed = false; foreach( var str in _DiscardedColumns ) {
				var col = _Schema.Find( null, str, anyTable: true, raise: false );
				_Schema.Remove( col );
			}

			// Validating the ID columns...
			_SchemaId = new KSchema( _Link.DbCaseSensitiveNames );
			foreach( var col in _Schema.PrimaryColumns ) _SchemaId.Add( col.Clone() );
			if( _SchemaId.Count == 0 ) foreach( var col in _Schema.UniqueColumns ) _SchemaId.Add( col.Clone() );
			if( _SchemaId.Count == 0 ) throw new InvalidOperationException( "Schema does not contain neither Primary Key columns nor Unique columns:" + _Schema );

			// Sealing the schemas...
			_Schema.Sealed = true;
			_SchemaId.Sealed = true;

			// And finally, setting the validation status...
			_IsValidated = true;
		}

		// --------------------------------------------------
		/// <summary>
		/// The default method invoked to create an instance of the business type if the CreateInstance delegate has not
		/// been set.
		/// </summary>
		/// <returns>A new instance of the business type.</returns>
		public T BaseCreateInstance()
		{
			if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );

			if( _Constructor == null ) throw new InvalidOperationException( "No parameterless constructor found for type: " + EntityType.Name );
			T obj = (T)_Constructor.Invoke( null );
			return obj;
		}

		/// <summary>
		/// Gets or sets the delegate to invoke to create an instance of the business type. If not explicitly set it will
		/// invoke the default BaseCreateInstance() method.
		/// </summary>
		public Func<T> CreateInstance
		{
			set
			{
				if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
				_CreateInstance = value;
			}
			get
			{
				Func<T> r = () => {
					if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );

					T obj = _CreateInstance != null ? _CreateInstance() : BaseCreateInstance();
					return obj;
				};
				return r;
			}
		}
		Func<object> IKMap.CreateInstance
		{
			get { Func<object> r = () => { return this.CreateInstance(); }; return r; }
		}

		// --------------------------------------------------
		/// <summary>
		/// The default method invoked to clear the instance of the business type given if the ClearInstance delegate has not
		/// been set.
		/// </summary>
		/// <param name="obj">The business instance to clear.</param>
		public void BaseClearInstance( T obj )
		{
			if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
			if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

			if( _Members.Count != 0 ) {
				foreach( var member in _Members ) {
					Type type = TypeHelper.GetElementType( member );
					object value = type.IsValueType ? Activator.CreateInstance( type ) : null;
					TypeHelper.SetElementValue( member, obj, value );
				}
			}
		}

		/// <summary>
		/// Gets or sets the delegate to invoke to clear the instance of the business type given. If not explicitly set it
		/// will invoke the default BaseClearInstance() method.
		/// </summary>
		public Action<T> ClearInstance
		{
			set
			{
				if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
				_ClearInstance = value;
			}
			get
			{
				Action<T> r = obj => {
					if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
					if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

					if( _ClearInstance != null ) _ClearInstance( obj ); else BaseClearInstance( obj );
				};
				return r;
			}
		}
		Action<object> IKMap.ClearInstance
		{
			get { Action<object> r = obj => { this.ClearInstance( (T)obj ); }; return r; }
		}

		// --------------------------------------------------
		/// <summary>
		/// The default method invoked to write on the record given its columns from the contents of the business instance
		/// given, if the WriteRecord delegate has not been set. It uses reflection to cache the appropriate properties or
		/// fields whose names match the ones of the columns.
		/// </summary>
		/// <param name="obj">The business instance to obtain its contents from.</param>
		/// <param name="record">The record whose columns are to be set with the business instance contents.</param>
		public void BaseWriteRecord( T obj, KRecord record )
		{
			if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
			if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );
			if( record == null ) throw new ArgumentNullException( "record", "Record cannot be null." );

			if( _Members.Count != 0 ) {
				for( int i = 0; i < record.Count; i++ ) {
					var col = record.Schema[i];
					var mi = _Members.Find( x => string.Compare( x.Name, col.ColumnName, !_Link.DbCaseSensitiveNames ) == 0 );
					if( mi == null ) continue; // No managed member found for this column 

					record[i] = TypeHelper.GetElementValue( mi, obj );
				}
			}
		}

		/// <summary>
		/// Gets or sets the delegate to invoke to write the contents on the record from the business type. If not explicitly
		/// set it will invoke the default BaseWriteRecord() method.
		/// </summary>
		public Action<T, KRecord> WriteRecord
		{
			set
			{
				if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
				_WriteRecord = value;
			}
			get
			{
				Action<T, KRecord> r = ( obj, record ) => {
					if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
					if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );
					if( record == null ) throw new ArgumentNullException( "record", "Record cannot be null." );

					if( _WriteRecord != null ) _WriteRecord( obj, record ); else BaseWriteRecord( obj, record );
				};
				return r;
			}
		}
		Action<object, KRecord> IKMap.WriteRecord
		{
			get { Action<object, KRecord> r = ( obj, record ) => { this.WriteRecord( (T)obj, record ); }; return r; }
		}

		// --------------------------------------------------
		/// <summary>
		/// The default method invoked to load from the record given the contents og the business instance, if the LoadRecord
		/// delegate has not been set. It uses reflection to cache the appropriate properties or fields whose names match the
		/// ones of the columns.
		/// </summary>
		/// <param name="record">The record where to find the contents to set into the business instance.</param>
		/// <param name="obj">The business instance whose contents are to be set.</param>
		public void BaseLoadRecord( KRecord record, T obj )
		{
			if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
			if( record == null ) throw new ArgumentNullException( "record", "Record cannot be null." );
			if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

			if( _Members.Count != 0 ) {
				for( int i = 0; i < record.Count; i++ ) {
					var col = record.Schema[i];
					var mi = _Members.Find( x => string.Compare( x.Name, col.ColumnName, !_Link.DbCaseSensitiveNames ) == 0 );
					if( mi == null ) continue; // No managed member found for this column 

					Type memberType = TypeHelper.GetElementType( mi );
					object converted = TypeHelper.ConvertTo( record[i], memberType );
					TypeHelper.SetElementValue( mi, obj, converted );
				}
			}
		}

		/// <summary>
		/// Gets or sets the delegate to invoke to load the contents of the business instance from the record given. If not
		/// explictly set, it will invoke the default BaseLoadRecord() method.
		/// </summary>
		public Action<KRecord, T> LoadRecord
		{
			set
			{
				if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
				_LoadRecord = value;
			}
			get
			{
				Action<KRecord, T> r = ( record, obj ) => {
					if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
					if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );
					if( record == null ) throw new ArgumentNullException( "record", "Record cannot be null." );

					if( _LoadRecord != null ) _LoadRecord( record, obj ); else BaseLoadRecord( record, obj );
				};
				return r;
			}
		}
		Action<KRecord, object> IKMap.LoadRecord
		{
			get { Action<KRecord, object> r = ( record, obj ) => { this.LoadRecord( record, (T)obj ); }; return r; }
		}

		// --------------------------------------------------
		/// <summary>
		/// The default method invoked to refresh the dependencies of the business instance given. This default implementation
		/// merely returns the instance given as its argument.
		/// </summary>
		/// <param name="obj">The object to refresh.</param>
		/// <returns>The object refreshed.</returns>
		public T BaseOnRefresh( T obj )
		{
			if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );
			return obj;
		}

		/// <summary>
		/// Gets or sets the delegate to invoke to refresh the contents of the business instance. If not explictly set, it
		/// will invoke the default BaseLoadRecord() method.
		/// </summary>
		public Func<T, T> OnRefresh
		{
			set
			{
				if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
				_OnRefresh = value;
			}
			get
			{
				Func<T, T> r = obj => {
					if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
					if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

					T item = _OnRefresh != null ? _OnRefresh( obj ) : BaseOnRefresh( obj );
					return item;
				};
				return r;
			}
		}
		Func<object, object> IKMap.OnRefresh
		{
			get { Func<object, object> r = obj => { return this.OnRefresh( (T)obj ); }; return r; }
		}
	}
}
// ========================================================
