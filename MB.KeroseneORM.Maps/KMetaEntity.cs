// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Maps
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::System.ComponentModel;
	using global::System.Timers;
	using global::MB.Tools;

	// =====================================================
	public static partial class KMaps
	{
		/// <summary>
		/// Enumerates the states of a <see cref="KMetaEntity"/>.
		/// </summary>
		public enum MetaState
		{
			/// <summary>
			/// The meta entity has been just created but is empty and potentially not registered with any host or any map.
			/// </summary>
			Empty,
			
			/// <summary>
			/// The entity and its associated host have been refreshed and are ready to be used.
			/// </summary>
			Ready,
			
			/// <summary>
			/// The associated host has been just inserted, and is waiting to be refreshed.
			/// </summary>
			Inserted,

			/// <summary>
			/// The associated host has been just updated, and is waiting to be refreshed.
			/// </summary>
			Updated,

			/// <summary>
			/// The associated host has been just deleted, and is waiting to be removed from the list of active entities.
			/// </summary>
			Deleted,

			/// <summary>
			/// The associated host has been just partially retrieved from the database, and is waiting for its extended
			/// information, as dependencies, to be refreshed.
			/// </summary>
			Dirty
		}

		/// <summary>
		/// Returns the <see cref="KMetaEntity"/> instance the host object is associated with, or null if this host object is
		/// not a registered entity.
		/// </summary>
		/// <param name="host">The host object.</param>
		/// <returns>The meta entity associated with the host, or null.</returns>
		public static KMetaEntity GetMetaEntity( object host )
		{
			return host == null ? null : KMetaEntity.Get( host, createMeta: false );
		}

		// --------------------------------------------------
		/// <summary>
		/// Establishes the frequency at which the entity collector will be called.
		/// </summary>
		public const int DefaultCollectorInterval = 15000;

		/// <summary>
		/// Establishes a lower limit on the frequency of the entity collector.
		/// </summary>
		public const int MinimalCollectorInterval = 500;

		/// <summary>
		/// Starts or resets the entity collector using the arguments given. 
		/// </summary>
		/// <param name="milliseconds">The milliseconds to wait to initiate a new collection.</param>
		/// <param name="invokeGC">True to invoke the platform's Garbage Collector in each entity collection.</param>
		public static void StartCollector( int milliseconds, bool invokeGC = false )
		{
			milliseconds = milliseconds >= MinimalCollectorInterval ? milliseconds : MinimalCollectorInterval;
			KMetaEntity._InvokeGC = invokeGC;

			if( KMetaEntity._Timer == null ) {
				KMetaEntity._Timer = new System.Timers.Timer();
				KMetaEntity._Timer.Elapsed += new ElapsedEventHandler( KMetaEntity.TimerCollect );
			}
			else KMetaEntity._Timer.Enabled = false;

			KMetaEntity._Timer.Interval = milliseconds;
			KMetaEntity._Timer.Enabled = true;
		}

		/// <summary>
		/// Starts the entity collector using either the default values, if it was not started before, or with the arguments
		/// it had when it was stopped.
		/// </summary>
		public static void StartCollector()
		{
			if( KMetaEntity._Timer != null ) {
				if( !KMetaEntity._Timer.Enabled )
					KMetaEntity._Timer.Enabled = true; // To re-use the settings of the already existing timer
			}
			else StartCollector( DefaultCollectorInterval, invokeGC: false );
		}
		
		/// <summary>
		/// Stops and potentially destroys the internal state of the entity collector.
		/// </summary>
		/// <param name="dispose">True to destroy the internal state of the entity collector, so that the next call to the
		/// StartCollector() method will behave as if it is the first time it is invoked, instead of using the values the
		/// former collector had.</param>
		public static void StopCollector( bool dispose = false )
		{
			if( KMetaEntity._Timer != null ) {
				KMetaEntity._Timer.Stop(); // By default, let it keep the settings it has...
				if( dispose ) { KMetaEntity._Timer.Dispose(); KMetaEntity._Timer = null; } // Or dispose it if requested...
			}
		}

		// --------------------------------------------------
		static object _TransientWitness = null;
		static string _TransientReason = null;

		/// <summary>
		/// Gets whether the Maps system is in transient mode or not.
		/// </summary>
		public static bool OnTransientMode
		{
			get { return _TransientWitness == null ? false : true; }
		}

		/// <summary>
		/// An object that can be used to force an exit from transient mode.
		/// <para>It is considered a bad practice to use, it should be used as a last resort mechanism.</para>
		/// 
		/// </summary>
		public static object TransientMasterWitness = new object();

		/// <summary>
		/// Enters into transient mode.
		/// <para>This method returns an object that serves as the witness to use to later exit from transient mode.</para>
		/// <para>If this object is lost or not used, the only way to exit transient mode is to use the
		/// <see cref="TransientMasterWitness"/> object, but this is considered a bad practice.</para>
		/// <para>This object returned can be null if the system is already in transient mode. Despite of that, it can be
		/// passed to the ExitTransientMode() method, who is prepared to ignore null witnesses as needed.</para>
		/// </summary>
		/// <param name="reason">The reason why entering transient mode, or null to use a generic reason.</param>
		/// <param name="args">An optional list of arguments to build the reason string.</param>
		/// <returns>The object to serve as the witness, or null if we are already in transient mode.</returns>
		public static object EnterTransientMode( string reason, params object[] args )
		{
			if( string.IsNullOrWhiteSpace( reason ) ) reason = "Generic Transient Mode requested";
			else if( args != null && args.Length != 0 ) reason = string.Format( reason, args );

			if( _TransientWitness != null ) {
				DEBUG.IndentLine( "\n-- EnterTransientMode() => Failed: {0}", reason );
				return null;
			}
			DEBUG.IndentLine( "\n-- EnterTransientMode() => Reason: {0}", reason );
			_TransientWitness = new object();
			_TransientReason = reason;
			return _TransientWitness;
		}

		/// <summary>
		/// Exists the transient mode if the witness provided is the same as the witness created when entering the transient
		/// mode.
		/// <para>If the object provided is null, or it is not tha same as the original witness, this method merely returns.</para>
		/// <para>It does accept the object to be the static TransientMasterWitness instance, as a safety meassure to force
		/// and exit from transient mode, but it is considered a bad practice.</para>
		/// </summary>
		/// <param name="witness">The witness to use to decide whether to exit from transient mode or not.</param>
		public static void ExitTransientMode( object witness )
		{
			if( witness == TransientMasterWitness ) _TransientWitness = TransientMasterWitness;

			if( witness == null ) {
				DEBUG.WriteLine( "\n-- ExitTransientMode() => Failed: witness is null." );
			}
			else if( witness != _TransientWitness ) {
				DEBUG.WriteLine( "\n-- ExitTransientMode() => Failed: witness is not current witness." );
			}
			else {
				DEBUG.WriteLine( "\n-- ExitTransientMode() => Reason:{0}", _TransientReason );

				_TransientReason = null; KMetaEntity.OnCollect( exitTransient: true );
				_TransientWitness = null;
			}
			DEBUG.Unindent();
		}
	}

	// =====================================================
	/// <summary>
	/// Instances of this class are automatically attached with the instances of the business classes converting them in
	/// entities managed by the maps system.
	/// <para>The application may want to access the meta entities for informational purposes, but it has not to do anything
	/// else as the life cycle of the meta entities is managed automatically by the maps system.</para>
	/// </summary>
	public class KMetaEntity : Attribute
	{
		static object _SyncRoot = new object();
		static List<KMetaEntity> _MetaEntities = new List<KMetaEntity>();

		static internal Timer _Timer = null;
		static internal bool _InvokeGC = false;

		internal static void TimerCollect( object source, ElapsedEventArgs e )
		{
			if( KMaps.OnTransientMode ) return; // While on transient mode we don't need to collect...

			DEBUG.Indent();
			bool enabled = false; if( _Timer != null ) { enabled = _Timer.Enabled; _Timer.Enabled = false; }

			int start = _MetaEntities.Count; OnCollect( exitTransient: false );
			int end = _MetaEntities.Count;
			if( ( start - end ) > 0 ) DEBUG.WriteLine( "\n-- KMaps.Collect() => Initial:{0} Removed:{1}", start, ( start - end ) );

			if( enabled && _Timer != null ) _Timer.Enabled = true;
			DEBUG.Unindent();
		}
		internal static void OnCollect( bool exitTransient )
		{
			lock( _SyncRoot ) {
				int count = _MetaEntities.Count; for( int i = 0; i < count; i++ ) {
					var meta = _MetaEntities[i];

					bool removable = false;
					if( meta.Host == null ) removable = true;
					else if( meta.Map == null ) removable = true;
					else if( meta.State == KMaps.MetaState.Empty ) removable = true;
					else if( meta.State == KMaps.MetaState.Deleted ) removable = true;

					// Removing the selected entries...
					if( removable ) {
						meta._WeakReference = null;
						meta._Map = null;
						_MetaEntities.RemoveAt( i ); count--; i--;
						continue;
					}

					// If exiting transient mode, refreshing the entries in transient state...
					if( exitTransient && meta.IsTransient ) {
						meta.Map.OnRefresh( meta.Host );
						meta.State = KMaps.MetaState.Ready;
					}
				}
			}
		}

		// --------------------------------------------------
		internal static KMetaEntity First( IKMap map, KRecord find )
		{
			if( map == null ) throw new ArgumentNullException( "map", "Map cannot be null." );
			if( find == null ) throw new ArgumentNullException( "find", "KRecord cannot be null." );
			if( find.Count == 0 ) throw new ArgumentException( "Find record contains no columns." );

			lock( _SyncRoot ) {
				foreach( var meta in _MetaEntities ) {
					if( meta.Host == null ) continue;
					if( meta.Record == null ) continue;
					if( meta.Map != map ) continue;

					if( !find.EquivalentTo( meta.Record, strict: false ) ) continue;
					return meta;
				}
			}
			return null;
		}
		internal static KMetaEntity First( IKMap map, params Func<dynamic, object>[] specs )
		{
			if( map == null ) throw new ArgumentNullException( "map", "Map cannot be null." );
			if( specs == null ) throw new ArgumentNullException( "specs", "Specifications cannot be null." );
			if( specs.Length == 0 ) throw new ArgumentException( "Specifications list is empty." );

			KRecordBuilder builder = new KRecordBuilder( map.Link.DbCaseSensitiveNames );
			foreach( var spec in specs ) builder.Add( spec );
			KRecord find = builder.Generate( autoDispose: true );

			var r = First( map, find ); find.Schema.Dispose(); find.Dispose();
			return r;
		}

		// --------------------------------------------------
		internal static KMetaEntity Get( object obj, bool createMeta )
		{
			if( obj == null ) throw new ArgumentNullException( "obj", "Host Entity is null." );
			KMetaEntity meta = null;

			AttributeCollection list = TypeDescriptor.GetAttributes( obj );
			if( list != null ) { foreach( var attr in list ) if( attr is KMetaEntity ) { meta = (KMetaEntity)attr; break; } }

			if( meta != null ) {
				if( meta.Host == null ) { meta.Reset(); meta.Host = obj; }
				return meta;
			}

			if( createMeta ) {
				meta = new KMetaEntity(); meta.Host = obj;
				TypeDescriptor.AddAttributes( obj, meta );
				return meta;
			}

			return null;
		}

		internal void InsertOn( IKMap map )
		{
			if( map == null ) throw new ArgumentNullException( "map", "Map cannot be null." );
			if( this._Map != null ) throw new InvalidOperationException( "This MetaEntity already belongs to other map: " + this );

			lock( _SyncRoot ) { this._Map = map; _MetaEntities.Add( this ); }
		}
		internal void Remove()
		{
			lock( _SyncRoot ) { this._Map = null; _MetaEntities.Remove( this ); }
		}

		internal static void ClearMap( IKMap map )
		{
			if( map == null ) throw new ArgumentNullException( "map", "Map cannot be null." );
			lock( _SyncRoot ) {
				int count = _MetaEntities.Count; for( int i = 0; i < count; i++ ) {
					var meta = _MetaEntities[i];
					if( meta._Map == map ) { _MetaEntities.RemoveAt( i ); count--; i--; }
				}
			}
		}

		// --------------------------------------------------
		WeakReference _WeakReference = null;
		KMaps.MetaState _State = KMaps.MetaState.Empty;
		KRecord _Record = null;
		IKMap _Map = null;

		internal KMetaEntity() { }
		internal void Reset()
		{
			_WeakReference = null;
			_State = KMaps.MetaState.Empty; // By default...
			_Map = null;
			_Record = null; // Do not dispose, might be in use elsewhere...
		}

		public override string ToString()
		{
			return string.Format( "{0}:[ {1} ]",
				_State,
				Host != null
					? string.Format( "Host: {0}", Host.ToString() )
					: string.Format( "Record: {0}", _Record == null ? "<null>" : _Record.ToString() ) );
		}

		/// <summary>
		/// Gets the host this meta entity is associated with, or null if it is not associated any longer with any business
		/// class instance.
		/// </summary>
		public object Host
		{
			internal set
			{
				if( _WeakReference != null ) throw new InvalidOperationException( "Entity already carries a MetaEntity." );
				_WeakReference = new WeakReference( value );
			}
			get
			{
				if( _WeakReference == null || _WeakReference.Target == null || !_WeakReference.IsAlive ) return null;
				return _WeakReference.Target;
			}
		}
		
		/// <summary>
		/// Gets the <see cref="IKMap"/> this meta entity is registered with, or null if it not registered with any map.
		/// </summary>
		public IKMap Map
		{
			internal set
			{
				if( value == null ) throw new ArgumentException( "Map to set cannot be null." );
				if( _Map != null ) throw new InvalidOperationException( "Entity already belongs to other map." );
				_Map = value;
			}
			get { return _Map; }
		}
		
		/// <summary>
		/// Gets the latest record obtained from the primary table on database when retrieving the information used to
		/// generate an instance of the business class this entity is associated with.
		/// <para>It might be null if this is a new entity not yet retrieved or saved to the database.</para>
		/// </summary>
		public KRecord Record
		{
			internal set
			{
				_Record = value; // Don't dispose the previous one as it might be in use elsewhere...
			}
			get { return _Record; }
		}
		
		/// <summary>
		/// Gets the <see cref="KMaps.MetaSate"/> state of this meta entity.
		/// </summary>
		public KMaps.MetaState State
		{
			internal set
			{
				if( value == KMaps.MetaState.Empty ) throw new ArgumentException( "State to set cannot be 'Empty'." );
				_State = value;
			}
			get { return _State; }
		}

		/// <summary>
		/// Gets whether this entity is dirty (transient) or not.
		/// </summary>
		public bool IsTransient
		{
			get { return ( _State == KMaps.MetaState.Empty || _State == KMaps.MetaState.Ready ) ? false : true; }
		}
		
		/// <summary>
		/// Gets a <see cref="KRecord"/> instance containing the changes from the current values of the business entity with
		/// respect to the values the original record it had when this instance were retrieved or saved to the database.
		/// <para>It might be null if no changes are detected.</para>
		/// </summary>
		public KRecord Changes
		{
			get
			{
				if( Host == null ) return null;
				if( Map == null ) throw new InvalidOperationException( "This entity is not registered with any map: " + this );
				if( Record == null ) throw new InvalidOperationException( "This entity has no stored record: " + this );

				KRecord current = new KRecord( Map.Schema ); Map.WriteRecord( Host, current );
				KRecord changes = current.Changes( Record, strict: false ); current.Dispose();
				return changes;
			}
		}
	}
}
// ========================================================
